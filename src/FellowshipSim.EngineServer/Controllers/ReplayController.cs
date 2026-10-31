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
        catch (SimEngineException ex)
        {
            return BadRequest(new EngineError(ex.Message));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Replay review of player {Player} failed", playerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EngineError($"{ex.GetType().Name}: {ex.Message}"));
        }
    }
}
