using System.Diagnostics;
using System.Threading.Channels;
using ElegooLink.Events;

namespace ElegooLink.Desktop.Core;

public sealed record ProcessLaunchRequest(
    string ExecutablePath,
    string Arguments,
    string WorkingDirectory,
    bool RunHidden);

public sealed record ProcessLaunchResult(int ProcessId);

public interface IProcessLauncher
{
    Task<ProcessLaunchResult> LaunchAsync(
        ProcessLaunchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DirectProcessLauncher : IProcessLauncher
{
    public Task<ProcessLaunchResult> LaunchAsync(
        ProcessLaunchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = request.RunHidden,
            WindowStyle = request.RunHidden
                ? ProcessWindowStyle.Hidden
                : ProcessWindowStyle.Normal
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                $"Windows did not start '{request.ExecutablePath}'.");
        return Task.FromResult(new ProcessLaunchResult(process.Id));
    }
}

public sealed record AutomationActionReport(
    Guid PrinterId,
    bool Succeeded,
    string Message,
    string Details);

public static class EventActionRuleValidator
{
    public static IReadOnlyList<string> Validate(EventActionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!rule.Enabled)
        {
            return [];
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(rule.ExecutablePath))
        {
            errors.Add("An executable path is required.");
        }
        else
        {
            try
            {
                if (!Path.IsPathFullyQualified(rule.ExecutablePath))
                {
                    errors.Add("The executable path must be absolute.");
                }
                else if (!File.Exists(rule.ExecutablePath))
                {
                    errors.Add("The executable file does not exist.");
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException)
            {
                errors.Add("The executable path is invalid.");
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.WorkingDirectory))
        {
            try
            {
                if (!Path.IsPathFullyQualified(rule.WorkingDirectory))
                {
                    errors.Add("The working directory must be absolute.");
                }
                else if (!Directory.Exists(rule.WorkingDirectory))
                {
                    errors.Add("The working directory does not exist.");
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException)
            {
                errors.Add("The working directory is invalid.");
            }
        }

        return errors;
    }
}

public static class ArgumentTemplateExpander
{
    public static string Expand(
        string? template,
        SavedPrinter printer,
        PrinterEvent printerEvent)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printerEvent);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{PrinterId}"] = printerEvent.PrinterId,
            ["{PrinterName}"] = printer.DisplayLabel,
            ["{PrinterIp}"] = printer.Host,
            ["{Event}"] = printerEvent.Kind.ToString(),
            ["{TimestampUtc}"] = printerEvent.TimestampUtc.ToString("O"),
            ["{Message}"] = printerEvent.Message,
            ["{FileName}"] = printerEvent.FileName,
            ["{Progress}"] = printerEvent.Progress?.ToString() ?? "",
            ["{CurrentLayer}"] = printerEvent.CurrentLayer?.ToString() ?? "",
            ["{TotalLayers}"] = printerEvent.TotalLayers?.ToString() ?? "",
            ["{State}"] = printerEvent.State?.ToString() ?? "",
            ["{SubState}"] = printerEvent.SubState?.ToString() ?? "",
            ["{ErrorCodes}"] = string.Join(",", printerEvent.ErrorCodes)
        };

        var result = template ?? "";
        foreach (var (placeholder, value) in values)
        {
            result = result.Replace(
                placeholder,
                value ?? "",
                StringComparison.Ordinal);
        }

        return result;
    }
}

public sealed class AutomationEngine : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly IProcessLauncher _processLauncher;
    private readonly Channel<AutomationWorkItem> _queue;
    private readonly Task _worker;
    private readonly Dictionary<Guid, bool> _connectionStates = [];
    private Dictionary<PrinterEventKind, EventActionRule> _rules = [];
    private int _completed;

    public AutomationEngine(
        IProcessLauncher processLauncher,
        IEnumerable<EventActionRule>? rules = null)
    {
        _processLauncher = processLauncher;
        _queue = Channel.CreateUnbounded<AutomationWorkItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        UpdateRules(rules ?? AutomationCatalog.CreateDefaultRules());
        _worker = RunAsync();
    }

    public event EventHandler<AutomationActionReport>? ActionReported;

    public void UpdateRules(IEnumerable<EventActionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        lock (_gate)
        {
            _rules = AutomationCatalog.NormalizeRules(rules)
                .ToDictionary(rule => rule.EventKind, rule => rule);
        }
    }

    public bool Enqueue(SavedPrinter printer, PrinterEvent printerEvent)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(printerEvent);

        EventActionRule? rule;
        lock (_gate)
        {
            if (printerEvent.Kind is PrinterEventKind.Connected or PrinterEventKind.Disconnected)
            {
                var connected = printerEvent.Kind == PrinterEventKind.Connected;
                if (_connectionStates.TryGetValue(printer.Id, out var previous) &&
                    previous == connected)
                {
                    return false;
                }

                _connectionStates[printer.Id] = connected;
            }

            if (!AutomationCatalog.IsActionable(printerEvent.Kind) ||
                printerEvent.IsInitialObservation ||
                !_rules.TryGetValue(printerEvent.Kind, out rule) ||
                !rule.Enabled)
            {
                return false;
            }

            rule = rule.Snapshot();
        }

        return _queue.Writer.TryWrite(
            new AutomationWorkItem(printer.Snapshot(), printerEvent, rule));
    }

    public async Task CompleteAsync()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
        {
            _queue.Writer.TryComplete();
        }

        await _worker.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await CompleteAsync().ConfigureAwait(false);

    private async Task RunAsync()
    {
        await foreach (var workItem in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            var arguments = ArgumentTemplateExpander.Expand(
                workItem.Rule.ArgumentsTemplate,
                workItem.Printer,
                workItem.PrinterEvent);
            var workingDirectory = string.IsNullOrWhiteSpace(workItem.Rule.WorkingDirectory)
                ? Path.GetDirectoryName(workItem.Rule.ExecutablePath) ?? ""
                : workItem.Rule.WorkingDirectory;
            var request = new ProcessLaunchRequest(
                workItem.Rule.ExecutablePath,
                arguments,
                workingDirectory,
                workItem.Rule.RunHidden);

            try
            {
                var result = await _processLauncher.LaunchAsync(request).ConfigureAwait(false);
                Report(new AutomationActionReport(
                    workItem.Printer.Id,
                    true,
                    $"Started {workItem.PrinterEvent.Kind} action.",
                    $"Executable: {request.ExecutablePath}{Environment.NewLine}" +
                    $"Arguments: {request.Arguments}{Environment.NewLine}" +
                    $"Process ID: {result.ProcessId}"));
            }
            catch (Exception exception)
            {
                Report(new AutomationActionReport(
                    workItem.Printer.Id,
                    false,
                    $"Could not start {workItem.PrinterEvent.Kind} action: {exception.Message}",
                    $"Executable: {request.ExecutablePath}{Environment.NewLine}" +
                    $"Arguments: {request.Arguments}{Environment.NewLine}" +
                    exception));
            }
        }
    }

    private void Report(AutomationActionReport report)
    {
        var handlers = ActionReported;
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<AutomationActionReport> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, report);
            }
            catch
            {
                // A UI observer must not stop the automation queue.
            }
        }
    }

    private sealed record AutomationWorkItem(
        SavedPrinter Printer,
        PrinterEvent PrinterEvent,
        EventActionRule Rule);
}
