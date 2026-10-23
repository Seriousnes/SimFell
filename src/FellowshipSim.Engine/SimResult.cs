namespace FellowshipSim.Engine;

/// <summary>A Monte Carlo sim's DPS: mean, lowest and highest fight, standard deviation, and each ability's mean DPS.</summary>
public sealed record SimResult(
    double MeanDps,
    double MinDps,
    double MaxDps,
    double StandardDeviation,
    IReadOnlyDictionary<string, double> AbilityDps,
    int Iterations,
    double DurationSeconds)
{
    public static SimResult From(MonteCarloResult result) => new(result.MeanDps, result.MinDps, result.MaxDps,
        result.StandardDeviation, result.AbilityDpsBreakdown, result.Iterations, result.DurationSeconds);
}
