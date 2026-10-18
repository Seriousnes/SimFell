using FellowshipSim.Logs.Replay;
using FellowshipSim.Logs;
using Xunit.Abstractions;

namespace FellowshipSim.Tests;

/// <summary>
/// Replays every fight downloaded into the data folder (data/fights, e.g. through the web page) for each player whose
/// hero the engine supports, and writes their review pages to data/reviews. With no fights downloaded it does nothing.
/// Run with: dotnet test --filter AllDownloadedFights
/// </summary>
public class CombatLogReplayTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AllDownloadedFights_Replay_And_Write_Reviews()
    {
        var library = new ReplayLibrary(Path.Combine(TestPaths.RepositoryRoot, "data"));
        var fights = library.ListDownloadedFights();
        if (fights.Count == 0)
        {
            output.WriteLine($"No fights in {library.FightsDirectory}; import one through the web page first.");
            return;
        }

        foreach (var (reportCode, fightId) in fights)
        {
            var raw = await library.GetRawFightAsync(null, reportCode, fightId);
            foreach (var player in ReplayScriptBuilder.Players(raw).Where(p => ReplayDriver.SupportsHero(p.Hero)))
            {
                var review = await library.RunReviewAsync(null, reportCode, fightId, player.Id);
                output.WriteLine($"{review.PlayerName} ({review.Hero}) {review.FightName}: {review.MatchedHits}/{review.LogHits} hits, " +
                                 $"damage {review.EngineDamage / review.LogDamage:P1} of log, {review.PassedSteps}/{review.TotalSteps} clean steps " +
                                 $"-> {Path.Combine(library.ReviewsDirectory, review.HtmlFile)}");
                Assert.True(review.MatchedHits > 0);
            }
        }
    }
}

internal static class TestPaths
{
    /// <summary>The folder holding FellowshipSim.slnx.</summary>
    public static string RepositoryRoot { get; } = Find();

    private static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles("FellowshipSim.slnx").Length > 0) return dir.FullName;
        }
        throw new DirectoryNotFoundException("Could not find the repository root (FellowshipSim.slnx).");
    }
}
