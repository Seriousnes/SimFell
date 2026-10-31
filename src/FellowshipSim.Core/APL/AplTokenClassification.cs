namespace FellowshipSim.Core.APL;

/// <summary>Token kind of a property for a hero, and the problem when the hero has nothing by that name.</summary>
public readonly record struct AplTokenClassification(AplTokenKind Kind, string? Problem);
