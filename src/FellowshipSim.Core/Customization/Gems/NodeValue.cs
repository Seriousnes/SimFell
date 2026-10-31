namespace FellowshipSim.Core.Customization.Gems;

/// <summary>A value of a gem node, by the key the gem reads it with.</summary>
public readonly record struct NodeValue(string Key, double Value);
