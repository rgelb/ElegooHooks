using System.Text.Json;
using ElegooLink.Events;
using Xunit;

namespace ElegooLink.Events.Tests;

public sealed class ElegooLinkEventProjectorTests
{
    [Fact]
    public void Projects_print_lifecycle_once_per_transition()
    {
        var projector = new ElegooLinkEventProjector();

        var idle = projector.Project(Status(PrinterState.Idle, PrinterSubState.None, 0));
        var started = projector.Project(Status(PrinterState.Printing, PrinterSubState.Printing, 1));
        var duplicate = projector.Project(Status(PrinterState.Printing, PrinterSubState.Printing, 1));
        var paused = projector.Project(Status(PrinterState.Printing, PrinterSubState.Paused, 42));
        var resumed = projector.Project(Status(PrinterState.Printing, PrinterSubState.ResumingCompleted, 42));
        var completed = projector.Project(Status(PrinterState.Idle, PrinterSubState.PrintingCompleted, 100));

        Assert.DoesNotContain(idle, item => item.Kind == PrinterEventKind.PrintProgress);
        Assert.Contains(started, item => item.Kind == PrinterEventKind.PrintStarted);
        Assert.Contains(started, item => item.Kind == PrinterEventKind.PrintProgress);
        Assert.DoesNotContain(duplicate, item => item.Kind == PrinterEventKind.PrintStarted);
        Assert.DoesNotContain(duplicate, item => item.Kind == PrinterEventKind.PrintProgress);
        Assert.Contains(paused, item => item.Kind == PrinterEventKind.PrintPaused);
        Assert.Contains(resumed, item => item.Kind == PrinterEventKind.PrintResumed);
        Assert.DoesNotContain(resumed, item => item.Kind == PrinterEventKind.PrintStarted);
        Assert.Contains(completed, item => item.Kind == PrinterEventKind.PrintCompleted);
    }

    [Fact]
    public void Projects_only_new_error_codes()
    {
        var projector = new ElegooLinkEventProjector();

        projector.Project(Status(PrinterState.Printing, PrinterSubState.Printing, 20));
        var firstError = projector.Project(
            Status(PrinterState.Exception, PrinterSubState.Unknown, 20, [1007], ["E1007"]));
        var duplicateError = projector.Project(
            Status(PrinterState.Exception, PrinterSubState.Unknown, 20, [1007], ["E1007"]));
        var addedError = projector.Project(
            Status(PrinterState.Exception, PrinterSubState.Unknown, 20, [1007, 1008], ["E1007", "E1008"]));

        var first = Assert.Single(firstError, item => item.Kind == PrinterEventKind.PrinterError);
        Assert.Equal(["1007", "E1007"], first.ErrorCodes);
        Assert.DoesNotContain(duplicateError, item => item.Kind == PrinterEventKind.PrinterError);
        var added = Assert.Single(addedError, item => item.Kind == PrinterEventKind.PrinterError);
        Assert.Contains("1008", added.Message);
        Assert.Contains("E1008", added.Message);
    }

    [Theory]
    [InlineData(0, PrinterEventKind.Disconnected)]
    [InlineData(1, PrinterEventKind.Connected)]
    public void Projects_connection_hooks(int status, PrinterEventKind expected)
    {
        var projector = new ElegooLinkEventProjector();
        var json = JsonSerializer.Serialize(new
        {
            type = "printer.connection",
            data = new { printerId = "printer-1", status }
        });

        var projected = projector.Project(json);

        Assert.Equal(expected, Assert.Single(projected).Kind);
    }

    [Fact]
    public void Preserves_unknown_sdk_events()
    {
        var projector = new ElegooLinkEventProjector();

        var projected = projector.Project("""{"type":"future.event","data":{"printerId":"p1","answer":42}}""");

        var printerEvent = Assert.Single(projected);
        Assert.Equal(PrinterEventKind.UnknownSdkEvent, printerEvent.Kind);
        Assert.Equal(42, printerEvent.Payload.GetProperty("data").GetProperty("answer").GetInt32());
    }

    [Theory]
    [InlineData("printer.attributes", PrinterEventKind.AttributesChanged)]
    [InlineData("printer.raw", PrinterEventKind.RawDataReceived)]
    [InlineData("printer.list.changed", PrinterEventKind.PrinterListChanged)]
    [InlineData("rtm.message", PrinterEventKind.RtmMessageReceived)]
    [InlineData("rtc.token.changed", PrinterEventKind.RtcTokenChanged)]
    [InlineData("user.logged.elsewhere", PrinterEventKind.LoggedInElsewhere)]
    [InlineData("user.online.status", PrinterEventKind.OnlineStatusChanged)]
    public void Projects_every_other_published_sdk_hook(string type, PrinterEventKind expected)
    {
        var projector = new ElegooLinkEventProjector();
        var json = JsonSerializer.Serialize(new
        {
            type,
            data = new { printerId = "printer-1", isOnline = true }
        });

        var projected = projector.Project(json);

        Assert.Equal(expected, Assert.Single(projected).Kind);
    }

    private static string Status(
        PrinterState state,
        PrinterSubState subState,
        int progress,
        int[]? exceptionCodes = null,
        string[]? exceptions = null) =>
        JsonSerializer.Serialize(new
        {
            type = "printer.status",
            data = new
            {
                printerId = "printer-1",
                printerStatus = new
                {
                    state = (int)state,
                    subState = (int)subState,
                    exceptionCodes = exceptionCodes ?? [],
                    supportProgress = true,
                    progress
                },
                printStatus = new
                {
                    taskId = state == PrinterState.Idle && subState == PrinterSubState.None ? "" : "task-1",
                    fileName = "part.gcode",
                    progress,
                    currentLayer = progress,
                    totalLayer = 100
                },
                exceptions = (exceptions ?? []).Select(code => new { code, timestamp = 0 })
            }
        });
}
