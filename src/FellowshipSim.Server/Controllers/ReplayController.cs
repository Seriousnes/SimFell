using FellowshipSim.Core.Replay;
using FellowshipSim.Logs;
using FellowshipSim.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FellowshipSim.Server.Controllers;

/// <summary>Importing fights from FellowshipLogs and running replay reviews of them.</summary>
[ApiController]
[Route("api")]
public sealed class ReplayController(ReplayLibrary library, LogsSettingsStore settings, ReviewJobs jobs) : ControllerBase
{
    /// <summary>Whether FellowshipLogs credentials are set, and which heroes can be replayed.</summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        credentialsConfigured = settings.Credentials != null,
        supportedHeroes = ReplayDriver.SupportedHeroes,
        dataDirectory = library.DataDirectory,
    });

    public sealed record CredentialsRequest(string ClientId, string ClientSecret);

    /// <summary>Saves the FellowshipLogs API client credentials after checking they can sign in.</summary>
    [HttpPut("settings/fellowshiplogs")]
    public async Task<IActionResult> SaveCredentials([FromBody] CredentialsRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest(new { error = "Enter both the client ID and the client secret." });
        }
        var credentials = new LogsCredentials(request.ClientId.Trim(), request.ClientSecret.Trim());
        try
        {
            await settings.Create(credentials).QueryAsync("query { __typename }", ct: ct);
        }
        catch (FellowshipLogsException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        settings.Save(credentials);
        return Ok(new { saved = true });
    }

    /// <summary>A report's fights and players, from a FellowshipLogs link or report code.</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> Report([FromQuery] string link, CancellationToken ct)
    {
        ReportLink parsed;
        try
        {
            parsed = ReportLink.Parse(link);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        var client = settings.CreateClient();
        if (client == null) return BadRequest(new { error = "Set your FellowshipLogs API client ID and secret first." });
        try
        {
            var summary = await client.GetSummaryAsync(parsed.ReportCode, ct);
            var downloaded = library.ListDownloadedFights().Where(f => f.ReportCode == parsed.ReportCode).Select(f => f.FightId).ToHashSet();
            return Ok(new
            {
                summary.Code,
                summary.Title,
                startTime = DateTimeOffset.FromUnixTimeMilliseconds(summary.StartTime).UtcDateTime,
                selectedFightId = parsed.FightId,
                selectedPlayerId = parsed.PlayerId,
                fights = summary.Fights.Select(f => new
                {
                    f.Id, f.Name, f.Kill, f.DurationSeconds,
                    downloaded = downloaded.Contains(f.Id),
                    players = f.Players.Select(p => new { p.Id, p.Name, p.Hero, supported = ReplayDriver.SupportsHero(p.Hero) }),
                }),
            });
        }
        catch (FellowshipLogsException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed record ReviewRequest(string ReportCode, int FightId, int PlayerId);

    /// <summary>Starts a replay review in the background; poll <c>GET api/reviews/jobs/{id}</c> for its progress.</summary>
    [HttpPost("reviews")]
    public IActionResult StartReview([FromBody] ReviewRequest request) =>
        Ok(jobs.Start(request.ReportCode, request.FightId, request.PlayerId));

    [HttpGet("reviews/jobs/{id}")]
    public IActionResult Job(string id) => jobs.Get(id) is { } job ? Ok(job) : NotFound();

    /// <summary>Every review run so far, newest first. Each page is at <c>/reviews/{id}.html</c>.</summary>
    [HttpGet("reviews")]
    public IActionResult Reviews() => Ok(library.ListReviews());
}
