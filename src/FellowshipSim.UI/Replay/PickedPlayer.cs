using FellowshipSim.UI.Services;

namespace FellowshipSim.UI.Replay;

/// <summary>A player picked in a fight of a report.</summary>
public sealed record PickedPlayer(string ReportCode, ReportFight Fight, FightPlayer Player);
