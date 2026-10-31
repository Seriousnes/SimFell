using FellowshipSim.Engine.Replay;
using Xunit.Abstractions;

namespace FellowshipSim.Tests;

/// <summary>
/// Replays a downloaded fight many times with free dice (the engine rolls its own crits and procs) and writes the spread of
/// the engine's damage against the log's to data/reviews/&lt;review&gt;.freedice.txt.
/// Choose the fight and the number of runs with the environment variables FREEDICE_REVIEW (a review ID such as
/// yMAZVJL9hTpFjqbC_fight7_p1, or "all" for every downloaded script; default: the first one), FREEDICE_ITERATIONS
/// (default 10) and FREEDICE_SEED (the first run's seed, default 1; "random" picks one). Run i uses seed + i, so a run
/// can be repeated exactly.
/// Run with: dotnet test --filter FreeDice
/// </summary>
public class FreeDiceReplayTests(ITestOutputHelper output)
{
    private const string ReviewVariable = "FREEDICE_REVIEW";
    private const string IterationsVariable = "FREEDICE_ITERATIONS";
    private const string SeedVariable = "FREEDICE_SEED";
    private const int DefaultIterations = 10;
    private const int DefaultSeed = 1;
    private const string ScriptsFolder = FellowshipSim.Logs.ReplayLibrary.ScriptsFolder;
    private const string ReviewsFolder = FellowshipSim.Logs.ReplayLibrary.ReviewsFolder;
    private const string ReplayScriptExtension = ".replay.json";

    [Fact]
    public void FreeDice_Replay_Of_One_Fight()
    {
        var data = Path.Combine(TestPaths.RepositoryRoot, TestPaths.DataFolder);
        var scripts = Path.Combine(data, ScriptsFolder);
        if (!Directory.Exists(scripts) || Directory.GetFiles(scripts, "*" + ReplayScriptExtension).Length == 0)
        {
            output.WriteLine("No replay scripts downloaded; nothing to run.");
            return;
        }

        var all = Directory.GetFiles(scripts, "*" + ReplayScriptExtension).OrderBy(f => f)
            .Select(f => Path.GetFileName(f).Replace(ReplayScriptExtension, "")).ToList();
        var setting = Environment.GetEnvironmentVariable(ReviewVariable) ?? all[0];
        var reviews = setting == "all" ? all : [setting];
        var iterations = int.TryParse(Environment.GetEnvironmentVariable(IterationsVariable), out var parsedIterations) ? parsedIterations : DefaultIterations;
        var seedSetting = Environment.GetEnvironmentVariable(SeedVariable);
        var seed = seedSetting == "random" ? Random.Shared.Next(1, int.MaxValue / 2) : int.TryParse(seedSetting, out var parsedSeed) ? parsedSeed : DefaultSeed;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        foreach (var review in reviews)
        {
            var script = ReplayScript.Load(Path.Combine(scripts, $"{review}{ReplayScriptExtension}"));
            var result = FreeDiceReplay.Run(script, iterations, seed);
            var text = $"seeds {seed}-{seed + iterations - 1}\n" + result;
            output.WriteLine($"{review}: {result.Mean / result.LogDamage:P1} of log, log at the {result.LogPercentile:P0} point ({result.Elapsed.TotalSeconds:F1}s)");
            File.WriteAllText(Path.Combine(data, ReviewsFolder, $"{review}.freedice.txt"), text);
            File.WriteAllText(Path.Combine(data, ReviewsFolder, $"{review}.freedice.json"), System.Text.Json.JsonSerializer.Serialize(result.ToSummary()));
            FreeDiceReportWriter.Write(result, Path.Combine(data, ReviewsFolder, $"{review}.freedice.html"),
                $"<nav class=\"fightnav\"><a href=\"{review}.html\">Exact replay</a></nav>");

            Assert.Equal(iterations, result.EngineTotals.Count);
            Assert.True(result.Mean > 0);
        }
        output.WriteLine($"{reviews.Count} fights x {iterations} runs in {clock.Elapsed.TotalSeconds:F1}s");
    }
}
