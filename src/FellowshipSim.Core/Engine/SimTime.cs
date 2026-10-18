namespace FellowshipSim.Core.Engine;

public readonly record struct SimTime(long Milliseconds) : IComparable<SimTime>
{
    public static SimTime Zero => new(0);
    public static SimTime Infinite => new(long.MaxValue);

    public static SimTime FromSeconds(double sec) => new((long)Math.Round(sec * 1000.0));
    public static SimTime FromMilliseconds(long ms) => new(ms);

    public double TotalSeconds => Milliseconds / 1000.0;
    public double TotalMilliseconds => Milliseconds;

    public int CompareTo(SimTime other) => Milliseconds.CompareTo(other.Milliseconds);

    public static SimTime operator +(SimTime a, SimTime b) => new(a.Milliseconds + b.Milliseconds);
    public static SimTime operator -(SimTime a, SimTime b) => new(Math.Max(0, a.Milliseconds - b.Milliseconds));
    public static bool operator <(SimTime a, SimTime b) => a.Milliseconds < b.Milliseconds;
    public static bool operator >(SimTime a, SimTime b) => a.Milliseconds > b.Milliseconds;
    public static bool operator <=(SimTime a, SimTime b) => a.Milliseconds <= b.Milliseconds;
    public static bool operator >=(SimTime a, SimTime b) => a.Milliseconds >= b.Milliseconds;

    public override string ToString() => $"{TotalSeconds:F3}s";
}

