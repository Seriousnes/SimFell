using System.Collections.Concurrent;
using FellowshipSim.Logs;

namespace FellowshipSim.Server.Services;

/// <summary>A replay review being made in the background, with its progress.</summary>
public sealed class ReviewJob
{
    public required string Id { get; init; }
    public required string ReportCode { get; init; }
    public required int FightId { get; init; }
    public required int PlayerId { get; init; }
    public string Status { get; set; } = "running"; // running | done | failed
    public string Message { get; set; } = "Starting";
    public ReviewSummary? Review { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Runs replay reviews one at a time in the background (downloading a fight can take a minute) so the page can poll for
/// progress.
/// </summary>
public sealed class ReviewJobs(ReplayLibrary library, LogsSettingsStore settings, ILogger<ReviewJobs> log)
{
    private readonly ConcurrentDictionary<string, ReviewJob> _jobs = new();
    private readonly SemaphoreSlim _oneAtATime = new(1, 1);

    public ReviewJob? Get(string id) => _jobs.GetValueOrDefault(id);

    public ReviewJob Start(string reportCode, int fightId, int playerId)
    {
        var job = new ReviewJob { Id = Guid.NewGuid().ToString("N"), ReportCode = reportCode, FightId = fightId, PlayerId = playerId };
        _jobs[job.Id] = job;
        _ = Task.Run(async () =>
        {
            await _oneAtATime.WaitAsync();
            try
            {
                var progress = new Progress<string>(m => job.Message = m);
                job.Review = await library.RunReviewAsync(settings.CreateClient(), reportCode, fightId, playerId, progress: progress);
                job.Message = "Done";
                job.Status = "done";
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Replay review of {Report} fight {Fight} player {Player} failed", reportCode, fightId, playerId);
                job.Error = ex is FellowshipLogsException or NotSupportedException or ArgumentException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";
                job.Status = "failed";
            }
            finally
            {
                _oneAtATime.Release();
            }
        });
        return job;
    }
}
