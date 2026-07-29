using ElegooLink.Events;

namespace ElegooLink.Desktop.Core;

public sealed class PrinterEventRouter
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Guid> _printerIds =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, string> _reversePrinterIds = [];
    private Guid? _pendingPrinter;

    public void RegisterKnownPrinter(Guid localPrinterId, string? sdkPrinterId)
    {
        if (!string.IsNullOrWhiteSpace(sdkPrinterId))
        {
            Map(localPrinterId, sdkPrinterId);
        }
    }

    public void BeginConnection(Guid localPrinterId)
    {
        lock (_gate)
        {
            if (_pendingPrinter is not null)
            {
                throw new InvalidOperationException(
                    "Printer connection attempts must be serialized.");
            }

            _pendingPrinter = localPrinterId;
        }
    }

    public void CompleteConnection(Guid localPrinterId, string sdkPrinterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdkPrinterId);
        lock (_gate)
        {
            MapCore(localPrinterId, sdkPrinterId);
            if (_pendingPrinter == localPrinterId)
            {
                _pendingPrinter = null;
            }
        }
    }

    public void EndConnection(Guid localPrinterId)
    {
        lock (_gate)
        {
            if (_pendingPrinter == localPrinterId)
            {
                _pendingPrinter = null;
            }
        }
    }

    public bool TryRoute(PrinterEvent printerEvent, out Guid localPrinterId)
    {
        ArgumentNullException.ThrowIfNull(printerEvent);
        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(printerEvent.PrinterId) &&
                _printerIds.TryGetValue(printerEvent.PrinterId, out localPrinterId))
            {
                return true;
            }

            if (_pendingPrinter is { } pending &&
                !string.IsNullOrWhiteSpace(printerEvent.PrinterId))
            {
                MapCore(pending, printerEvent.PrinterId);
                localPrinterId = pending;
                return true;
            }
        }

        localPrinterId = Guid.Empty;
        return false;
    }

    public string? GetSdkPrinterId(Guid localPrinterId)
    {
        lock (_gate)
        {
            return _reversePrinterIds.GetValueOrDefault(localPrinterId);
        }
    }

    public void Remove(Guid localPrinterId)
    {
        lock (_gate)
        {
            if (_reversePrinterIds.Remove(localPrinterId, out var sdkPrinterId))
            {
                _printerIds.Remove(sdkPrinterId);
            }

            if (_pendingPrinter == localPrinterId)
            {
                _pendingPrinter = null;
            }
        }
    }

    private void Map(Guid localPrinterId, string sdkPrinterId)
    {
        lock (_gate)
        {
            MapCore(localPrinterId, sdkPrinterId);
        }
    }

    private void MapCore(Guid localPrinterId, string sdkPrinterId)
    {
        if (_reversePrinterIds.Remove(localPrinterId, out var previousSdkPrinterId))
        {
            _printerIds.Remove(previousSdkPrinterId);
        }

        _printerIds[sdkPrinterId] = localPrinterId;
        _reversePrinterIds[localPrinterId] = sdkPrinterId;
    }
}
