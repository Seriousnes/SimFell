namespace FellowshipSim.Core.Domain;

/// <summary>A talent in a hero's talent tree: its row and column, its point cost and its talent ID.</summary>
public sealed record TalentSlot(int Row, int Column, int Cost, int TalentId);
