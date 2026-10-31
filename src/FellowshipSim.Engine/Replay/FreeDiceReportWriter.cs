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

    /// <summary>The log's place (fraction of runs dealing less) from which the page marks it typical.</summary>
    public const double PlaceOkLow = 0.1;

    /// <summary>The log's place up to which the page marks it typical.</summary>
    public const double PlaceOkHigh = 0.9;

    /// <summary>The log's place from which the page marks it unusual instead of far off.</summary>
    public const double PlaceWarnLow = 0.03;

    /// <summary>The log's place up to which the page marks it unusual instead of far off.</summary>
    public const double PlaceWarnHigh = 0.97;

    /// <summary>Deviation of the engine's average from the log (fraction) up to which the page marks it matching.</summary>
    public const double MeanGoodDeviation = 0.02;

    /// <summary>Deviation of the engine's average from the log (fraction) up to which the page warns.</summary>
    public const double MeanWarnDeviation = 0.05;

    /// <summary>Deviation of an ability's engine average from the log (fraction) up to which the page marks it matching.</summary>
    private const double AbilityGoodDeviation = 0.03;

    /// <summary>Deviation of an ability's engine average from the log (fraction) up to which the page warns.</summary>
    private const double AbilityWarnDeviation = 0.10;

    /// <summary>Share of the histogram's span added on each side of its range.</summary>
    private const double HistogramMarginFraction = 0.02;

    /// <summary>Height kept free above the tallest histogram bar (SVG units).</summary>
    private const int BarTopInset = 10;

    /// <summary>Distance between a histogram bar's slot start and the bar (SVG units).</summary>
    private const int BarOffset = 1;

    /// <summary>Width a histogram bar is narrower than its slot (SVG units).</summary>
    private const int BarWidthTrim = 2;

    /// <summary>Nearest the log label's centre comes to either histogram edge (SVG units).</summary>
    private const int LogLabelMargin = 40;

    /// <summary>Distance between the histogram's baseline and its labels (SVG units).</summary>
    private const int AxisLabelDrop = 16;

    public static void Write(FreeDiceReplay.Result result, string path, string? navigation = null) =>
        File.WriteAllText(path, Render(result, navigation));

    public static string Render(FreeDiceReplay.Result result, string? navigation = null)
    {
        var builder = new StringBuilder();
        builder.Append(ReplayReportWriter.Head($"Free dice: {result.PlayerName}"));
        if (navigation != null) builder.Append(navigation);
        builder.Append($"<h1>Free dice: {EncodeHtml(result.PlayerName)} - {EncodeHtml(result.FightName)}</h1>");
        builder.Append("<div class=\"muted\">The fight replayed many times: casts follow the log, the engine rolls its own crits, " +
                  "procs and damage spread, and area hits reach the enemies the player was fighting at the time. " +
                  "If the engine is right, the log should look like a typical run (somewhere near the middle).</div>");

        var place = result.LogPercentile;
        var placeClass = place is >= PlaceOkLow and <= PlaceOkHigh ? "ok" : place is >= PlaceWarnLow and <= PlaceWarnHigh ? "warn" : "bad";
        builder.Append("<div class=\"panel grid\">");
        Stat(builder, "Log damage", FormatNumber(result.LogDamage), "");
        Stat(builder, "Engine average", $"{FormatNumber(result.Mean)} <span class=\"{RatioClass(result.Mean / result.LogDamage, MeanGoodDeviation, MeanWarnDeviation)}\">({FormatPercent(result.Mean / result.LogDamage)} of log)</span>", "");
        Stat(builder, "Engine 5-95%", $"{FormatNumber(result.Percentile(FreeDiceReplay.LowPercentile))} - {FormatNumber(result.Percentile(FreeDiceReplay.HighPercentile))}", $"spread {FormatPercent(result.StdDev / result.Mean)} (1 std dev)");
        Stat(builder, "Log's place", $"<span class=\"{placeClass}\">{place.ToString("P0", Inv)}</span>", "of the engine's runs dealt less than the log");
        Stat(builder, "Runs", result.EngineTotals.Count.ToString(Inv), $"{result.Elapsed.TotalSeconds.ToString("F1", Inv)}s; casts forced (engine short of resources): {result.MeanForcedCasts.ToString("F1", Inv)} per run");
        builder.Append("</div>");

        builder.Append("<h2>Engine runs</h2><div class=\"panel\">");
        builder.Append("<div class=\"legend\"><span><i style=\"background:var(--engine)\"></i>engine runs</span><span><i style=\"background:var(--log)\"></i>log</span></div>");
        builder.Append(Histogram(result.EngineTotals, result.LogDamage));
        builder.Append("</div>");

        builder.Append("<h2>Damage by ability</h2><div class=\"muted\">Engine columns are averages over the runs. A log damage outside the " +
                  "engine's 5-95% range is flagged: one such source can be luck, several point at the engine.</div>");
        builder.Append("<div class=\"panel scroll\"><table><thead><tr><th>Ability</th><th class=\"num\">ID</th>" +
                  "<th class=\"num\">Log damage</th><th class=\"num\">Engine average</th><th class=\"num\">Engine vs log</th>" +
                  "<th class=\"num\">Engine 5-95%</th><th class=\"num\">Log hits</th><th class=\"num\">Engine hits</th>" +
                  "<th class=\"num\">Log crit</th><th class=\"num\">Engine crit</th></tr></thead><tbody>");
        foreach (var ability in result.Abilities)
        {
            var outside = ability.LogDamage < ability.Engine5 || ability.LogDamage > ability.Engine95;
            var ratio = ability.LogDamage > 0 ? $"<span class=\"{RatioClass(ability.EngineMean / ability.LogDamage, AbilityGoodDeviation, AbilityWarnDeviation)}\">{FormatPercent(ability.EngineMean / ability.LogDamage)}</span>" : "<span class=\"muted\">engine only</span>";
            builder.Append($"<tr><td>{EncodeHtml(ability.Name)}{(outside ? " <span class=\"tag\" title=\"The log's damage is outside the engine's 5-95% range\">outside range</span>" : "")}</td>" +
                      $"<td class=\"num muted\">{ability.Id}</td><td class=\"num\">{FormatNumber(ability.LogDamage)}</td><td class=\"num\">{FormatNumber(ability.EngineMean)}</td>" +
                      $"<td class=\"num\">{ratio}</td><td class=\"num muted\">{FormatNumber(ability.Engine5)} - {FormatNumber(ability.Engine95)}</td>" +
                      $"<td class=\"num\">{ability.LogHits.ToString("N0", Inv)}</td><td class=\"num\">{ability.EngineHits.ToString("N1", Inv)}</td>" +
                      $"<td class=\"num\">{Rate(ability.LogCrits, ability.LogHits)}</td><td class=\"num\">{Rate(ability.EngineCrits, ability.EngineHits)}</td></tr>");
        }
        builder.Append($"<tr class=\"total\"><td>Total</td><td></td><td class=\"num\">{FormatNumber(result.LogDamage)}</td><td class=\"num\">{FormatNumber(result.Mean)}</td>" +
                  $"<td class=\"num\"><span class=\"{RatioClass(result.Mean / result.LogDamage, MeanGoodDeviation, MeanWarnDeviation)}\">{FormatPercent(result.Mean / result.LogDamage)}</span></td>" +
                  $"<td class=\"num muted\">{FormatNumber(result.Percentile(FreeDiceReplay.LowPercentile))} - {FormatNumber(result.Percentile(FreeDiceReplay.HighPercentile))}</td><td></td><td></td><td></td><td></td></tr>");
        builder.Append("</tbody></table></div></main></body></html>");
        return builder.ToString();
    }

    private static void Stat(StringBuilder builder, string label, string value, string note) =>
        builder.Append($"<div><div class=\"muted\">{label}</div><div style=\"font-size:18px;font-weight:600\">{value}</div>" +
                  (note.Length > 0 ? $"<div class=\"muted\">{note}</div>" : "") + "</div>");

    /// <summary>The engine's totals as bars, with the log's damage as a line.</summary>
    private static string Histogram(IReadOnlyList<double> totals, double log)
    {
        const int bins = 40, width = 1000, height = 180, pad = 24;
        var lowest = Math.Min(totals.Min(), log);
        var highest = Math.Max(totals.Max(), log);
        var span = Math.Max(highest - lowest, 1.0);
        lowest -= span * HistogramMarginFraction;
        highest += span * HistogramMarginFraction;
        var counts = new int[bins];
        foreach (var total in totals) counts[Math.Clamp((int)((total - lowest) / (highest - lowest) * bins), 0, bins - 1)]++;
        var max = counts.Max();
        double ScaleX(double value) => (value - lowest) / (highest - lowest) * width;
        var builder = new StringBuilder($"<svg class=\"chart\" viewBox=\"0 0 {width} {height + pad}\" role=\"img\" aria-label=\"Engine runs against the log\">");
        var barW = (double)width / bins;
        for (var i = 0; i < bins; i++)
        {
            if (counts[i] == 0) continue;
            var barHeight = (double)counts[i] / max * (height - BarTopInset);
            builder.Append(string.Format(Inv, "<rect x=\"{0:F1}\" y=\"{1:F1}\" width=\"{2:F1}\" height=\"{3:F1}\" fill=\"var(--engine)\" opacity=\"0.75\"/>",
                i * barW + BarOffset, height - barHeight, barW - BarWidthTrim, barHeight));
        }
        builder.Append($"<line class=\"axis\" x1=\"0\" y1=\"{height}\" x2=\"{width}\" y2=\"{height}\"/>");
        builder.Append(string.Format(Inv, "<line x1=\"{0:F1}\" y1=\"0\" x2=\"{0:F1}\" y2=\"{1}\" stroke=\"var(--log)\" stroke-width=\"3\"/>", ScaleX(log), height));
        builder.Append(string.Format(Inv, "<text x=\"{0:F1}\" y=\"{1}\" text-anchor=\"middle\">log {2}</text>", Math.Clamp(ScaleX(log), LogLabelMargin, width - LogLabelMargin), height + AxisLabelDrop, FormatNumber(log)));
        builder.Append($"<text x=\"0\" y=\"{height + AxisLabelDrop}\">{FormatNumber(lowest)}</text><text x=\"{width}\" y=\"{height + AxisLabelDrop}\" text-anchor=\"end\">{FormatNumber(highest)}</text>");
        builder.Append("</svg>");
        return builder.ToString();
    }

    private static string RatioClass(double ratio, double good, double warn)
    {
        var off = Math.Abs(1 - ratio);
        return off <= good ? "ok" : off <= warn ? "warn" : "bad";
    }

    private static string Rate(double crits, double hits) => hits > 0 ? (crits / hits).ToString("P1", Inv) : "-";
    private static string FormatNumber(double value) => value.ToString("N0", Inv);
    private static string FormatPercent(double value) => value.ToString("P1", Inv);
    private static string EncodeHtml(string text) => WebUtility.HtmlEncode(text);
}
