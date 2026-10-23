namespace FellowshipSim.Engine;

/// <summary>
/// A Monte Carlo sim's fights: <see cref="Iterations"/> fights of <see cref="DurationSeconds"/> against
/// <see cref="TargetCount"/> targets. Fight i has seed <see cref="FirstSeed"/> + i. <see cref="DegreeOfParallelism"/> is
/// the most threads the fights use; null is <see cref="MonteCarloSimulator.DefaultDegreeOfParallelism"/>.
/// </summary>
public sealed record SimSettings(int Iterations, double DurationSeconds, int TargetCount = 1, int FirstSeed = 1, int? DegreeOfParallelism = null);
