namespace FellowshipSim.Core.Engine;

/// <summary>Allowance for rounding error in comparisons of computed doubles.</summary>
public static class Tolerance
{
    /// <summary>Allowance for rounding error when comparing a computed value with a bound, a cost or a whole number.</summary>
    public const double Comparison = 1e-9;

    /// <summary>The smallest difference that counts as a change of a rate or haste.</summary>
    public const double Change = 1e-12;

    /// <summary>Allowance for rounding error when rounding a duration to whole ticks or server frames, and when a root search stops.</summary>
    public const double Rounding = 1e-6;

    /// <summary>A number in an action list is true when its absolute value is above this.</summary>
    public const double AplTruth = 0.00001;

    /// <summary>Two numbers in an action list are equal when they differ by less than this.</summary>
    public const double AplEquality = 0.0001;
}
