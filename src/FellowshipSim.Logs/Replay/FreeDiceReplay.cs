using System.Diagnostics;

namespace FellowshipSim.Logs.Replay;

/// <summary>
/// "Replay with free dice": the logged fight (casts, enemies, situational auras) replayed many times while the engine
/// rolls its own crits, procs and damage spread. The spread of the engine's totals shows whether the log's real damage is
/// a plausible outcome of the engine's random model; a replay that follows the log's dice cannot show that.
/// </summary>
public static class FreeDiceReplay
{
    public sealed record Result(
        string PlayerName, string FightName, double LogDamage, IReadOnlyList<double> EngineTotals,
        double MeanForcedCasts, TimeSpan Elapsed)
    {
        public double Mean => EngineTotals.Average();
        public double StdDev => Math.Sqrt(EngineTotals.Sum(t => (t - Mean) * (t - Mean)) / Math.Max(1, EngineTotals.Count - 1));
        public double Percentile(double p)
        {
            var sorted = EngineTotals.OrderBy(t => t).ToList();
            return sorted[(int)Math.Clamp(Math.Round(p * (sorted.Count - 1)), 0, sorted.Count - 1)];
        }

        /// <summary>The share of runs that dealt less than the log (0.5 = the log is the typical outcome).</summary>
        public double LogPercentile => EngineTotals.Count(t => t < LogDamage) / (double)EngineTotals.Count;

        public override string ToString() =>
            $"{PlayerName} - {FightName}: {EngineTotals.Count} runs in {Elapsed.TotalSeconds:F1}s " +
            $"({Elapsed.TotalMilliseconds / EngineTotals.Count:F0} ms/run)\n" +
            $"  log damage    {LogDamage,12:N0}\n" +
            $"  engine mean   {Mean,12:N0}  ({Mean / LogDamage:P1} of log)\n" +
            $"  engine stddev {StdDev,12:N0}  ({StdDev / Mean:P1})\n" +
            $"  engine 5-95%  {Percentile(0.05),12:N0} - {Percentile(0.95):N0}\n" +
            $"  log sits at the {LogPercentile:P0} point of the engine's runs\n" +
            $"  casts forced to follow the log (engine short of resources): {MeanForcedCasts:F1} per run";
    }

    /// <summary>
    /// Runs <paramref name="iterations"/> replays, in parallel across the CPU's cores (each run has its own hero and
    /// engine; run i uses seed <paramref name="firstSeed"/> + i, so results do not depend on the thread count).
    /// </summary>
    public static Result Run(ReplayScript script, int iterations, int firstSeed = 1)
    {
        var totals = new double[iterations];
        var forced = new int[iterations];
        var clock = Stopwatch.StartNew();
        // Building one hero first sets up the shared class catalog before the threads start.
        _ = ReplayDriver.CreateHero(script);
        Parallel.For(0, iterations, i =>
        {
            var hero = ReplayDriver.CreateHero(script);
            var options = new ReplayOptions { FreeDiceSeed = firstSeed + i, StopOnFirstFailure = false, ForceCasts = true };
            var result = new ReplayDriver(hero, script, options).Run();
            totals[i] = result.EngineTotalDamage;
            forced[i] = result.ForcedCasts.Count;
        });
        clock.Stop();
        double logDamage = script.Observations.Damage.Sum(d => d.TotalAmount);
        return new Result(script.Player.Name, script.FightName, logDamage, totals, forced.Average(), clock.Elapsed);
    }
}
