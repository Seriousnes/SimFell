using System.Text.Json;
using System.Threading.Channels;
using FellowshipSim.Engine;
using Microsoft.AspNetCore.Mvc;

namespace FellowshipSim.EngineServer.Controllers;

/// <summary>Monte Carlo sims of a profile, with their progress.</summary>
[ApiController]
public sealed class SimController(ISimEngine engine, ILogger<SimController> log) : ControllerBase
{
    /// <summary>The fewest milliseconds between two progress lines.</summary>
    private const int ProgressIntervalMs = 100;

    /// <summary>
    /// Runs a sim of the request's profile and streams <see cref="SimStreamLine"/>s: progress lines, then the result or
    /// the error. A request with no fights or no duration is refused (400) before the stream starts. Closing the
    /// connection cancels the sim.
    /// </summary>
    [HttpPost("sims")]
    public async Task Simulate([FromBody] SimRequest request)
    {
        var ct = HttpContext.RequestAborted;
        if (request.Settings.Iterations <= 0 || request.Settings.DurationSeconds <= 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new EngineError("A sim needs at least one fight and a duration above 0 seconds."), ct);
            return;
        }

        Response.ContentType = SimStreamLine.ContentType;
        // Fights report on their own threads. The channel keeps the latest report, and this request's thread writes it.
        var reports = Channel.CreateBounded<SimProgress>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
        var sim = engine.SimulateAsync(request.Profile, request.Settings, new ChannelProgress(reports.Writer), ct);
        _ = sim.ContinueWith(_ => reports.Writer.TryComplete(), TaskScheduler.Default);

        await foreach (var progress in reports.Reader.ReadAllAsync(ct))
        {
            await WriteLine(new SimStreamLine(Progress: progress), ct);
            await Task.Delay(ProgressIntervalMs, ct);
        }

        SimStreamLine last;
        try
        {
            last = new SimStreamLine(Result: await sim);
        }
        catch (SimEngineException exception)
        {
            last = new SimStreamLine(Error: exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.LogError(exception, "Sim of {Hero} failed", request.Profile.Build.Hero);
            last = new SimStreamLine(Error: $"{exception.GetType().Name}: {exception.Message}");
        }
        await WriteLine(last, ct);
    }

    private async Task WriteLine(SimStreamLine line, CancellationToken ct)
    {
        await Response.Body.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(line, SimStreamLine.Json), ct);
        await Response.Body.WriteAsync("\n"u8.ToArray(), ct);
        await Response.Body.FlushAsync(ct);
    }

    private sealed class ChannelProgress(ChannelWriter<SimProgress> writer) : IProgress<SimProgress>
    {
        public void Report(SimProgress value) => writer.TryWrite(value);
    }
}
