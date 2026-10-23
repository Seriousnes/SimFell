namespace FellowshipSim.UI.Services;

/// <summary>
/// A FellowshipLogs report with its fights, and the fight and player its link names (<see cref="SelectedFightId"/>,
/// <see cref="SelectedPlayerId"/>).
/// </summary>
public sealed record Report(
    string Code, string Title, DateTime StartTime, int? SelectedFightId, int? SelectedPlayerId, IReadOnlyList<ReportFight> Fights);
