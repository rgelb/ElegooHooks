using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;

namespace ElegooLink.Events;

/// <summary>A managed event-oriented wrapper around the native Elegoo Link SDK.</summary>
public sealed class ElegooLinkClient : IDisposable, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Channel<string> _nativeEvents = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ElegooLinkEventProjector _projector = new();
    private readonly NativeMethods.EventCallback _nativeCallback;
    private readonly Task _dispatchTask;
    private bool _initialized;
    private bool _nativeConsoleLogging;
    private bool _disposed;

    public ElegooLinkClient()
    {
        _nativeCallback = OnNativeEvent;
        _dispatchTask = DispatchEventsAsync();
    }

    /// <summary>Raised for every low-level SDK event and every projected lifecycle event.</summary>
    public event EventHandler<PrinterEvent>? EventReceived;

    /// <summary>The version reported by the native Elegoo Link SDK.</summary>
    public string SdkVersion
    {
        get
        {
            EnsureInitialized();
            return NativeMethods.GetUtf8(NativeMethods.el_get_version());
        }
    }

    /// <summary>Initializes the SDK and subscribes to all of its published event types.</summary>
    public void Initialize(int logLevel = 2, bool enableNativeConsoleLogging = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_initialized)
        {
            return;
        }

        try
        {
            // The current upstream SDK does not consistently honor logEnableConsole=false.
            // Force its log level off unless the caller explicitly requested native logs.
            var effectiveLogLevel = enableNativeConsoleLogging ? logLevel : 6;
            var result = NativeMethods.el_initialize(
                effectiveLogLevel,
                enableNativeConsoleLogging ? 1 : 0,
                _nativeCallback,
                nint.Zero);
            if (result == 0)
            {
                throw new ElegooLinkException("Elegoo Link SDK initialization failed.");
            }

            _initialized = true;
            _nativeConsoleLogging = enableNativeConsoleLogging;
        }
        catch (DllNotFoundException exception)
        {
            throw MissingNativeBridge(exception);
        }
        catch (BadImageFormatException exception)
        {
            throw new ElegooLinkException(
                "The native Elegoo Link bridge architecture does not match this process. Build and run both as x64.",
                innerException: exception);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw new ElegooLinkException(
                "The native Elegoo Link bridge is incompatible with this managed wrapper. Rebuild the native project.",
                innerException: exception);
        }
    }

    /// <summary>Discovers Elegoo printers on the local network.</summary>
    public async Task<IReadOnlyList<PrinterInfo>> DiscoverAsync(
        int timeoutMs = 5_000,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentOutOfRangeException.ThrowIfLessThan(timeoutMs, 1);

        var json = await Task.Run(
            () => NativeMethods.GetUtf8(NativeMethods.el_discover(timeoutMs)),
            cancellationToken).ConfigureAwait(false);
        var result = ReadResult<DiscoveryData>(json, "Printer discovery");
        var printers = (IReadOnlyList<PrinterInfo>)(result.Data?.Printers ?? []);

        foreach (var printer in printers)
        {
            Raise(_projector.CreateDiscovered(printer));
        }

        return printers;
    }

    /// <summary>Connects to one printer and returns the SDK's normalized printer information.</summary>
    public async Task<PrinterInfo> ConnectAsync(
        PrinterConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Host) && string.IsNullOrWhiteSpace(options.PrinterId))
        {
            throw new ArgumentException("A host or discovered printer ID is required.", nameof(options));
        }

        if (options.PrinterType == PrinterType.Unknown)
        {
            throw new ArgumentException(
                "PrinterType cannot be Unknown when connecting. Use the type returned by discovery or specify it explicitly.",
                nameof(options));
        }

        if (_nativeConsoleLogging && HasCredentials(options))
        {
            throw new ElegooLinkException(
                "Native SDK console logging cannot be used for a credentialed connection because the upstream SDK logs connection parameters without redacting every credential. Reinitialize with native logging disabled.");
        }

        var request = JsonSerializer.Serialize(options, JsonOptions);
        var json = await Task.Run(
            () => NativeMethods.GetUtf8(NativeMethods.el_connect(request)),
            cancellationToken).ConfigureAwait(false);
        var result = ReadResult<ConnectData>(json, "Printer connection");
        var printer = result.Data?.PrinterInfo
            ?? throw new ElegooLinkException("Elegoo Link reported a successful connection without printer information.");

        return printer;
    }

    /// <summary>Requests a fresh status event for a connected printer.</summary>
    public async Task RefreshStatusAsync(string printerId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(printerId);

        var json = await Task.Run(
            () => NativeMethods.GetUtf8(NativeMethods.el_refresh_status(printerId)),
            cancellationToken).ConfigureAwait(false);
        ReadResult<JsonElement>(json, "Status refresh");
    }

    /// <summary>Disconnects a printer.</summary>
    public async Task DisconnectAsync(string printerId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        ArgumentException.ThrowIfNullOrWhiteSpace(printerId);

        var json = await Task.Run(
            () => NativeMethods.GetUtf8(NativeMethods.el_disconnect(printerId)),
            cancellationToken).ConfigureAwait(false);
        ReadResult<JsonElement>(json, "Printer disconnect");
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_initialized)
        {
            NativeMethods.el_cleanup();
            _initialized = false;
        }

        _nativeEvents.Writer.TryComplete();
        await _dispatchTask.ConfigureAwait(false);
        GC.KeepAlive(_nativeCallback);
        GC.SuppressFinalize(this);
    }

    private void OnNativeEvent(nint utf8Json, nint context)
    {
        try
        {
            var json = Marshal.PtrToStringUTF8(utf8Json);
            if (!string.IsNullOrWhiteSpace(json))
            {
                _nativeEvents.Writer.TryWrite(json);
            }
        }
        catch
        {
            // Exceptions must never cross the unmanaged callback boundary.
        }
    }

    private async Task DispatchEventsAsync()
    {
        await foreach (var envelope in _nativeEvents.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            IReadOnlyList<PrinterEvent> events;
            try
            {
                events = _projector.Project(envelope);
            }
            catch (Exception exception)
            {
                using var document = JsonDocument.Parse("{}");
                events =
                [
                    new PrinterEvent
                    {
                        TimestampUtc = DateTimeOffset.UtcNow,
                        Kind = PrinterEventKind.UnknownSdkEvent,
                        Message = $"Could not parse an Elegoo Link event: {exception.Message}",
                        Payload = document.RootElement.Clone()
                    }
                ];
            }

            foreach (var printerEvent in events)
            {
                Raise(printerEvent);
            }
        }
    }

    private void Raise(PrinterEvent printerEvent)
    {
        var handlers = EventReceived;
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<PrinterEvent> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, printerEvent);
            }
            catch
            {
                // One consumer must not stop SDK event processing for all other consumers.
            }
        }
    }

    private static NativeResult<T> ReadResult<T>(string json, string operation)
    {
        NativeResult<T>? result;
        try
        {
            result = JsonSerializer.Deserialize<NativeResult<T>>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ElegooLinkException(
                $"{operation} returned invalid JSON: {exception.Message}",
                innerException: exception);
        }

        if (result is null)
        {
            throw new ElegooLinkException($"{operation} returned an empty result.");
        }

        if (result.Code != 0)
        {
            throw new ElegooLinkException(
                $"{operation} failed: {result.Message} (SDK code {result.Code}).",
                result.Code);
        }

        return result;
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_initialized)
        {
            throw new InvalidOperationException("Call Initialize() before using Elegoo Link.");
        }
    }

    private static ElegooLinkException MissingNativeBridge(Exception innerException) =>
        new(
            $"Could not load {NativeMethods.LibraryName}. Expected the native library in " +
            $"'{AppContext.BaseDirectory}'. Build it with scripts/build.ps1 and make sure " +
            "the application and native bridge are both x64.",
            innerException: innerException);

    private static bool HasCredentials(PrinterConnectionOptions options) =>
        !string.IsNullOrEmpty(options.Password)
        || !string.IsNullOrEmpty(options.Token)
        || !string.IsNullOrEmpty(options.AccessCode)
        || !string.IsNullOrEmpty(options.PinCode);

    private sealed record NativeResult<T>
    {
        public int Code { get; init; }
        public string Message { get; init; } = "";
        public T? Data { get; init; }
    }

    private sealed record DiscoveryData
    {
        public List<PrinterInfo> Printers { get; init; } = [];
    }

    private sealed record ConnectData
    {
        public bool IsConnected { get; init; }
        public PrinterInfo? PrinterInfo { get; init; }
    }
}
