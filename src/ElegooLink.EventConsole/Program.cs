using System.Text.Json;
using System.Text.Json.Serialization;
using ElegooLink.Events;

return await ProgramEntry.RunAsync(args);

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        ListenerOptions options;
        try
        {
            options = ListenerOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"Argument error: {exception.Message}");
            Console.Error.WriteLine("Run with --help for usage.");
            return 2;
        }

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        var writer = new EventWriter(options.Json, options.Raw);
        if (options.Demo)
        {
            await RunDemoAsync(writer);
            return 0;
        }

        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            shutdown.Cancel();
        };

        try
        {
            await using var client = new ElegooLinkClient();
            client.EventReceived += (_, printerEvent) => writer.Write(printerEvent);
            client.Initialize(
                logLevel: options.NativeLogLevel,
                enableNativeConsoleLogging: options.NativeLogs);

            Console.Error.WriteLine($"Elegoo Link SDK {client.SdkVersion} initialized.");
            Console.Error.WriteLine("Press Ctrl+C to stop.");

            IReadOnlyList<PrinterConnectionOptions> connectionOptions;
            if (string.IsNullOrWhiteSpace(options.Host))
            {
                Console.Error.WriteLine($"Discovering printers for {options.DiscoveryTimeoutMs} ms...");
                var discovered = await client.DiscoverAsync(options.DiscoveryTimeoutMs, shutdown.Token);
                if (discovered.Count == 0)
                {
                    Console.Error.WriteLine(
                        "No printers were discovered. Confirm the printer is on the same LAN, or pass --host and --type.");
                    return 3;
                }

                connectionOptions = discovered
                    .Where(printer => printer.PrinterType != PrinterType.Unknown)
                    .Select(printer => PrinterConnectionOptions.FromDiscovered(
                        printer,
                        options.AccessCode,
                        options.ConnectionTimeoutMs,
                        options.AutoReconnect))
                    .ToArray();

                if (connectionOptions.Count == 0)
                {
                    Console.Error.WriteLine(
                        "Printers were found, but the SDK did not identify a supported printer type. Use --host and --type.");
                    return 4;
                }
            }
            else
            {
                if (options.PrinterType == PrinterType.Unknown)
                {
                    Console.Error.WriteLine("--type is required when --host is supplied.");
                    return 2;
                }

                connectionOptions =
                [
                    new PrinterConnectionOptions
                    {
                        Host = options.Host,
                        PrinterType = options.PrinterType,
                        Brand = "ELEGOO",
                        Name = options.Name,
                        Model = string.IsNullOrWhiteSpace(options.Model)
                            ? DefaultModel(options.PrinterType)
                            : options.Model,
                        AuthMode = string.IsNullOrWhiteSpace(options.AccessCode) ? "" : "accessCode",
                        AccessCode = options.AccessCode,
                        AutoReconnect = options.AutoReconnect,
                        ConnectionTimeout = options.ConnectionTimeoutMs
                    }
                ];
            }

            var connectedIds = new List<string>();
            foreach (var connection in connectionOptions)
            {
                shutdown.Token.ThrowIfCancellationRequested();
                Console.Error.WriteLine($"Connecting to {Target(connection)}...");
                try
                {
                    var printer = await client.ConnectAsync(connection, shutdown.Token);
                    connectedIds.Add(printer.PrinterId);
                    Console.Error.WriteLine($"Monitoring {DisplayName(printer)} ({printer.PrinterId}).");
                    await client.RefreshStatusAsync(printer.PrinterId, shutdown.Token);
                }
                catch (ElegooLinkException exception)
                {
                    Console.Error.WriteLine($"Could not connect to {Target(connection)}: {exception.Message}");
                }
            }

            if (connectedIds.Count == 0)
            {
                return 5;
            }

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
            {
                // Normal Ctrl+C shutdown.
            }

            Console.Error.WriteLine("Disconnecting...");
            foreach (var printerId in connectedIds)
            {
                try
                {
                    await client.DisconnectAsync(printerId);
                }
                catch (ElegooLinkException exception)
                {
                    Console.Error.WriteLine($"Disconnect warning for {printerId}: {exception.Message}");
                }
            }

            return 0;
        }
        catch (ElegooLinkException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            return 0;
        }
    }

    private static async Task RunDemoAsync(EventWriter writer)
    {
        var projector = new ElegooLinkEventProjector();
        var samples = new[]
        {
            Status("demo-printer", 0, 0, "", 0, [], []),
            Status("demo-printer", 1, 101, "benchy.gcode", 1, [], []),
            Status("demo-printer", 1, 104, "benchy.gcode", 47, [], []),
            Status("demo-printer", 1, 106, "benchy.gcode", 47, [], []),
            Status("demo-printer", 99, 1, "benchy.gcode", 53, [1007], ["E1007"]),
            Status("demo-printer", 0, 102, "benchy.gcode", 100, [], [])
        };

        foreach (var sample in samples)
        {
            foreach (var printerEvent in projector.Project(sample))
            {
                writer.Write(printerEvent);
            }

            await Task.Delay(150);
        }
    }

    private static string Status(
        string printerId,
        int state,
        int subState,
        string fileName,
        int progress,
        int[] exceptionCodes,
        string[] exceptions) =>
        JsonSerializer.Serialize(new
        {
            type = "printer.status",
            data = new
            {
                printerId,
                printerStatus = new
                {
                    state,
                    subState,
                    exceptionCodes,
                    supportProgress = true,
                    progress
                },
                printStatus = new
                {
                    taskId = string.IsNullOrEmpty(fileName) ? "" : "demo-task",
                    fileName,
                    progress,
                    currentLayer = progress,
                    totalLayer = 100
                },
                exceptions = exceptions.Select(code => new { code, timestamp = 0 })
            }
        });

    private static string Target(PrinterConnectionOptions options) =>
        !string.IsNullOrWhiteSpace(options.Name)
            ? $"{options.Name} at {options.Host}"
            : options.Host;

    private static string DisplayName(PrinterInfo printer) =>
        !string.IsNullOrWhiteSpace(printer.Name)
            ? printer.Name
            : !string.IsNullOrWhiteSpace(printer.Model)
                ? printer.Model
                : printer.Host;

    private static string DefaultModel(PrinterType type) => type switch
    {
        PrinterType.ElegooCentauriCarbon => "Centauri Carbon",
        PrinterType.ElegooCentauriCarbon2 => "Centauri Carbon 2",
        PrinterType.ElegooFdmKlipper => "Elegoo FDM Klipper",
        PrinterType.GenericFdmKlipper => "Generic Moonraker",
        _ => ""
    };

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            Elegoo Link event listener

            Usage:
              dotnet run --project src/ElegooLink.EventConsole -- [options]

            With no connection options, all printers discovered on the LAN are monitored.

            Options:
              --host <address>          Connect directly instead of discovering.
              --type <type>             Required with --host: cc, cc2, klipper, or moonraker.
              --model <name>            Override the model name sent to the SDK.
              --name <name>             Friendly printer name.
              --access-code <code>      Access code for printers that require one (not logged).
              --discovery-timeout <ms>  Discovery timeout (default: 5000).
              --connection-timeout <ms> Connection timeout (default: 5000).
              --no-reconnect            Disable the SDK's automatic reconnection.
              --json                    Write newline-delimited JSON events.
              --raw                     Include each full SDK JSON envelope in text output.
              --native-logs             Enable the SDK's own console logging.
              --native-log-level <0-6>  0 trace, 1 debug, 2 info ... 6 off (default: 2).
              --demo                    Show synthetic lifecycle events; no SDK/printer needed.
              --help                    Show this help.

            Examples:
              dotnet run --project src/ElegooLink.EventConsole
              dotnet run --project src/ElegooLink.EventConsole -- --host 192.168.1.42 --type cc
              dotnet run --project src/ElegooLink.EventConsole -- --host 192.168.1.43 --type cc2 --access-code 123456
              dotnet run --project src/ElegooLink.EventConsole -- --json
            """);
    }
}

internal sealed class EventWriter(bool json, bool raw)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly object _gate = new();

    public void Write(PrinterEvent printerEvent)
    {
        lock (_gate)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(printerEvent, JsonOptions));
                return;
            }

            var printer = string.IsNullOrWhiteSpace(printerEvent.PrinterId)
                ? ""
                : $" [{printerEvent.PrinterId}]";
            Console.WriteLine(
                $"[{printerEvent.TimestampUtc:O}] {Label(printerEvent.Kind),-20}{printer} {printerEvent.Message}");

            if (raw)
            {
                Console.WriteLine($"  SDK {printerEvent.SdkEventType}: {printerEvent.Payload.GetRawText()}");
            }
        }
    }

    private static string Label(PrinterEventKind kind) =>
        string.Concat(kind.ToString().Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString())).ToUpperInvariant();
}

internal sealed record ListenerOptions
{
    public bool ShowHelp { get; init; }
    public bool Demo { get; init; }
    public string Host { get; init; } = "";
    public PrinterType PrinterType { get; init; } = PrinterType.Unknown;
    public string Model { get; init; } = "";
    public string Name { get; init; } = "";
    public string AccessCode { get; init; } = "";
    public int DiscoveryTimeoutMs { get; init; } = 5_000;
    public int ConnectionTimeoutMs { get; init; } = 5_000;
    public bool AutoReconnect { get; init; } = true;
    public bool Json { get; init; }
    public bool Raw { get; init; }
    public bool NativeLogs { get; init; }
    public int NativeLogLevel { get; init; } = 2;

    public static ListenerOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected value '{argument}'.");
            }

            if (argument is "--help" or "--demo" or "--no-reconnect" or "--json" or "--raw" or "--native-logs")
            {
                flags.Add(argument);
                continue;
            }

            if (argument is not ("--host" or "--type" or "--model" or "--name" or "--access-code"
                or "--discovery-timeout" or "--connection-timeout" or "--native-log-level"))
            {
                throw new ArgumentException($"Unknown option '{argument}'.");
            }

            if (++index >= args.Length)
            {
                throw new ArgumentException($"Option '{argument}' requires a value.");
            }

            values[argument] = args[index];
        }

        var discoveryTimeout = ReadInt(values, "--discovery-timeout", 5_000, 1, 120_000);
        var connectionTimeout = ReadInt(values, "--connection-timeout", 5_000, 1, 120_000);
        var nativeLogLevel = ReadInt(values, "--native-log-level", 2, 0, 6);
        var accessCode = Get(values, "--access-code");
        if (flags.Contains("--native-logs") && !string.IsNullOrEmpty(accessCode))
        {
            throw new ArgumentException(
                "--native-logs cannot be combined with --access-code because the upstream SDK does not redact every connection parameter.");
        }

        return new ListenerOptions
        {
            ShowHelp = flags.Contains("--help"),
            Demo = flags.Contains("--demo"),
            Host = Get(values, "--host"),
            PrinterType = ParsePrinterType(Get(values, "--type")),
            Model = Get(values, "--model"),
            Name = Get(values, "--name"),
            AccessCode = accessCode,
            DiscoveryTimeoutMs = discoveryTimeout,
            ConnectionTimeoutMs = connectionTimeout,
            AutoReconnect = !flags.Contains("--no-reconnect"),
            Json = flags.Contains("--json"),
            Raw = flags.Contains("--raw"),
            NativeLogs = flags.Contains("--native-logs"),
            NativeLogLevel = nativeLogLevel
        };
    }

    private static PrinterType ParsePrinterType(string value) => value.ToLowerInvariant() switch
    {
        "" or "auto" => PrinterType.Unknown,
        "cc" or "centauri-carbon" => PrinterType.ElegooCentauriCarbon,
        "cc2" or "centauri-carbon-2" => PrinterType.ElegooCentauriCarbon2,
        "klipper" or "elegoo-klipper" => PrinterType.ElegooFdmKlipper,
        "moonraker" or "generic-klipper" => PrinterType.GenericFdmKlipper,
        _ => throw new ArgumentException(
            $"Unknown printer type '{value}'. Use cc, cc2, klipper, or moonraker.")
    };

    private static string Get(IReadOnlyDictionary<string, string> values, string name) =>
        values.TryGetValue(name, out var value) ? value : "";

    private static int ReadInt(
        IReadOnlyDictionary<string, string> values,
        string name,
        int defaultValue,
        int minimum,
        int maximum)
    {
        var text = Get(values, name);
        if (string.IsNullOrEmpty(text))
        {
            return defaultValue;
        }

        if (!int.TryParse(text, out var value) || value < minimum || value > maximum)
        {
            throw new ArgumentException($"{name} must be an integer from {minimum} to {maximum}.");
        }

        return value;
    }
}
