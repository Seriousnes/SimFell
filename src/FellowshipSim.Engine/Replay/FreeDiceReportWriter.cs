using System.Globalization;
using System.Net;
using System.Text;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// The page of a free-dice run (&lt;review&gt;.freedice.html): the engine's damage over all runs against the log's, its
/// spread, and the same per damage source.
/// </summary>
public static class FreeDiceReportWriter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Write(FreeDiceReplay.Result result, string path, string? navigation = null) =>
        File.WriteAllText(path, Render(result, navigation));

    public static string Render(FreeDiceReplay.Result result, string? navigation = null)
    {
        var sb = new StringBuilder();
        sb.Append(ReplayReportWriter.Head($"Free dice: {result.PlayerName}"));
        if (navigation != null) sb.Append(navigation);
        sb.Append($"<h1>Free dice: {EncodeHtml(result.PlayerName)} - {EncodeHtml(result.FightName)}</h1>");
        sb.Append("<div class=\"muted\">The fight replayed many times: casts follow the log, the engine rolls its own crits, " +
                  "procs and damage spread, and area hits reach the enemies the player was fighting at the time. " +
                  "If the engine is right, the log should look like a typical run (somewhere near the middle).</div>");

        double place = result.LogPercentile;
        string placeClass = place is >= 0.1 and <= 0.9 ? "ok" : place is >= 0.03 and <= 0.97 ? "warn" : "bad";
        sb.Append("<div class=\"panel grid\">");
        Stat(sb, "Log damage", FormatNumber(result.LogDamage), "");
        Stat(sb, "Engine average", $"{FormatNumber(result.Mean)} <span class=\"{RatioClass(result.Mean / result.LogDamage, 0.02, 0.05)}\">({FormatPercent(result.Mean / result.LogDamage)} of log)</span>", "");
        Stat(sb, "Engine 5-95%", $"{FormatNumber(result.Percentile(0.05))} - {FormatNumber(result.Percentile(0.95))}", $"spread {FormatPercent(result.StdDev / result.Mean)} (1 std dev)");
        Stat(sb, "Log's place", $"<span class=\"{placeClass}\">{place.ToString("P0", Inv)}</span>", "of the engine's runs dealt less than the log");
        Stat(sb, "Runs", result.EngineTotals.Count.ToString(Inv), $"{result.Elapsed.TotalSeconds.ToString("F1", Inv)}s; casts forced (engine short of resources): {result.MeanForcedCasts.ToString("F1", Inv)} per run");
        sb.Append("</div>");

        sb.Append("<h2>Engine runs</h2><div class=\"panel\">");
        sb.Append("<div class=\"legend\"><span><i style=\"background:var(--engine)\"></i>engine runs</span><span><i style=\"background:var(--log)\"></i>log</span></div>");
        sb.Append(Histogram(result.EngineTotals, result.LogDamage));
        sb.Append("</div>");

        sb.Append("<h2>Damage by ability</h2><div class=\"muted\">Engine columns are averages over the runs. A log damage outside the " +
                  "engine's 5-95% range is flagged: one such source can be luck, several point at the engine.</div>");
        sb.Append("<div class=\"panel scroll\"><table><thead><tr><th>Ability</th><th class=\"num\">ID</th>" +
                  "<th class=\"num\">Log damage</th><th class=\"num\">Engine average</th><th class=\"num\">Engine vs log</th>" +
                  "<th class=\"num\">Engine 5-95%</th><th class=\"num\">Log hits</th><th class=\"num\">Engine hits</th>" +
                  "<th class=\"num\">Log crit</th><th class=\"num\">Engine crit</th></tr></thead><tbody>");
        foreach (var ability in result.Abilities)
        {
            bool outside = ability.LogDamage < ability.Engine5 || ability.LogDamage > ability.Engine95;
            string ratio = ability.LogDamage > 0 ? $"<span class=\"{RatioClass(ability.EngineMean / ability.LogDamage, 0.03, 0.10)}\">{FormatPercent(ability.EngineMean / ability.LogDamage)}</span>" : "<span class=\"muted\">engine only</span>";
            sb.Append($"<tr><td>{EncodeHtml(ability.Name)}{(outside ? " <span class=\"tag\" title=\"The log's damage is outside the engine's 5-95% range\">outside range</span>" : "")}</td>" +
                      $"<td class=\"num muted\">{ability.Id}</td><td class=\"num\">{FormatNumber(ability.LogDamage)}</td><td class=\"num\">{FormatNumber(ability.EngineMean)}</td>" +
                      $"<td class=\"num\">{ratio}</td><td class=\"num muted\">{FormatNumber(ability.Engine5)} - {FormatNumber(ability.Engine95)}</td>" +
                      $"<td class=\"num\">{ability.LogHits.ToString("N0", Inv)}</td><td class=\"num\">{ability.EngineHits.ToString("N1", Inv)}</td>" +
                      $"<td class=\"num\">{Rate(ability.LogCrits, ability.LogHits)}</td><td class=\"num\">{Rate(ability.EngineCrits, ability.EngineHits)}</td></tr>");
        }
        sb.Append($"<tr class=\"total\"><td>Total</td><td></td><td class=\"num\">{FormatNumber(result.LogDamage)}</td><td class=\"num\">{FormatNumber(result.Mean)}</td>" +
                  $"<td class=\"num\"><span class=\"{RatioClass(result.Mean / result.LogDamage, 0.02, 0.05)}\">{FormatPercent(result.Mean / result.LogDamage)}</span></td>" +
                  $"<td class=\"num muted\">{FormatNumber(result.Percentile(0.05))} - {FormatNumber(result.Percentile(0.95))}</td><td></td><td></td><td></td><td></td></tr>");
        sb.Append("</tbody></table></div></main></body></html>");
        return sb.ToString();
    }

    private static void Stat(StringBuilder sb, string label, string value, string note) =>
        sb.Append($"<div><div class=\"muted\">{label}</div><div style=\"font-size:18px;font-weight:600\">{value}</div>" +
                  (note.Length > 0 ? $"<div class=\"muted\">{note}</div>" : "") + "</div>");

    /// <summary>The engine's totals as bars, with the log's damage as a line.</summary>
    private static string Histogram(IReadOnlyList<double> totals, double log)
    {
        const int bins = 40, width = 1000, height = 180, pad = 24;
        double lo = Math.Min(totals.Min(), log), hi = Math.Max(totals.Max(), log);
        double span = Math.Max(hi - lo, 1.0);
        lo -= span * 0.02;
        hi += span * 0.02;
        var counts = new int[bins];
        foreach (double total in totals) counts[Math.Clamp((int)((total - lo) / (hi - lo) * bins), 0, bins - 1)]++;
        int max = counts.Max();
        double ScaleX(double value) => (value - lo) / (hi - lo) * width;
        var sb = new StringBuilder($"<svg class=\"chart\" viewBox=\"0 0 {width} {height + pad}\" role=\"img\" aria-label=\"Engine runs against the log\">");
        double barW = (double)width / bins;
        for (int i = 0; i < bins; i++)
        {
            if (counts[i] == 0) continue;
            double barHeight = (double)counts[i] / max * (height - 10);
            sb.Append(string.Format(Inv, "<rect x=\"{0:F1}\" y=\"{1:F1}\" width=\"{2:F1}\" height=\"{3:F1}\" fill=\"var(--engine)\" opacity=\"0.75\"/>",
                i * barW + 1, height - barHeight, barW - 2, barHeight));
        }
        sb.Append($"<line class=\"axis\" x1=\"0\" y1=\"{height}\" x2=\"{width}\" y2=\"{height}\"/>");
        sb.Append(string.Format(Inv, "<line x1=\"{0:F1}\" y1=\"0\" x2=\"{0:F1}\" y2=\"{1}\" stroke=\"var(--log)\" stroke-width=\"3\"/>", ScaleX(log), height));
        sb.Append(string.Format(Inv, "<text x=\"{0:F1}\" y=\"{1}\" text-anchor=\"middle\">log {2}</text>", Math.Clamp(ScaleX(log), 40, width - 40), height + 16, FormatNumber(log)));
        sb.Append($"<text x=\"0\" y=\"{height + 16}\">{FormatNumber(lo)}</text><text x=\"{width}\" y=\"{height + 16}\" text-anchor=\"end\">{FormatNumber(hi)}</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string RatioClass(double ratio, double good, double warn)
    {
        double off = Math.Abs(1 - ratio);
        return off <= good ? "ok" : off <= warn ? "warn" : "bad";
    }

    private static string Rate(double crits, double hits) => hits > 0 ? (crits / hits).ToString("P1", Inv) : "-";
    private static string FormatNumber(double value) => value.ToString("N0", Inv);
    private static string FormatPercent(double value) => value.ToString("P1", Inv);
    private static string EncodeHtml(string text) => WebUtility.HtmlEncode(text);
}
