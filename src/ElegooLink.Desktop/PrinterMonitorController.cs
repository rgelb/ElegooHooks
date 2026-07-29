using ElegooLink.Desktop.Core;
using ElegooLink.Events;

namespace ElegooLink.Desktop;

internal enum PrinterConnectionStatus
{
    Offline,
    Connecting,
    Connected
}

internal sealed record PrinterView(
    Guid Id,
    string DisplayName,
    string Host,
    PrinterType PrinterType,
    PrinterConnectionStatus Status);

internal sealed record NewPrinterRequest(
    string Host,
    string DisplayName,
    PrinterType PrinterType,
    PrinterInfo? DiscoveredPrinter);

internal sealed class PrinterLogChangedEventArgs(
    Guid printerId,
    PrinterLogEntry entry) : EventArgs
{
    public Guid PrinterId { get; } = printerId;

    public PrinterLogEntry Entry { get; } = entry;
}

internal sealed class ControllerWarningEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

internal sealed class PrinterMonitorController : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ElegooLinkClient _client;
    private readonly ISettingsStore _settingsStore;
    private readonly PrinterEventRouter _eventRouter = new();
    private readonly PrinterLogStore _logs = new();
    private readonly AutomationEngine _automation;
    private readonly Dictionary<Guid, PrinterConnectionStatus> _connectionStates = [];
    private AppSettings _settings = SettingsNormalizer.Normalize(null);
    private bool _acceptEvents;
    private bool _initialized;
    private int _shutdownStarted;

    public PrinterMonitorController(
        ElegooLinkClient? client = null,
        ISettingsStore? settingsStore = null,
        IProcessLauncher? processLauncher = null)
    {
        _client = client ?? new ElegooLinkClient();
        _settingsStore = settingsStore ?? new JsonSettingsStore();
        _automation = new AutomationEngine(
            processLauncher ?? new DirectProcessLauncher());
        _automation.ActionReported += OnAutomationActionReported;
    }

    public event EventHandler? PrintersChanged;

    public event EventHandler<PrinterLogChangedEventArgs>? LogEntryAdded;

    public event EventHandler<ControllerWarningEventArgs>? WarningRaised;

    public string SettingsPath => _settingsStore.SettingsPath;

    public IReadOnlyList<PrinterView> GetPrinters()
    {
        lock (_gate)
        {
            return _settings.Printers
                .Select(printer => new PrinterView(
                    printer.Id,
                    printer.DisplayLabel,
                    printer.Host,
                    printer.PrinterType,
                    _connectionStates.GetValueOrDefault(
                        printer.Id,
                        PrinterConnectionStatus.Offline)))
                .ToArray();
        }
    }

    public IReadOnlyList<PrinterLogEntry> GetLogs(Guid printerId) =>
        _logs.GetSnapshot(printerId);

    public IReadOnlyList<EventActionRule> GetAutomationRules()
    {
        lock (_gate)
        {
            return _settings.EventActions
                .Select(rule => rule.Snapshot())
                .ToArray();
        }
    }

    public bool IsDuplicateHost(string host)
    {
        lock (_gate)
        {
            return PrinterAddress.IsDuplicate(_settings.Printers, host);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var loadResult = await _settingsStore.LoadAsync(cancellationToken)
            .ConfigureAwait(false);

        lock (_gate)
        {
            _settings = loadResult.Settings;
            _connectionStates.Clear();
            foreach (var printer in _settings.Printers)
            {
                _connectionStates[printer.Id] = PrinterConnectionStatus.Offline;
                _eventRouter.RegisterKnownPrinter(
                    printer.Id,
                    printer.LastKnownPrinterId);
            }
        }

        _automation.UpdateRules(loadResult.Settings.EventActions);
        cancellationToken.ThrowIfCancellationRequested();
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _shutdownStarted) != 0)
            {
                throw new OperationCanceledException(
                    "The printer monitor is shutting down.",
                    cancellationToken);
            }

            _client.EventReceived += OnPrinterEventReceived;
            _client.Initialize(enableNativeConsoleLogging: false);
            _initialized = true;
            _acceptEvents = true;
        }
        finally
        {
            _connectionLock.Release();
        }

        RaisePrintersChanged();
        if (!string.IsNullOrWhiteSpace(loadResult.Warning))
        {
            WarningRaised?.Invoke(
                this,
                new ControllerWarningEventArgs(loadResult.Warning));
        }

        Guid[] printerIds;
        lock (_gate)
        {
            printerIds = _settings.Printers.Select(printer => printer.Id).ToArray();
        }

        foreach (var printerId in printerIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ConnectPrinterAsync(printerId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task<PrinterInfo?> DiscoverByHostAsync(
        string host,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        var discovered = await _client.DiscoverAsync(5_000, cancellationToken)
            .ConfigureAwait(false);
        return PrinterAddress.FindDiscovered(discovered, host);
    }

    public async Task<Guid> AddPrinterAsync(
        NewPrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        if (!PrinterAddress.TryNormalize(request.Host, out var normalizedHost))
        {
            throw new ArgumentException("A valid IPv4 or IPv6 address is required.");
        }

        if (request.PrinterType == PrinterType.Unknown)
        {
            throw new ArgumentException(
                "A printer type is required when discovery did not identify the printer.");
        }

        SavedPrinter printer;
        lock (_gate)
        {
            if (PrinterAddress.IsDuplicate(_settings.Printers, normalizedHost))
            {
                throw new InvalidOperationException(
                    $"A printer at {normalizedHost} is already configured.");
            }

            var discovered = request.DiscoveredPrinter;
            printer = new SavedPrinter
            {
                Host = normalizedHost,
                DisplayName = FirstNonEmpty(
                    request.DisplayName,
                    discovered?.Name,
                    normalizedHost),
                PrinterType = request.PrinterType,
                Model = discovered?.Model ?? DefaultModel(request.PrinterType),
                LastKnownPrinterId = discovered?.PrinterId ?? ""
            };
            _settings.Printers.Add(printer);
            _connectionStates[printer.Id] = PrinterConnectionStatus.Offline;
            _eventRouter.RegisterKnownPrinter(
                printer.Id,
                printer.LastKnownPrinterId);
        }

        try
        {
            await SaveSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                _settings.Printers.RemoveAll(item => item.Id == printer.Id);
                _connectionStates.Remove(printer.Id);
                _eventRouter.Remove(printer.Id);
            }

            throw;
        }

        RaisePrintersChanged();
        await ConnectPrinterAsync(printer.Id, cancellationToken).ConfigureAwait(false);
        return printer.Id;
    }

    public async Task RetryConnectionAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        await ConnectPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemovePrinterAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        var sdkPrinterId = _eventRouter.GetSdkPrinterId(printerId);
        if (!string.IsNullOrWhiteSpace(sdkPrinterId))
        {
            try
            {
                await _client.DisconnectAsync(sdkPrinterId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ElegooLinkException exception)
            {
                AddApplicationLog(
                    printerId,
                    "DisconnectWarning",
                    $"The printer could not be disconnected cleanly: {exception.Message}",
                    exception.ToString());
            }
        }

        SavedPrinter? removed;
        lock (_gate)
        {
            removed = _settings.Printers.FirstOrDefault(
                printer => printer.Id == printerId);
            if (removed is null)
            {
                return;
            }

            _settings.Printers.Remove(removed);
            _connectionStates.Remove(printerId);
            _eventRouter.Remove(printerId);
        }

        try
        {
            await SaveSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                _settings.Printers.Add(removed);
                _connectionStates[printerId] = PrinterConnectionStatus.Offline;
                _eventRouter.RegisterKnownPrinter(
                    removed.Id,
                    removed.LastKnownPrinterId);
            }

            throw;
        }

        _logs.Remove(printerId);
        RaisePrintersChanged();
    }

    public async Task UpdateAutomationRulesAsync(
        IEnumerable<EventActionRule> rules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var normalized = AutomationCatalog.NormalizeRules(rules);
        List<EventActionRule> previous;
        lock (_gate)
        {
            previous = _settings.EventActions;
            _settings.EventActions = normalized;
        }

        _automation.UpdateRules(normalized);
        try
        {
            await SaveSettingsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                _settings.EventActions = previous;
            }

            _automation.UpdateRules(previous);
            throw;
        }
    }

    public void ClearLogs(Guid printerId) => _logs.Clear(printerId);

    public async Task ShutdownAsync()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return;
        }

        _acceptEvents = false;
        _client.EventReceived -= OnPrinterEventReceived;
        await _automation.CompleteAsync().ConfigureAwait(false);

        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                Guid[] localPrinterIds;
                lock (_gate)
                {
                    localPrinterIds = _settings.Printers
                        .Select(printer => printer.Id)
                        .ToArray();
                }

                var sdkPrinterIds = localPrinterIds
                    .Select(_eventRouter.GetSdkPrinterId)
                    .Where(printerId => !string.IsNullOrWhiteSpace(printerId))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToArray();

                foreach (var sdkPrinterId in sdkPrinterIds)
                {
                    try
                    {
                        await _client.DisconnectAsync(sdkPrinterId).ConfigureAwait(false);
                    }
                    catch (ElegooLinkException)
                    {
                        // Shutdown continues even when a printer is already offline.
                    }
                }
            }

            await _client.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }

    public async ValueTask DisposeAsync() => await ShutdownAsync().ConfigureAwait(false);

    private async Task ConnectPrinterAsync(
        Guid printerId,
        CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SavedPrinter? printer;
            lock (_gate)
            {
                printer = _settings.Printers.FirstOrDefault(
                    item => item.Id == printerId);
                if (printer is not null)
                {
                    _connectionStates[printerId] = PrinterConnectionStatus.Connecting;
                }
            }

            if (printer is null)
            {
                return;
            }

            RaisePrintersChanged();
            AddApplicationLog(
                printerId,
                "ConnectionAttempt",
                $"Connecting to {printer.DisplayLabel} at {printer.Host}...");

            _eventRouter.BeginConnection(printerId);
            try
            {
                var connected = await _client.ConnectAsync(
                    new PrinterConnectionOptions
                    {
                        Host = printer.Host,
                        PrinterType = printer.PrinterType,
                        Brand = "ELEGOO",
                        Name = printer.DisplayLabel,
                        Model = string.IsNullOrWhiteSpace(printer.Model)
                            ? DefaultModel(printer.PrinterType)
                            : printer.Model,
                        AutoReconnect = true,
                        ConnectionTimeout = 5_000
                    },
                    cancellationToken).ConfigureAwait(false);

                _eventRouter.CompleteConnection(printerId, connected.PrinterId);
                lock (_gate)
                {
                    var configured = _settings.Printers.First(
                        item => item.Id == printerId);
                    configured.LastKnownPrinterId = connected.PrinterId;
                    if (string.IsNullOrWhiteSpace(configured.DisplayName))
                    {
                        configured.DisplayName = FirstNonEmpty(
                            connected.Name,
                            configured.Host);
                    }

                    if (!string.IsNullOrWhiteSpace(connected.Model))
                    {
                        configured.Model = connected.Model;
                    }

                    _connectionStates[printerId] = PrinterConnectionStatus.Connected;
                }

                RaisePrintersChanged();
                try
                {
                    await SaveSettingsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    AddApplicationLog(
                        printerId,
                        "SettingsWarning",
                        $"Connected, but updated printer details could not be saved: {exception.Message}",
                        exception.ToString());
                }

                await _client.RefreshStatusAsync(
                    connected.PrinterId,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                SetConnectionState(printerId, PrinterConnectionStatus.Offline);
                throw;
            }
            catch (Exception exception) when (
                exception is ElegooLinkException or ArgumentException)
            {
                SetConnectionState(printerId, PrinterConnectionStatus.Offline);
                AddApplicationLog(
                    printerId,
                    "ConnectionFailed",
                    $"Could not connect to {printer.DisplayLabel}: {exception.Message}",
                    exception.ToString());
            }
            finally
            {
                _eventRouter.EndConnection(printerId);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void OnPrinterEventReceived(object? sender, PrinterEvent printerEvent)
    {
        if (!_acceptEvents ||
            !_eventRouter.TryRoute(printerEvent, out var localPrinterId))
        {
            return;
        }

        SavedPrinter? printer;
        lock (_gate)
        {
            printer = _settings.Printers
                .FirstOrDefault(item => item.Id == localPrinterId)
                ?.Snapshot();

            if (printerEvent.Kind == PrinterEventKind.Connected)
            {
                _connectionStates[localPrinterId] = PrinterConnectionStatus.Connected;
            }
            else if (printerEvent.Kind == PrinterEventKind.Disconnected)
            {
                _connectionStates[localPrinterId] = PrinterConnectionStatus.Offline;
            }
        }

        if (printer is null)
        {
            return;
        }

        var logEntry = PrinterLogEntry.FromPrinterEvent(printerEvent);
        AddLog(localPrinterId, logEntry);
        _automation.Enqueue(printer, printerEvent);

        if (printerEvent.Kind is PrinterEventKind.Connected or PrinterEventKind.Disconnected)
        {
            RaisePrintersChanged();
        }
    }

    private void OnAutomationActionReported(
        object? sender,
        AutomationActionReport report)
    {
        AddApplicationLog(
            report.PrinterId,
            report.Succeeded ? "ActionStarted" : "ActionFailed",
            report.Message,
            report.Details);
    }

    private void AddApplicationLog(
        Guid printerId,
        string eventName,
        string message,
        string? details = null) =>
        AddLog(
            printerId,
            PrinterLogEntry.Application(eventName, message, details));

    private void AddLog(Guid printerId, PrinterLogEntry entry)
    {
        _logs.Add(printerId, entry);
        LogEntryAdded?.Invoke(
            this,
            new PrinterLogChangedEventArgs(printerId, entry));
    }

    private void SetConnectionState(
        Guid printerId,
        PrinterConnectionStatus status)
    {
        lock (_gate)
        {
            _connectionStates[printerId] = status;
        }

        RaisePrintersChanged();
    }

    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        AppSettings snapshot;
        lock (_gate)
        {
            snapshot = new AppSettings
            {
                Printers = _settings.Printers
                    .Select(printer => printer.Snapshot())
                    .ToList(),
                EventActions = _settings.EventActions
                    .Select(rule => rule.Snapshot())
                    .ToList()
            };
        }

        await _settingsStore.SaveAsync(snapshot, cancellationToken)
            .ConfigureAwait(false);
    }

    private void RaisePrintersChanged() => PrintersChanged?.Invoke(this, EventArgs.Empty);

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "The printer monitor has not been initialized.");
        }
    }

    private static string DefaultModel(PrinterType printerType) =>
        printerType switch
        {
            PrinterType.ElegooCentauriCarbon => "Centauri Carbon",
            PrinterType.ElegooCentauriCarbon2 => "Centauri Carbon 2",
            PrinterType.GenericFdmKlipper => "Generic FDM Klipper",
            _ => "ELEGOO FDM Klipper"
        };

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
}
