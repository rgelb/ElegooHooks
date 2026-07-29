using System.Text.Json;

namespace ElegooLink.Events;

/// <summary>
/// Converts the SDK's typed status stream into convenient lifecycle events while preserving
/// each original SDK event.
/// </summary>
public sealed class ElegooLinkEventProjector
{
    private readonly object _gate = new();
    private readonly Dictionary<string, PrinterTracker> _trackers = new(StringComparer.Ordinal);

    public IReadOnlyList<PrinterEvent> Project(string nativeEnvelopeJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeEnvelopeJson);

        using var document = JsonDocument.Parse(nativeEnvelopeJson);
        var root = document.RootElement;
        var payload = root.Clone();
        var sdkType = GetString(root, "type");
        var data = TryGetProperty(root, "data", out var dataElement)
            ? dataElement
            : default;
        var timestamp = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            return sdkType switch
            {
                "printer.connection" => ProjectConnection(data, payload, sdkType, timestamp),
                "printer.status" => ProjectStatus(data, payload, sdkType, timestamp),
                "printer.attributes" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.AttributesChanged,
                        sdkType,
                        GetString(data, "printerId"),
                        "Printer attributes changed.",
                        payload)
                ],
                "printer.raw" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.RawDataReceived,
                        sdkType,
                        GetString(data, "printerId"),
                        "Raw printer data received.",
                        payload)
                ],
                "printer.list.changed" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.PrinterListChanged,
                        sdkType,
                        "",
                        "The printer list changed.",
                        payload)
                ],
                "rtm.message" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.RtmMessageReceived,
                        sdkType,
                        GetString(data, "printerId"),
                        "An RTM message was received.",
                        payload)
                ],
                "rtc.token.changed" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.RtcTokenChanged,
                        sdkType,
                        "",
                        "The RTC token changed (the token value is redacted by the bridge).",
                        payload)
                ],
                "user.logged.elsewhere" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.LoggedInElsewhere,
                        sdkType,
                        "",
                        "The Elegoo account was used to log in elsewhere.",
                        payload)
                ],
                "user.online.status" =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.OnlineStatusChanged,
                        sdkType,
                        "",
                        GetBoolean(data, "isOnline") ? "Elegoo cloud is online." : "Elegoo cloud is offline.",
                        payload)
                ],
                _ =>
                [
                    Create(
                        timestamp,
                        PrinterEventKind.UnknownSdkEvent,
                        sdkType,
                        GetString(data, "printerId"),
                        $"Unrecognized SDK event '{sdkType}'.",
                        payload)
                ]
            };
        }
    }

    public PrinterEvent CreateDiscovered(PrinterInfo printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        var payload = JsonSerializer.SerializeToElement(printer);

        return new PrinterEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Kind = PrinterEventKind.Discovered,
            SdkEventType = "printer.discovered",
            PrinterId = printer.PrinterId,
            Message = $"Discovered {DisplayName(printer)} at {printer.Host}.",
            Payload = payload
        };
    }

    private IReadOnlyList<PrinterEvent> ProjectConnection(
        JsonElement data,
        JsonElement payload,
        string sdkType,
        DateTimeOffset timestamp)
    {
        var printerId = GetString(data, "printerId");
        var connected = GetInt32(data, "status") == 1;
        if (!connected && !string.IsNullOrEmpty(printerId))
        {
            _trackers.Remove(printerId);
        }

        return
        [
            Create(
                timestamp,
                connected ? PrinterEventKind.Connected : PrinterEventKind.Disconnected,
                sdkType,
                printerId,
                connected ? "Printer connected." : "Printer disconnected.",
                payload)
        ];
    }

    private IReadOnlyList<PrinterEvent> ProjectStatus(
        JsonElement data,
        JsonElement payload,
        string sdkType,
        DateTimeOffset timestamp)
    {
        var events = new List<PrinterEvent>();
        var printerId = GetString(data, "printerId");
        var printerStatus = GetObject(data, "printerStatus");
        var printStatus = GetObject(data, "printStatus");

        var state = (PrinterState)GetInt32(printerStatus, "state", (int)PrinterState.Unknown);
        var subState = (PrinterSubState)GetInt32(printerStatus, "subState");
        var taskId = GetString(printStatus, "taskId");
        var fileName = GetString(printStatus, "fileName");
        var progress = TryGetInt32(printStatus, "progress")
            ?? TryGetInt32(printerStatus, "progress");
        var currentLayer = TryGetInt32(printStatus, "currentLayer");
        var totalLayers = TryGetInt32(printStatus, "totalLayer");
        var errorCodes = ReadErrorCodes(printerStatus, data);

        _trackers.TryGetValue(printerId, out var previous);
        var initial = previous is null;
        var summary = BuildStatusSummary(state, subState, fileName, progress, currentLayer, totalLayers);

        events.Add(CreateStatusEvent(
            timestamp,
            PrinterEventKind.StatusUpdated,
            sdkType,
            printerId,
            summary,
            state,
            subState,
            fileName,
            progress,
            currentLayer,
            totalLayers,
            errorCodes,
            initial,
            payload));

        if (initial || previous!.State != state || previous.SubState != subState)
        {
            events.Add(CreateStatusEvent(
                timestamp,
                PrinterEventKind.StateChanged,
                sdkType,
                printerId,
                initial
                    ? $"Initial state: {Friendly(state)} / {Friendly(subState)}."
                    : $"State changed from {Friendly(previous!.State)} / {Friendly(previous.SubState)} to {Friendly(state)} / {Friendly(subState)}.",
                state,
                subState,
                fileName,
                progress,
                currentLayer,
                totalLayers,
                errorCodes,
                initial,
                payload));
        }

        var hasActivePrint = previous?.HasActivePrint ?? false;
        var newTask = !string.IsNullOrEmpty(taskId)
            && previous is not null
            && !string.IsNullOrEmpty(previous.TaskId)
            && !string.Equals(previous.TaskId, taskId, StringComparison.Ordinal);
        var printIsActive = IsActivePrintState(state, subState);

        if ((!hasActivePrint && printIsActive) || (newTask && printIsActive))
        {
            events.Add(CreateStatusEvent(
                timestamp,
                PrinterEventKind.PrintStarted,
                sdkType,
                printerId,
                initial ? $"Print already in progress: {File(fileName)}." : $"Print started: {File(fileName)}.",
                state,
                subState,
                fileName,
                progress,
                currentLayer,
                totalLayers,
                errorCodes,
                initial,
                payload));
            hasActivePrint = true;
        }

        AddLifecycleTransition(
            events,
            previous,
            subState,
            PrinterSubState.PrintingCompleted,
            PrinterEventKind.PrintCompleted,
            $"Print completed: {File(fileName)}.",
            timestamp,
            sdkType,
            printerId,
            state,
            fileName,
            progress,
            currentLayer,
            totalLayers,
            errorCodes,
            payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.Pausing, PrinterEventKind.PrintPausing,
            "Print is pausing.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.Paused, PrinterEventKind.PrintPaused,
            "Print paused.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.Resuming, PrinterEventKind.PrintResuming,
            "Print is resuming.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.ResumingCompleted, PrinterEventKind.PrintResumed,
            "Print resumed.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.Stopping, PrinterEventKind.PrintStopping,
            "Print is stopping.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);
        AddLifecycleTransition(events, previous, subState, PrinterSubState.Stopped, PrinterEventKind.PrintStopped,
            $"Print stopped: {File(fileName)}.", timestamp, sdkType, printerId, state, fileName, progress, currentLayer, totalLayers, errorCodes, payload);

        if (progress is not null
            && (hasActivePrint || printIsActive)
            && (initial || previous!.Progress != progress))
        {
            events.Add(CreateStatusEvent(
                timestamp,
                PrinterEventKind.PrintProgress,
                sdkType,
                printerId,
                $"Print progress: {progress}%.",
                state,
                subState,
                fileName,
                progress,
                currentLayer,
                totalLayers,
                errorCodes,
                initial,
                payload));
        }

        var newErrorCodes = previous is null
            ? errorCodes
            : errorCodes.Where(code => !previous.ErrorCodes.Contains(code)).ToArray();
        var enteredErrorState = IsErrorState(state)
            && (previous is null || !IsErrorState(previous.State));
        var enteredFailureSubState = IsFailureSubState(subState)
            && (previous is null || previous.SubState != subState);

        if (newErrorCodes.Count > 0 || enteredErrorState || enteredFailureSubState)
        {
            var detail = newErrorCodes.Count > 0
                ? $" Error code(s): {string.Join(", ", newErrorCodes)}."
                : "";
            events.Add(CreateStatusEvent(
                timestamp,
                PrinterEventKind.PrinterError,
                sdkType,
                printerId,
                $"Printer error: {Friendly(state)} / {Friendly(subState)}.{detail}".Replace("..", ".", StringComparison.Ordinal),
                state,
                subState,
                fileName,
                progress,
                currentLayer,
                totalLayers,
                errorCodes,
                initial,
                payload));
        }

        if (subState is PrinterSubState.PrintingCompleted or PrinterSubState.Stopped
            || state == PrinterState.Idle && subState == PrinterSubState.None)
        {
            hasActivePrint = false;
        }

        _trackers[printerId] = new PrinterTracker(
            state,
            subState,
            taskId,
            progress,
            hasActivePrint,
            errorCodes.ToHashSet(StringComparer.Ordinal));

        return events;
    }

    private static void AddLifecycleTransition(
        ICollection<PrinterEvent> events,
        PrinterTracker? previous,
        PrinterSubState actual,
        PrinterSubState expected,
        PrinterEventKind kind,
        string message,
        DateTimeOffset timestamp,
        string sdkType,
        string printerId,
        PrinterState state,
        string fileName,
        int? progress,
        int? currentLayer,
        int? totalLayers,
        IReadOnlyList<string> errorCodes,
        JsonElement payload)
    {
        if (actual != expected || previous?.SubState == expected)
        {
            return;
        }

        events.Add(CreateStatusEvent(
            timestamp,
            kind,
            sdkType,
            printerId,
            message,
            state,
            actual,
            fileName,
            progress,
            currentLayer,
            totalLayers,
            errorCodes,
            previous is null,
            payload));
    }

    private static PrinterEvent CreateStatusEvent(
        DateTimeOffset timestamp,
        PrinterEventKind kind,
        string sdkType,
        string printerId,
        string message,
        PrinterState state,
        PrinterSubState subState,
        string fileName,
        int? progress,
        int? currentLayer,
        int? totalLayers,
        IReadOnlyList<string> errorCodes,
        bool initial,
        JsonElement payload) =>
        new()
        {
            TimestampUtc = timestamp,
            Kind = kind,
            SdkEventType = sdkType,
            PrinterId = printerId,
            Message = message,
            State = state,
            SubState = subState,
            FileName = fileName,
            Progress = progress,
            CurrentLayer = currentLayer,
            TotalLayers = totalLayers,
            ErrorCodes = errorCodes,
            IsInitialObservation = initial,
            Payload = payload
        };

    private static PrinterEvent Create(
        DateTimeOffset timestamp,
        PrinterEventKind kind,
        string sdkType,
        string printerId,
        string message,
        JsonElement payload) =>
        new()
        {
            TimestampUtc = timestamp,
            Kind = kind,
            SdkEventType = sdkType,
            PrinterId = printerId,
            Message = message,
            Payload = payload
        };

    private static IReadOnlyList<string> ReadErrorCodes(JsonElement printerStatus, JsonElement data)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);

        if (TryGetProperty(printerStatus, "exceptionCodes", out var exceptionCodes)
            && exceptionCodes.ValueKind == JsonValueKind.Array)
        {
            foreach (var code in exceptionCodes.EnumerateArray())
            {
                codes.Add(code.ToString());
            }
        }

        if (TryGetProperty(data, "exceptions", out var exceptions)
            && exceptions.ValueKind == JsonValueKind.Array)
        {
            foreach (var exception in exceptions.EnumerateArray())
            {
                var code = GetString(exception, "code");
                if (!string.IsNullOrWhiteSpace(code))
                {
                    codes.Add(code);
                }
            }
        }

        return codes.Order(StringComparer.Ordinal).ToArray();
    }

    private static bool IsActivePrintState(PrinterState state, PrinterSubState subState) =>
        subState is >= PrinterSubState.Printing and <= PrinterSubState.Stopping
        && subState is not PrinterSubState.PrintingCompleted
        || state == PrinterState.Printing
        && subState is not PrinterSubState.PrintingCompleted and not PrinterSubState.Stopped;

    private static bool IsErrorState(PrinterState state) =>
        state is PrinterState.Exception or PrinterState.EmergencyStop;

    private static bool IsFailureSubState(PrinterSubState subState) =>
        subState is PrinterSubState.PidCalibratingFailed
            or PrinterSubState.ResonanceTestFailed
            or PrinterSubState.SelfCheckPidCalibratingFailed
            or PrinterSubState.SelfCheckResonanceTestFailed
            or PrinterSubState.UpdatingFailed
            or PrinterSubState.HomingFailed;

    private static string BuildStatusSummary(
        PrinterState state,
        PrinterSubState subState,
        string fileName,
        int? progress,
        int? currentLayer,
        int? totalLayers)
    {
        var parts = new List<string> { $"{Friendly(state)} / {Friendly(subState)}" };
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            parts.Add(fileName);
        }

        if (progress is not null)
        {
            parts.Add($"{progress}%");
        }

        if (currentLayer is > 0 || totalLayers is > 0)
        {
            parts.Add($"layer {currentLayer ?? 0}/{totalLayers ?? 0}");
        }

        return string.Join(" | ", parts);
    }

    private static string DisplayName(PrinterInfo printer) =>
        !string.IsNullOrWhiteSpace(printer.Name)
            ? printer.Name
            : !string.IsNullOrWhiteSpace(printer.Model)
                ? printer.Model
                : "Elegoo printer";

    private static string File(string fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? "(unknown file)" : fileName;

    private static string Friendly<T>(T value) where T : struct, Enum =>
        Enum.GetName(value)?.Replace('_', ' ') ?? Convert.ToInt32(value).ToString();

    private static JsonElement GetObject(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : default;

    private static string GetString(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static bool GetBoolean(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
        && value.GetBoolean();

    private static int GetInt32(JsonElement element, string name, int fallback = 0) =>
        TryGetInt32(element, name) ?? fallback;

    private static int? TryGetInt32(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), out number)
                ? number
                : null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private sealed record PrinterTracker(
        PrinterState State,
        PrinterSubState SubState,
        string TaskId,
        int? Progress,
        bool HasActivePrint,
        HashSet<string> ErrorCodes);
}
