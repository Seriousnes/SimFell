using FellowshipSim.Engine;
using Microsoft.AspNetCore.Mvc;

namespace FellowshipSim.EngineServer.Controllers;

/// <summary>The heroes the engine simulates, and replay reviews of logged fights.</summary>
[ApiController]
public sealed class ReplayController(ISimEngine engine, ILogger<ReplayController> log) : ControllerBase
{
    /// <summary>The heroes the engine can replay and write action lists for.</summary>
    [HttpGet("heroes")]
    public async Task<IActionResult> Heroes(CancellationToken ct) => Ok(await engine.GetHeroesAsync(ct));

    /// <summary>
    /// Replays one player of a raw fight (the request body, as downloaded from FellowshipLogs) and returns the review's
    /// files.
    /// </summary>
    [HttpPost("reviews")]
    public async Task<IActionResult> Review([FromQuery] int playerId, CancellationToken ct)
    {
        try
        {
            return Ok(await engine.ReviewAsync(Request.Body, playerId, ct: ct));
        }
        catch (SimEngineException exception)
        {
            return BadRequest(new EngineError(exception.Message));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            log.LogError(exception, "Replay review of player {Player} failed", playerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EngineError($"{exception.GetType().Name}: {exception.Message}"));
        }
    }
}
