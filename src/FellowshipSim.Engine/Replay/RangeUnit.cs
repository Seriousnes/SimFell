namespace FellowshipSim.Engine.Replay;

/// <summary>The unit of an <see cref="ExpectedRange"/>'s values.</summary>
public enum RangeUnit
{
    Seconds,
    Damage,
    Crit,
    Stacks,
    Ticks,
    Procs,
    /// <summary>A hero resource, named by <see cref="ExpectedRange.ResourceName"/>.</summary>
    Resource,
}
