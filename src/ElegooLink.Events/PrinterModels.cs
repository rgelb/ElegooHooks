using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElegooLink.Events;

/// <summary>The printer protocols supported by the Elegoo Link SDK.</summary>
public enum PrinterType
{
    Unknown = -1,
    ElegooFdmKlipper = 0,
    ElegooCentauriCarbon = 1,
    ElegooCentauriCarbon2 = 2,
    GenericFdmKlipper = 100
}

/// <summary>The main printer states defined by Elegoo Link.</summary>
public enum PrinterState
{
    Offline = -1,
    Idle = 0,
    Printing = 1,
    FilamentOperating = 2,
    AutoLeveling = 3,
    PidCalibrating = 4,
    ResonanceTesting = 5,
    SelfChecking = 6,
    Updating = 7,
    Homing = 8,
    FileTransferring = 9,
    FileCopying = 10,
    Preheating = 11,
    ExtruderOperating = 12,
    VideoComposing = 13,
    EmergencyStop = 14,
    PowerLossRecovery = 15,
    Initializing = 97,
    Busy = 98,
    Exception = 99,
    Unknown = 100
}

/// <summary>The detailed printer substates defined by Elegoo Link.</summary>
public enum PrinterSubState
{
    None = 0,
    Unknown = 1,

    Printing = 101,
    PrintingCompleted = 102,
    Pausing = 103,
    Paused = 104,
    Resuming = 105,
    ResumingCompleted = 106,
    Stopping = 107,
    Stopped = 108,

    Preheating = 120,
    ExtruderPreheating = 121,
    HeatedBedPreheating = 122,
    Homing = 123,
    AutoLeveling = 124,
    LoadingFilament = 125,
    UnloadingFilament = 126,
    DownloadingFile = 127,

    FilamentLoading = 201,
    FilamentLoadingCompleted = 202,
    FilamentUnloading = 203,
    FilamentUnloadingCompleted = 204,

    AutoLevelingInProgress = 301,
    AutoLevelingCompleted = 302,

    PidCalibrating = 401,
    PidCalibratingCompleted = 402,
    PidCalibratingFailed = 403,

    ResonanceTest = 501,
    ResonanceTestCompleted = 502,
    ResonanceTestFailed = 503,

    SelfCheckPidCalibrating = 601,
    SelfCheckPidCalibratingCompleted = 602,
    SelfCheckPidCalibratingFailed = 603,
    SelfCheckResonanceTest = 610,
    SelfCheckResonanceTestCompleted = 611,
    SelfCheckResonanceTestFailed = 612,
    SelfCheckAutoLeveling = 620,
    SelfCheckAutoLevelingCompleted = 621,
    SelfCheckCompleted = 699,

    Updating = 701,
    UpdatingCompleted = 702,
    UpdatingFailed = 703,

    HomingInProgress = 801,
    HomingCompleted = 802,
    HomingFailed = 803,

    UploadingFile = 901,
    UploadingFileCompleted = 902,

    CopyingFile = 1001,
    CopyingFileCompleted = 1002,

    ExtruderPreheatingInProgress = 1101,
    ExtruderPreheatingCompleted = 1102,
    HeatedBedPreheatingInProgress = 1103,
    HeatedBedPreheatingCompleted = 1104,

    ExtruderLoading = 1201,
    ExtruderLoadingCompleted = 1202,
    ExtruderUnloading = 1203,
    ExtruderUnloadingCompleted = 1204
}

/// <summary>Semantic and low-level events produced by the managed listener.</summary>
public enum PrinterEventKind
{
    Discovered,
    Connected,
    Disconnected,
    StatusUpdated,
    AttributesChanged,
    StateChanged,
    PrintStarted,
    PrintCompleted,
    PrintPausing,
    PrintPaused,
    PrintResuming,
    PrintResumed,
    PrintStopping,
    PrintStopped,
    PrintProgress,
    PrinterError,
    RawDataReceived,
    PrinterListChanged,
    RtmMessageReceived,
    RtcTokenChanged,
    LoggedInElsewhere,
    OnlineStatusChanged,
    UnknownSdkEvent
}

/// <summary>Information returned by Elegoo Link discovery.</summary>
public sealed record PrinterInfo
{
    [JsonPropertyName("printerId")]
    public string PrinterId { get; init; } = "";

    [JsonPropertyName("printerType")]
    public PrinterType PrinterType { get; init; } = PrinterType.Unknown;

    [JsonPropertyName("brand")]
    public string Brand { get; init; } = "";

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("firmwareVersion")]
    public string FirmwareVersion { get; init; } = "";

    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; init; } = "";

    [JsonPropertyName("mainboardId")]
    public string MainboardId { get; init; } = "";

    [JsonPropertyName("host")]
    public string Host { get; init; } = "";

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; init; } = "";

    [JsonPropertyName("authMode")]
    public string AuthMode { get; init; } = "";
}

/// <summary>Parameters used when Elegoo Link connects to a printer.</summary>
public sealed record PrinterConnectionOptions
{
    public string PrinterId { get; init; } = "";
    public PrinterType PrinterType { get; init; } = PrinterType.Unknown;
    public string Brand { get; init; } = "ELEGOO";
    public string Name { get; init; } = "";
    public string Model { get; init; } = "";
    public string Host { get; init; } = "";
    public string SerialNumber { get; init; } = "";
    public string WebUrl { get; init; } = "";
    public string AuthMode { get; init; } = "";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string Token { get; init; } = "";
    public string AccessCode { get; init; } = "";
    public string PinCode { get; init; } = "";
    public bool CheckConnection { get; init; } = true;
    public bool AutoReconnect { get; init; } = true;
    public int ConnectionTimeout { get; init; } = 5_000;
    public int NetworkMode { get; init; }

    public override string ToString() =>
        $"{PrinterType} at {Host} (credentials redacted)";

    public static PrinterConnectionOptions FromDiscovered(
        PrinterInfo printer,
        string? accessCode = null,
        int connectionTimeout = 5_000,
        bool autoReconnect = true)
    {
        var authMode = printer.AuthMode;
        if (!string.IsNullOrWhiteSpace(accessCode) && string.IsNullOrWhiteSpace(authMode))
        {
            authMode = "accessCode";
        }

        return new PrinterConnectionOptions
        {
            PrinterId = printer.PrinterId,
            PrinterType = printer.PrinterType,
            Brand = string.IsNullOrWhiteSpace(printer.Brand) ? "ELEGOO" : printer.Brand,
            Name = printer.Name,
            Model = printer.Model,
            Host = printer.Host,
            SerialNumber = printer.SerialNumber,
            WebUrl = printer.WebUrl,
            AuthMode = authMode,
            AccessCode = accessCode ?? "",
            ConnectionTimeout = connectionTimeout,
            AutoReconnect = autoReconnect
        };
    }
}

/// <summary>An event emitted by the listener.</summary>
public sealed class PrinterEvent : EventArgs
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required PrinterEventKind Kind { get; init; }
    public string SdkEventType { get; init; } = "";
    public string PrinterId { get; init; } = "";
    public string Message { get; init; } = "";
    public PrinterState? State { get; init; }
    public PrinterSubState? SubState { get; init; }
    public string FileName { get; init; } = "";
    public int? Progress { get; init; }
    public int? CurrentLayer { get; init; }
    public int? TotalLayers { get; init; }
    public IReadOnlyList<string> ErrorCodes { get; init; } = [];
    public bool IsInitialObservation { get; init; }

    /// <summary>The complete JSON payload produced by the native Elegoo Link bridge.</summary>
    public JsonElement Payload { get; init; }
}
