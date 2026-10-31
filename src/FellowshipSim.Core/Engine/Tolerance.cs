namespace FellowshipSim.Core.Engine;

/// <summary>Floating-point slack for comparisons of computed doubles.</summary>
public static class Tolerance
{
    /// <summary>Slack for comparing a computed value with a bound, a cost or a whole number.</summary>
    public const double Comparison = 1e-9;

    /// <summary>The smallest difference that counts as a change of a rate or haste.</summary>
    public const double Change = 1e-12;

    /// <summary>Slack for rounding a duration to whole ticks or server frames, and for a root search to stop.</summary>
    public const double Rounding = 1e-6;
}
