namespace FellowshipSim.Engine.Jobs;

/// <summary>What a <see cref="SimJob"/> does.</summary>
public enum SimJobKind
{
    /// <summary>A Monte Carlo sim of a profile.</summary>
    Sim,
    /// <summary>A replay review of a logged fight: downloading the fight, then replaying it.</summary>
    Review,
}
