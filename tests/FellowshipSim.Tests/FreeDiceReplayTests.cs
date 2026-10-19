using FellowshipSim.Logs.Replay;
using Xunit.Abstractions;

namespace FellowshipSim.Tests;

/// <summary>
/// Replays a downloaded fight many times with free dice (the engine rolls its own crits and procs) and writes the spread of
/// the engine's damage against the log's to data/reviews/&lt;review&gt;.freedice.txt.
/// Choose the fight and the number of runs with the environment variables FREEDICE_REVIEW (a review ID such as
/// yMAZVJL9hTpFjqbC_fight7_p1; default: the first downloaded script) and FREEDICE_ITERATIONS (default 10).
/// Run with: dotnet test --filter FreeDice
/// </summary>
public class FreeDiceReplayTests(ITestOutputHelper output)
{
    [Fact]
    public void FreeDice_Replay_Of_One_Fight()
    {
        string data = Path.Combine(TestPaths.RepositoryRoot, "data");
        string scripts = Path.Combine(data, "scripts");
        if (!Directory.Exists(scripts) || Directory.GetFiles(scripts, "*.replay.json").Length == 0)
        {
            output.WriteLine("No replay scripts downloaded; nothing to run.");
            return;
        }

        string review = Environment.GetEnvironmentVariable("FREEDICE_REVIEW")
                        ?? Path.GetFileName(Directory.GetFiles(scripts, "*.replay.json").OrderBy(f => f).First()).Replace(".replay.json", "");
        int iterations = int.TryParse(Environment.GetEnvironmentVariable("FREEDICE_ITERATIONS"), out int n) ? n : 10;

        var script = ReplayScript.Load(Path.Combine(scripts, $"{review}.replay.json"));
        var result = FreeDiceReplay.Run(script, iterations);
        string text = result.ToString();
        output.WriteLine(text);
        File.WriteAllText(Path.Combine(data, "reviews", $"{review}.freedice.txt"), text);

        Assert.Equal(iterations, result.EngineTotals.Count);
        Assert.True(result.Mean > 0);
    }
}
