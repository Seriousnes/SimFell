using FellowshipSim.Engine.Jobs;
using FellowshipSim.Logs;
using FellowshipSim.Service.Services;
using FellowshipSim.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FellowshipSim.Service.Controllers;

/// <summary>Importing fights from FellowshipLogs and running replay reviews of them.</summary>
[ApiController]
[Route("api")]
public sealed class ReplayController(ReplayLibrary library, LogsSettingsStore settings, ReviewJobs reviews, SimJobQueue queue,
    EngineClient engine) : ControllerBase
{
    /// <summary>Whether FellowshipLogs credentials are set, and which heroes can be replayed.</summary>
    [HttpGet("status")]
    public async Task<ReplayStatus> Status(CancellationToken ct) =>
        new(settings.Credentials != null, await engine.HeroesAsync(ct), library.DataDirectory, settings.SettingsPath);

    /// <summary>Saves the FellowshipLogs API client credentials after checking they can sign in.</summary>
    [HttpPut("settings/fellowshiplogs")]
    [ProducesResponseType<CredentialsSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CredentialsSaved>> SaveCredentials([FromBody] CredentialsRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return BadRequest(new ApiError("Enter both the client ID and the client secret."));
        }
        var credentials = new LogsCredentials(request.ClientId.Trim(), request.ClientSecret.Trim());
        try
        {
            await settings.Create(credentials).QueryAsync("query { __typename }", ct: ct);
        }
        catch (FellowshipLogsException ex)
        {
            return BadRequest(new ApiError(ex.Message));
        }
        settings.Save(credentials);
        return new CredentialsSaved(true);
    }

    /// <summary>A report's fights and players, from a FellowshipLogs link or report code.</summary>
    [HttpGet("reports")]
    [ProducesResponseType<Report>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Report>> Report([FromQuery] string link, CancellationToken ct)
    {
        ReportLink parsed;
        try
        {
            parsed = ReportLink.Parse(link);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiError(ex.Message));
        }
        var client = settings.CreateClient();
        if (client == null) return BadRequest(new ApiError("Set your FellowshipLogs API client ID and secret first."));
        try
        {
            var summary = await client.GetSummaryAsync(parsed.ReportCode, ct);
            var heroes = (await engine.HeroesAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var downloaded = library.ListDownloadedFights().Where(f => f.ReportCode == parsed.ReportCode).Select(f => f.FightId).ToHashSet();
            return new Report(
                summary.Code,
                summary.Title,
                DateTimeOffset.FromUnixTimeMilliseconds(summary.StartTime).UtcDateTime,
                parsed.FightId,
                parsed.PlayerId,
                summary.Fights.Select(f => new ReportFight(
                    f.Id, f.Name, f.Kill, f.DurationSeconds, downloaded.Contains(f.Id),
                    f.Players.Select(p => new ReportPlayer(p.Id, p.Name, p.Hero, heroes.Contains(p.Hero))).ToList())).ToList());
        }
        catch (FellowshipLogsException ex)
        {
            return BadRequest(new ApiError(ex.Message));
        }
    }

    /// <summary>Starts a replay review in the background; poll <c>GET api/reviews/jobs/{id}</c> for its progress.</summary>
    [HttpPost("reviews")]
    public ReviewJob StartReview([FromBody] ReviewRequest request) =>
        ReviewJob.From(reviews.Start(request.ReportCode, request.FightId, request.PlayerId));

    [HttpGet("reviews/jobs/{id}")]
    [ProducesResponseType<ReviewJob>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public ActionResult<ReviewJob> Job(string id) =>
        queue.Get(id) is { Kind: SimJobKind.Review } job ? ReviewJob.From(job) : NotFound(new ApiError($"No replay review job {id}."));

    /// <summary>Every review run so far, newest first. Each page is at <c>/reviews/{id}.html</c>.</summary>
    [HttpGet("reviews")]
    public IReadOnlyList<ReviewSummary> Reviews() => library.ListReviews();
}
