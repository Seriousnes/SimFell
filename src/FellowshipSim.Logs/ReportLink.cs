using System.Text.RegularExpressions;

namespace FellowshipSim.Logs;

/// <summary>A FellowshipLogs report, optionally narrowed to a fight and a player (from a pasted link or a bare code).</summary>
public sealed record ReportLink(string ReportCode, int? FightId, int? PlayerId)
{
    /// <summary>Matches the report code in the path of a report link.</summary>
    private const string ReportPathPattern = @"reports/([A-Za-z0-9]+)";

    /// <summary>Matches the fight ID in the query of a report link.</summary>
    private const string FightPattern = @"[?&#]fight=(\d+)";

    /// <summary>Matches the player's actor ID in the query of a report link.</summary>
    private const string SourcePattern = @"[?&#]source=(\d+)";

    /// <summary>Matches a bare report code.</summary>
    private const string ReportCodePattern = "^[A-Za-z0-9]+$";

    /// <summary>
    /// Reads "https://www.fellowshiplogs.com/reports/CODE?fight=7&amp;source=1" or just "CODE".
    /// </summary>
    public static ReportLink Parse(string linkOrCode)
    {
        if (string.IsNullOrWhiteSpace(linkOrCode)) throw new ArgumentException("Paste a FellowshipLogs report link or code.");
        string text = linkOrCode.Trim();
        var code = Regex.Match(text, ReportPathPattern);
        var fight = Regex.Match(text, FightPattern);
        var source = Regex.Match(text, SourcePattern);
        string reportCode = code.Success ? code.Groups[1].Value : text.Split('?', '#')[0].Trim('/');
        if (!Regex.IsMatch(reportCode, ReportCodePattern)) throw new ArgumentException($"'{linkOrCode}' is not a FellowshipLogs report link or code.");
        return new ReportLink(
            reportCode,
            fight.Success ? int.Parse(fight.Groups[1].Value) : null,
            source.Success ? int.Parse(source.Groups[1].Value) : null);
    }
}
