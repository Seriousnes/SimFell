namespace FellowshipSim.Logs;

/// <summary>Fight already downloaded into the data folder.</summary>
public readonly record struct DownloadedFight(string ReportCode, int FightId);
