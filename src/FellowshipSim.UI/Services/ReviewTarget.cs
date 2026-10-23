namespace FellowshipSim.UI.Services;

/// <summary>The fight and player of a review job.</summary>
public sealed record ReviewTarget(string ReportCode, int FightId, int PlayerId);
