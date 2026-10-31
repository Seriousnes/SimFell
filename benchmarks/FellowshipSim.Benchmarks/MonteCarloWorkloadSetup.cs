namespace FellowshipSim.Benchmarks;

/// <summary>Target count and action list of a Monte Carlo workload.</summary>
public readonly record struct MonteCarloWorkloadSetup(int Targets, bool Idle);
