namespace FellowshipSim.Core.Engine;

public readonly record struct SimTime(long Milliseconds) : IComparable<SimTime>
{
    public static SimTime Zero => new(0);
    public static SimTime Infinite => new(long.MaxValue);

    public const double MillisecondsPerSecond = 1000.0;
    public const double SecondsPerMinute = 60.0;

    public static SimTime FromSeconds(double sec) => new((long)Math.Round(sec * MillisecondsPerSecond));
    public static SimTime FromMilliseconds(long ms) => new(ms);

    public double TotalSeconds => Milliseconds / MillisecondsPerSecond;
    public double TotalMilliseconds => Milliseconds;

    public int CompareTo(SimTime other) => Milliseconds.CompareTo(other.Milliseconds);

    public static SimTime operator +(SimTime left, SimTime right) => new(left.Milliseconds + right.Milliseconds);
    public static SimTime operator -(SimTime left, SimTime right) => new(Math.Max(0, left.Milliseconds - right.Milliseconds));
    public static bool operator <(SimTime left, SimTime right) => left.Milliseconds < right.Milliseconds;
    public static bool operator >(SimTime left, SimTime right) => left.Milliseconds > right.Milliseconds;
    public static bool operator <=(SimTime left, SimTime right) => left.Milliseconds <= right.Milliseconds;
    public static bool operator >=(SimTime left, SimTime right) => left.Milliseconds >= right.Milliseconds;

    public override string ToString() => $"{TotalSeconds:F3}s";
}

