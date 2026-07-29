using ElegooLink.Events;

namespace ElegooLink.Desktop.Core;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<SavedPrinter> Printers { get; set; } = [];

    public List<EventActionRule> EventActions { get; set; } = AutomationCatalog.CreateDefaultRules();
}

public sealed class SavedPrinter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Host { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public PrinterType PrinterType { get; set; } = PrinterType.Unknown;

    public string Model { get; set; } = "";

    public string LastKnownPrinterId { get; set; } = "";

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(DisplayName) ? Host : DisplayName;

    public SavedPrinter Snapshot() =>
        new()
        {
            Id = Id,
            Host = Host,
            DisplayName = DisplayName,
            PrinterType = PrinterType,
            Model = Model,
            LastKnownPrinterId = LastKnownPrinterId
        };
}

public sealed class EventActionRule
{
    public PrinterEventKind EventKind { get; set; }

    public bool Enabled { get; set; }

    public string ExecutablePath { get; set; } = "";

    public string ArgumentsTemplate { get; set; } = "";

    public string WorkingDirectory { get; set; } = "";

    public bool RunHidden { get; set; } = true;

    public EventActionRule Snapshot() =>
        new()
        {
            EventKind = EventKind,
            Enabled = Enabled,
            ExecutablePath = ExecutablePath,
            ArgumentsTemplate = ArgumentsTemplate,
            WorkingDirectory = WorkingDirectory,
            RunHidden = RunHidden
        };
}

public static class AutomationCatalog
{
    public static IReadOnlyList<PrinterEventKind> ActionableEvents { get; } =
    [
        PrinterEventKind.Connected,
        PrinterEventKind.Disconnected,
        PrinterEventKind.PrintStarted,
        PrinterEventKind.PrintCompleted,
        PrinterEventKind.PrintPausing,
        PrinterEventKind.PrintPaused,
        PrinterEventKind.PrintResuming,
        PrinterEventKind.PrintResumed,
        PrinterEventKind.PrintStopping,
        PrinterEventKind.PrintStopped,
        PrinterEventKind.PrinterError
    ];

    public static bool IsActionable(PrinterEventKind eventKind) =>
        ActionableEvents.Contains(eventKind);

    public static List<EventActionRule> CreateDefaultRules() =>
        ActionableEvents
            .Select(eventKind => new EventActionRule
            {
                EventKind = eventKind,
                RunHidden = true
            })
            .ToList();

    public static List<EventActionRule> NormalizeRules(IEnumerable<EventActionRule>? rules)
    {
        var configured = (rules ?? [])
            .Where(rule => IsActionable(rule.EventKind))
            .GroupBy(rule => rule.EventKind)
            .ToDictionary(group => group.Key, group => group.Last());

        return ActionableEvents
            .Select(eventKind => configured.TryGetValue(eventKind, out var rule)
                ? rule.Snapshot()
                : new EventActionRule { EventKind = eventKind, RunHidden = true })
            .ToList();
    }
}

public static class SettingsNormalizer
{
    public static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= new AppSettings();
        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;

        var normalizedPrinters = new List<SavedPrinter>();
        var printerIds = new HashSet<Guid>();
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var printer in settings.Printers ?? [])
        {
            if (!PrinterAddress.TryNormalize(printer.Host, out var host) ||
                !hosts.Add(host))
            {
                continue;
            }

            if (printer.Id == Guid.Empty || !printerIds.Add(printer.Id))
            {
                printer.Id = Guid.NewGuid();
                printerIds.Add(printer.Id);
            }

            printer.Host = host;
            printer.DisplayName = printer.DisplayName?.Trim() ?? "";
            printer.Model = printer.Model?.Trim() ?? "";
            printer.LastKnownPrinterId = printer.LastKnownPrinterId?.Trim() ?? "";
            normalizedPrinters.Add(printer);
        }

        settings.Printers = normalizedPrinters;
        settings.EventActions = AutomationCatalog.NormalizeRules(settings.EventActions);
        return settings;
    }
}
