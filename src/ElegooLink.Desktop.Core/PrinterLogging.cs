using System.Text;
using System.Text.Json;
using ElegooLink.Events;

namespace ElegooLink.Desktop.Core;

public sealed record PrinterLogEntry(
    DateTimeOffset TimestampUtc,
    string EventName,
    string Message,
    string Details,
    bool IsApplicationEvent = false)
{
    private static readonly JsonSerializerOptions DetailJsonOptions = new()
    {
        WriteIndented = true
    };

    public static PrinterLogEntry FromPrinterEvent(PrinterEvent printerEvent)
    {
        ArgumentNullException.ThrowIfNull(printerEvent);

        var details = new StringBuilder()
            .AppendLine($"SDK event: {printerEvent.SdkEventType}")
            .AppendLine($"Printer ID: {printerEvent.PrinterId}")
            .AppendLine($"Initial observation: {printerEvent.IsInitialObservation}");

        AppendValue(details, "State", printerEvent.State);
        AppendValue(details, "Sub-state", printerEvent.SubState);
        AppendValue(details, "File", printerEvent.FileName);
        AppendValue(details, "Progress", printerEvent.Progress);
        AppendValue(details, "Current layer", printerEvent.CurrentLayer);
        AppendValue(details, "Total layers", printerEvent.TotalLayers);

        if (printerEvent.ErrorCodes.Count > 0)
        {
            details.AppendLine($"Error codes: {string.Join(", ", printerEvent.ErrorCodes)}");
        }

        if (printerEvent.Payload.ValueKind is not JsonValueKind.Undefined)
        {
            details
                .AppendLine()
                .AppendLine("Payload:")
                .AppendLine(JsonSerializer.Serialize(
                    printerEvent.Payload,
                    DetailJsonOptions));
        }

        return new PrinterLogEntry(
            printerEvent.TimestampUtc,
            printerEvent.Kind.ToString(),
            printerEvent.Message,
            details.ToString().TrimEnd());
    }

    public static PrinterLogEntry Application(
        string eventName,
        string message,
        string? details = null) =>
        new(
            DateTimeOffset.UtcNow,
            eventName,
            message,
            details ?? message,
            IsApplicationEvent: true);

    private static void AppendValue<T>(StringBuilder builder, string name, T? value)
    {
        if (value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
        {
            builder.AppendLine($"{name}: {value}");
        }
    }
}

public sealed class PrinterLogStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Queue<PrinterLogEntry>> _logs = [];

    public PrinterLogStore(int maximumEntriesPerPrinter = 10_000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEntriesPerPrinter, 1);
        MaximumEntriesPerPrinter = maximumEntriesPerPrinter;
    }

    public int MaximumEntriesPerPrinter { get; }

    public void Add(Guid printerId, PrinterLogEntry entry)
    {
        lock (_gate)
        {
            if (!_logs.TryGetValue(printerId, out var entries))
            {
                entries = new Queue<PrinterLogEntry>();
                _logs.Add(printerId, entries);
            }

            entries.Enqueue(entry);
            while (entries.Count > MaximumEntriesPerPrinter)
            {
                entries.Dequeue();
            }
        }
    }

    public IReadOnlyList<PrinterLogEntry> GetSnapshot(Guid printerId)
    {
        lock (_gate)
        {
            return _logs.TryGetValue(printerId, out var entries)
                ? entries.ToArray()
                : [];
        }
    }

    public void Clear(Guid printerId)
    {
        lock (_gate)
        {
            _logs.Remove(printerId);
        }
    }

    public void Remove(Guid printerId) => Clear(printerId);
}
