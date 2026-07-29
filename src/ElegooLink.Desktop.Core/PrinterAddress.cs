using System.Net;
using ElegooLink.Events;

namespace ElegooLink.Desktop.Core;

public static class PrinterAddress
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        var candidate = value?.Trim() ?? "";
        if (candidate.Length > 2 &&
            candidate[0] == '[' &&
            candidate[^1] == ']')
        {
            candidate = candidate[1..^1];
        }

        if (IPAddress.TryParse(candidate, out var address))
        {
            normalized = address.ToString();
            return true;
        }

        normalized = "";
        return false;
    }

    public static bool AreEqual(string? first, string? second) =>
        TryNormalize(first, out var normalizedFirst) &&
        TryNormalize(second, out var normalizedSecond) &&
        string.Equals(normalizedFirst, normalizedSecond, StringComparison.OrdinalIgnoreCase);

    public static bool IsDuplicate(IEnumerable<SavedPrinter> printers, string host) =>
        printers.Any(printer => AreEqual(printer.Host, host));

    public static PrinterInfo? FindDiscovered(
        IEnumerable<PrinterInfo> printers,
        string host) =>
        printers.FirstOrDefault(printer => AreEqual(printer.Host, host));
}
