namespace FellowshipSim.Engine.Replay;

/// <summary>Enemy identified by its ID and instance, as a dictionary or set key.</summary>
public readonly record struct UnitKey(int Id, int Instance);
