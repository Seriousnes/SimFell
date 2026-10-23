using FellowshipSim.Core.APL;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Import;

namespace FellowshipSim.Tests;

/// <summary>
/// Monte Carlo progress and cancellation: completed-fight counts rise to the fight count, a cancelled run stops early, and
/// a simulator fights as before after a cancelled run.
/// </summary>
public class MonteCarloProgressTests
{
    private const string Apl = """
        actions+=/ice_blitz
        actions+=/winters_blessing
        actions+=/glacial_blast
        actions+=/frost_bolt
        """;

    private const int Seconds = 60;

    [Theory]
    [InlineData(1, 1)]
    [InlineData(250, 1)]
    [InlineData(250, 4)]
    public void Progress_Rises_To_The_Fight_Count(int fights, int degreeOfParallelism)
    {
        var reports = new List<int>();
        Simulator().Run(SimTime.FromSeconds(Seconds), fights, degreeOfParallelism: degreeOfParallelism, progress: reports.Add);
        Assert.NotEmpty(reports);
        Assert.Equal(fights, reports[^1]);
        Assert.Equal(reports.Order(), reports);
        Assert.Equal(reports.Distinct().Count(), reports.Count);
    }

    [Fact]
    public void A_Cancelled_Run_Stops_Before_Its_Last_Fight()
    {
        const int fights = 100_000;
        using var cancel = new CancellationTokenSource();
        int last = 0;
        Assert.ThrowsAny<OperationCanceledException>(() => Simulator().Run(SimTime.FromSeconds(Seconds), fights,
            degreeOfParallelism: 2, progress: completed =>
            {
                last = completed;
                cancel.Cancel();
            }, cancellationToken: cancel.Token));
        Assert.True(last < fights, $"{last} fights completed");
    }

    [Fact]
    public void A_Simulator_Fights_As_Before_After_A_Cancelled_Run()
    {
        const int fights = 32;
        var simulator = Simulator();
        using var cancel = new CancellationTokenSource();
        Assert.ThrowsAny<OperationCanceledException>(() => simulator.Run(SimTime.FromSeconds(Seconds), 10_000,
            degreeOfParallelism: 4, progress: _ => cancel.Cancel(), cancellationToken: cancel.Token));

        var after = simulator.Run(SimTime.FromSeconds(Seconds), fights, degreeOfParallelism: 4, keepFightDamage: true);
        var fresh = Simulator().Run(SimTime.FromSeconds(Seconds), fights, degreeOfParallelism: 4, keepFightDamage: true);
        Assert.Equal(fresh.IterationDamage, after.IterationDamage);
    }

    private static MonteCarloSimulator Simulator()
    {
        var build = System.Text.Json.JsonSerializer.Deserialize<CharacterBuild>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "rime_character.json")))!;
        return new MonteCarloSimulator(build, AplParser.ParseScript(Apl));
    }
}
