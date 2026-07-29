using System.Text.Json;
using ElegooLink.Desktop.Core;
using ElegooLink.Events;
using Xunit;

namespace ElegooLink.Events.Tests;

public sealed class DesktopCoreTests
{
    [Fact]
    public void Defaults_include_each_actionable_event_once()
    {
        var settings = SettingsNormalizer.Normalize(null);

        Assert.Equal(
            AutomationCatalog.ActionableEvents,
            settings.EventActions.Select(rule => rule.EventKind));
        Assert.All(settings.EventActions, rule =>
        {
            Assert.False(rule.Enabled);
            Assert.True(rule.RunHidden);
        });
    }

    [Fact]
    public void Normalizer_removes_duplicate_hosts_and_repairs_ids()
    {
        var settings = new AppSettings
        {
            Printers =
            [
                new SavedPrinter { Id = Guid.Empty, Host = " 192.168.1.20 " },
                new SavedPrinter { Id = Guid.Empty, Host = "192.168.1.20" },
                new SavedPrinter { Host = "not-an-ip" }
            ]
        };

        var normalized = SettingsNormalizer.Normalize(settings);

        var printer = Assert.Single(normalized.Printers);
        Assert.NotEqual(Guid.Empty, printer.Id);
        Assert.Equal("192.168.1.20", printer.Host);
    }

    [Fact]
    public async Task Settings_round_trip_and_leave_no_temporary_file()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "settings.json");
            var store = new JsonSettingsStore(path);
            var settings = new AppSettings
            {
                Printers =
                [
                    new SavedPrinter
                    {
                        Host = "192.168.1.42",
                        DisplayName = "Garage printer",
                        PrinterType = PrinterType.ElegooCentauriCarbon
                    }
                ]
            };

            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();

            Assert.Null(loaded.Warning);
            Assert.Equal("Garage printer", Assert.Single(loaded.Settings.Printers).DisplayName);
            Assert.Equal(
                PrinterType.ElegooCentauriCarbon,
                loaded.Settings.Printers[0].PrinterType);
            Assert.Equal(
                ["settings.json"],
                Directory.GetFiles(directory).Select(Path.GetFileName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Malformed_settings_are_preserved_with_bad_suffix()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "settings.json");
            await File.WriteAllTextAsync(path, "{this is not json");
            var store = new JsonSettingsStore(path);

            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded.Warning);
            Assert.NotNull(loaded.PreservedBadPath);
            Assert.EndsWith(".bad", loaded.PreservedBadPath, StringComparison.Ordinal);
            Assert.True(File.Exists(loaded.PreservedBadPath));
            Assert.False(File.Exists(path));
            Assert.Empty(loaded.Settings.Printers);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Address_matching_normalizes_ipv4_and_ipv6()
    {
        Assert.True(PrinterAddress.AreEqual(" 192.168.1.10 ", "192.168.1.10"));
        Assert.True(PrinterAddress.AreEqual("[2001:db8::1]", "2001:0db8:0:0:0:0:0:1"));

        var discovered = new[]
        {
            new PrinterInfo
            {
                Host = "192.168.1.33",
                PrinterId = "printer-33"
            }
        };
        Assert.Equal(
            "printer-33",
            PrinterAddress.FindDiscovered(discovered, "192.168.1.33")?.PrinterId);
    }

    [Fact]
    public void Log_store_is_isolated_and_bounded_per_printer()
    {
        var store = new PrinterLogStore(maximumEntriesPerPrinter: 2);
        var firstPrinter = Guid.NewGuid();
        var secondPrinter = Guid.NewGuid();

        store.Add(firstPrinter, PrinterLogEntry.Application("One", "one"));
        store.Add(firstPrinter, PrinterLogEntry.Application("Two", "two"));
        store.Add(firstPrinter, PrinterLogEntry.Application("Three", "three"));
        store.Add(secondPrinter, PrinterLogEntry.Application("Other", "other"));

        Assert.Equal(
            ["Two", "Three"],
            store.GetSnapshot(firstPrinter).Select(entry => entry.EventName));
        Assert.Equal(
            "Other",
            Assert.Single(store.GetSnapshot(secondPrinter)).EventName);
    }

    [Fact]
    public void Event_router_uses_known_and_pending_printer_ids()
    {
        var router = new PrinterEventRouter();
        var known = Guid.NewGuid();
        var pending = Guid.NewGuid();
        router.RegisterKnownPrinter(known, "sdk-known");

        Assert.True(router.TryRoute(Event("sdk-known"), out var knownResult));
        Assert.Equal(known, knownResult);

        router.BeginConnection(pending);
        Assert.True(router.TryRoute(Event("sdk-new"), out var pendingResult));
        Assert.Equal(pending, pendingResult);
        router.CompleteConnection(pending, "sdk-new");
        Assert.Equal("sdk-new", router.GetSdkPrinterId(pending));
    }

    [Fact]
    public void Argument_templates_expand_every_supported_placeholder()
    {
        var printer = new SavedPrinter
        {
            Host = "192.168.1.42",
            DisplayName = "Garage Printer"
        };
        var printerEvent = Event(
            "printer-1",
            PrinterEventKind.PrintCompleted,
            fileName: "part one.gcode",
            progress: 100,
            currentLayer: 20,
            totalLayers: 20,
            state: PrinterState.Idle,
            subState: PrinterSubState.PrintingCompleted,
            errorCodes: ["E1"]);

        var expanded = ArgumentTemplateExpander.Expand(
            "{PrinterId}|{PrinterName}|{PrinterIp}|{Event}|{TimestampUtc}|" +
            "{Message}|{FileName}|{Progress}|{CurrentLayer}|{TotalLayers}|" +
            "{State}|{SubState}|{ErrorCodes}",
            printer,
            printerEvent);

        Assert.Contains(
            "printer-1|Garage Printer|192.168.1.42|PrintCompleted|",
            expanded,
            StringComparison.Ordinal);
        Assert.EndsWith(
            "|Printer message|part one.gcode|100|20|20|Idle|PrintingCompleted|E1",
            expanded,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Automation_launches_enabled_rule_and_skips_initial_events()
    {
        var launcher = new FakeProcessLauncher();
        var rule = new EventActionRule
        {
            EventKind = PrinterEventKind.PrintCompleted,
            Enabled = true,
            ExecutablePath = @"C:\Tools\done.exe",
            ArgumentsTemplate = "--printer \"{PrinterName}\" --file \"{FileName}\"",
            WorkingDirectory = @"C:\Tools",
            RunHidden = true
        };
        await using var engine = new AutomationEngine(launcher, [rule]);
        var printer = new SavedPrinter
        {
            Host = "192.168.1.42",
            DisplayName = "Garage Printer"
        };

        Assert.True(engine.Enqueue(
            printer,
            Event(
                "printer-1",
                PrinterEventKind.PrintCompleted,
                fileName: "part.gcode")));
        Assert.False(engine.Enqueue(
            printer,
            Event(
                "printer-1",
                PrinterEventKind.PrintCompleted,
                fileName: "old.gcode",
                isInitial: true)));

        await engine.CompleteAsync();

        var request = Assert.Single(launcher.Requests);
        Assert.Equal(@"C:\Tools\done.exe", request.ExecutablePath);
        Assert.Equal(
            "--printer \"Garage Printer\" --file \"part.gcode\"",
            request.Arguments);
        Assert.Equal(@"C:\Tools", request.WorkingDirectory);
        Assert.True(request.RunHidden);
    }

    [Fact]
    public async Task Automation_suppresses_duplicate_connection_state()
    {
        var launcher = new FakeProcessLauncher();
        var rule = new EventActionRule
        {
            EventKind = PrinterEventKind.Connected,
            Enabled = true,
            ExecutablePath = @"C:\Tools\connected.exe"
        };
        await using var engine = new AutomationEngine(launcher, [rule]);
        var printer = new SavedPrinter { Host = "192.168.1.42" };
        var connected = Event("printer-1", PrinterEventKind.Connected);

        Assert.True(engine.Enqueue(printer, connected));
        Assert.False(engine.Enqueue(printer, connected));
        await engine.CompleteAsync();

        Assert.Single(launcher.Requests);
    }

    private static PrinterEvent Event(
        string printerId,
        PrinterEventKind kind = PrinterEventKind.StatusUpdated,
        string fileName = "",
        int? progress = null,
        int? currentLayer = null,
        int? totalLayers = null,
        PrinterState? state = null,
        PrinterSubState? subState = null,
        IReadOnlyList<string>? errorCodes = null,
        bool isInitial = false)
    {
        using var payload = JsonDocument.Parse("""{"data":{"test":true}}""");
        return new PrinterEvent
        {
            TimestampUtc = new DateTimeOffset(2026, 7, 28, 18, 0, 0, TimeSpan.Zero),
            Kind = kind,
            PrinterId = printerId,
            Message = "Printer message",
            FileName = fileName,
            Progress = progress,
            CurrentLayer = currentLayer,
            TotalLayers = totalLayers,
            State = state,
            SubState = subState,
            ErrorCodes = errorCodes ?? [],
            IsInitialObservation = isInitial,
            Payload = payload.RootElement.Clone()
        };
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"ElegooHooks.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public List<ProcessLaunchRequest> Requests { get; } = [];

        public Task<ProcessLaunchResult> LaunchAsync(
            ProcessLaunchRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new ProcessLaunchResult(123));
        }
    }
}
