namespace FellowshipSim.Core.APL.Expressions;

/// <summary>Slack for the truth and equality of action-list numbers.</summary>
public static class AplTolerance
{
    /// <summary>A number is true when its absolute value is above this.</summary>
    public const double Truth = 0.00001;

    /// <summary>Two numbers are equal when they differ by less than this.</summary>
    public const double Equality = 0.0001;
}
