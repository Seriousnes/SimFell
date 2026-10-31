namespace FellowshipSim.Engine.Replay;

public static class RangeUnitText
{
    /// <summary>The text of <paramref name="unit"/> after a value; <paramref name="resourceName"/> names a <see cref="RangeUnit.Resource"/>.</summary>
    public static string DisplayText(this RangeUnit unit, string? resourceName) => unit switch
    {
        RangeUnit.Seconds => "s",
        RangeUnit.Damage => "damage",
        RangeUnit.Crit => "crit",
        RangeUnit.Stacks => "stacks",
        RangeUnit.Ticks => "ticks",
        RangeUnit.Procs => "procs",
        RangeUnit.Resource => resourceName ?? throw new ArgumentNullException(nameof(resourceName)),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };
}
