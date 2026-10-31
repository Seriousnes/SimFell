using System.Net;
using System.Text;

namespace FellowshipSim.Engine.Replay;

/// <summary>Renders a <see cref="ReplayResult"/> as a self-contained HTML review page and a short text summary.</summary>
public static class ReplayReportWriter
{

    public static string Summary(ReplayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Replay {result.ReportCode} fight {result.FightId} ({result.FightName}) - {result.PlayerName} ({result.Hero})");
        sb.AppendLine(result.HaltedAt == null
            ? $"Replayed all {result.Steps.Count} steps: {result.PassedSteps} passed, {result.Steps.Count - result.PassedSteps} failed"
            : $"HALTED at step {result.HaltedAt.Index}/{result.TotalSteps} ({result.HaltedAt.AbilityName} @ {result.HaltedAt.Time:F3}s); {result.Steps.Count(s => s.Index < result.HaltedAt.Index)} steps passed before it");
        if (result.ForcedCasts.Count > 0) sb.AppendLine($"  casts forced to follow the log: {result.ForcedCasts.Count}");
        if (result.HaltedAt == null && result.PassedSteps < result.Steps.Count)
        {
            var first = result.Steps.First(s => !s.Passed);
            sb.AppendLine($"  first failing step: {first.Index} ({first.AbilityName} @ {first.Time:F3}s)");
            foreach (var group in result.Steps.SelectMany(s => s.Issues).GroupBy(i => i.Category).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"  {group.Key}: {group.Count()} issues in {group.Select(i => i.Step).Distinct().Count()} steps");
                foreach (var sub in group.GroupBy(i => i.Subject ?? "other").OrderByDescending(x => x.Count()).Take(6))
                {
                    sb.AppendLine($"      {sub.Count(),6} x {sub.Key} (first at step {sub.Min(i => i.Step)})");
                }
            }
        }
        if (result.AllHits.Count > 0)
        {
            sb.AppendLine("  calibration (mean log/engine over matched hits; spread averages out to 1.000):");
            foreach (var row in Calibration(result))
            {
                sb.AppendLine($"    {row.Name,-26} {row.Id,8}  normal {Fmt(row.Normal)}  crit {Fmt(row.Crit)}");
            }
        }
        foreach (var issue in result.HaltIssues) sb.AppendLine($"  {issue}");
        sb.AppendLine($"  log hits matched: {result.MatchedLogHits}/{result.LogHits}; dealt by the replay: {result.LoggedHitsDealt}");
        return sb.ToString();
    }

    public static void Write(ReplayResult result, string path, string? navigation = null) => File.WriteAllText(path, Render(result, navigation));

    /// <summary>
    /// Engine and log damage totals of a replay: <see cref="ReplayResult.EngineTotalDamage"/> and the log hits' damage
    /// before absorbs and blocks (what the replay deals).
    /// </summary>
    public static (double Engine, double Log) DamageTotals(ReplayResult result) =>
        (result.EngineTotalDamage, result.LogDamage.Sum(d => d.UnblockedAmount));

    /// <summary>True for an engine hit; false for a log hit no engine hit matched.</summary>
    private static bool IsEngineHit(HitPair hit) => hit.Log == null || hit.LandedTime.HasValue;

    /// <summary>An index page linking the review page of every replayed fight, with headline numbers.</summary>
    public static string RenderIndex(IReadOnlyList<(ReplayResult Result, string File)> fights)
    {
        var sb = new StringBuilder();
        sb.Append(Head("Replay Reviews"));
        sb.Append("<h1>Replay reviews</h1><div class=\"muted\">Every fight replayed through the engine (survey mode: the whole fight, casts follow the log). Click a fight for its full review.</div>");
        sb.Append("<div class=\"panel scroll\"><table><thead><tr><th>Fight</th><th>Player</th><th>Report</th><th class=\"num\">Steps passed</th><th class=\"num\">Log hits matched</th><th class=\"num\">Log DPS</th><th class=\"num\">Engine DPS</th><th class=\"num\">Engine vs log</th><th class=\"num\">Forced casts</th></tr></thead><tbody>");
        foreach (var (result, file) in fights)
        {
            var (eng, log) = DamageTotals(result);
            double deviation = log > 0 ? eng / log - 1 : 0;
            double dur = Math.Max(1, result.DurationSeconds);
            string cls = Math.Abs(deviation) <= 0.03 ? "diff-ok" : "diff-bad";
            sb.Append($"<tr><td><a href=\"{EncodeHtml(file)}\">{EncodeHtml(result.FightName)}</a></td><td>{EncodeHtml(result.PlayerName)}</td><td>{EncodeHtml(result.ReportCode)} #{result.FightId}</td>");
            sb.Append($"<td class=\"num\">{result.PassedSteps} / {result.Steps.Count}</td><td class=\"num\">{(result.LogHits > 0 ? (double)result.MatchedLogHits / result.LogHits : 0):P1}</td>");
            sb.Append($"<td class=\"num\">{log / dur:N0}</td><td class=\"num\">{eng / dur:N0}</td><td class=\"num {cls}\">{deviation:+0.0%;-0.0%;0.0%}</td><td class=\"num\">{result.ForcedCasts.Count}</td></tr>");
        }
        sb.Append("</tbody></table></div></main></body></html>");
        return sb.ToString();
    }

    public sealed record CalibrationRow(string Name, int Id, (int N, double Mean) Normal, (int N, double Mean) Crit);

    /// <summary>Per damage ID: mean log/engine ratio of matched hits, separately for normal hits and crits.</summary>
    public static IEnumerable<CalibrationRow> Calibration(ReplayResult result) =>
        result.AllHits.Where(h => h.Ratio.HasValue)
            .GroupBy(h => h.AbilityId)
            .Select(g => new CalibrationRow(g.First().AbilityName, g.Key, Mean(g.Where(h => !h.Crit)), Mean(g.Where(h => h.Crit))))
            .OrderByDescending(row => row.Normal.N + row.Crit.N);

    private static (int, double) Mean(IEnumerable<HitPair> hits)
    {
        var ratios = hits.Select(h => h.Ratio!.Value).ToList();
        return (ratios.Count, ratios.Count == 0 ? 0 : ratios.Average());
    }

    private static string Fmt((int N, double Mean) value) => value.N == 0 ? "     -      " : $"{value.Mean:F3} (n={value.N,-4})";

    public static string Render(ReplayResult result, string? navigation = null)
    {
        var sb = new StringBuilder();
        sb.Append(Head("Replay Parity Review"));
        if (navigation != null) sb.Append(navigation);
        return RenderBody(sb, result);
    }

    internal static string Head(string title)
    {
        var sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Replay Parity Review</title>
<style>
:root{color-scheme:light dark;--bg:#f7f7f5;--panel:#fff;--text:#1d1d1b;--muted:#6b6b66;--line:#e2e1dc;--ok:#1f7a4d;--ok-bg:#e5f3ec;--bad:#b42318;--bad-bg:#fdecea;--warn:#9a6700;--warn-bg:#fff4d6;--accent:#3b5bdb}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--bg:#151514;--panel:#1e1e1c;--text:#ecebe6;--muted:#a3a29b;--line:#34332f;--ok:#5cc58f;--ok-bg:#16301f;--bad:#ff8a80;--bad-bg:#3a1a17;--warn:#f2c14e;--warn-bg:#3a3014;--accent:#8ea2ff}}
:root[data-theme="dark"]{--bg:#151514;--panel:#1e1e1c;--text:#ecebe6;--muted:#a3a29b;--line:#34332f;--ok:#5cc58f;--ok-bg:#16301f;--bad:#ff8a80;--bad-bg:#3a1a17;--warn:#f2c14e;--warn-bg:#3a3014;--accent:#8ea2ff}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:14px/1.45 system-ui,-apple-system,Segoe UI,sans-serif}
main{max-width:1180px;margin:0 auto;padding:24px 16px 64px}h1{font-size:22px;margin:0 0 4px}h2{font-size:16px;margin:28px 0 10px}
.muted{color:var(--muted)}.panel{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:14px 16px;margin:12px 0}
.halt{border-color:var(--bad);background:var(--bad-bg)}.done{border-color:var(--ok);background:var(--ok-bg)}
.tag{display:inline-block;font-size:11px;font-weight:600;padding:1px 7px;border-radius:999px;background:var(--line);margin-right:6px}
.tag.bad{background:var(--bad);color:var(--panel)}.ok{color:var(--ok)}.bad{color:var(--bad)}.warn{color:var(--warn)}
.scroll{overflow-x:auto}table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums}th,td{text-align:left;padding:4px 8px;border-bottom:1px solid var(--line);white-space:nowrap}
th{font-size:12px;color:var(--muted);font-weight:600}td.num,th.num{text-align:right}tr.fail td{background:var(--bad-bg)}tr.skip td{color:var(--muted)}
details{margin:8px 0}summary{cursor:pointer;font-weight:600}ul{margin:6px 0;padding-left:20px}code{font-size:12px}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:12px}
:root{--engine:#3b5bdb;--log:#d9480f}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--engine:#8ea2ff;--log:#ff922b}}
:root[data-theme="dark"]{--engine:#8ea2ff;--log:#ff922b}
.chart{width:100%;height:auto;display:block}.chart text{fill:var(--muted);font-size:11px}.chart .axis{stroke:var(--line)}
.legend{display:flex;gap:16px;font-size:12px;color:var(--muted);margin-bottom:4px}.legend i{display:inline-block;width:14px;height:3px;vertical-align:middle;margin-right:6px}
td.diff-bad{color:var(--bad)}td.diff-ok{color:var(--ok)}tr.total td{font-weight:600;border-top:2px solid var(--line)}
.fightnav{display:flex;flex-wrap:wrap;gap:6px;margin:0 0 14px}.fightnav a{font-size:12px;padding:3px 9px;border:1px solid var(--line);border-radius:999px;color:var(--text);text-decoration:none;background:var(--panel)}
button.state{font:inherit;font-size:12px;padding:1px 8px;border:1px solid var(--line);border-radius:6px;background:var(--panel);color:var(--accent);cursor:pointer}tr.state-row td{white-space:normal;background:var(--bg)}
.fightnav a.cur{background:var(--accent);border-color:var(--accent);color:var(--panel)}a{color:var(--accent)}
</style></head><body><main>
""");
        return sb.ToString().Replace("<title>Replay Parity Review</title>", $"<title>{title}</title>");
    }

    private static string RenderBody(StringBuilder sb, ReplayResult result)
    {
        sb.Append($"<h1>Replay parity review</h1><div class=\"muted\">{EncodeHtml(result.FightName)} · report {EncodeHtml(result.ReportCode)} fight {result.FightId} · {EncodeHtml(result.PlayerName)} ({EncodeHtml(result.Hero)})</div>");

        if (result.HaltedAt != null)
        {
            sb.Append($"<div class=\"panel halt\"><strong>Halted at step {result.HaltedAt.Index} of {result.TotalSteps}</strong> — {EncodeHtml(result.HaltedAt.AbilityName)} at {result.HaltedAt.Time:F3}s. ");
            sb.Append($"{result.Steps.Count(s => s.Index < result.HaltedAt.Index)} steps passed before it.<ul>");
            foreach (var issue in result.HaltIssues) sb.Append($"<li><span class=\"tag bad\">{issue.Category}</span>step {issue.Step} @ {issue.Time:F3}s — {EncodeHtml(issue.Message)}{EncodeHtml(RangeText(issue))}</li>");
            sb.Append("</ul></div>");
        }
        else
        {
            string cls = result.Completed ? "done" : "halt";
            sb.Append($"<div class=\"panel {cls}\"><strong>Replayed all {result.Steps.Count} steps</strong> — {result.PassedSteps} passed, {result.Steps.Count - result.PassedSteps} failed.</div>");
        }

        sb.Append($"<div class=\"muted\">Log hits matched by the engine: {result.MatchedLogHits:N0} of {result.LogHits:N0}</div>");
        if (result.ForcedCasts.Count > 0)
        {
            sb.Append($"<details class=\"panel\"><summary>{result.ForcedCasts.Count} casts forced to follow the log (the engine would have refused them)</summary><ul>");
            foreach (var forcedCast in result.ForcedCasts) sb.Append($"<li>{EncodeHtml(forcedCast)}</li>");
            sb.Append("</ul></details>");
        }
        RenderDamageOverview(sb, result);
        DisagreementsSection.Render(sb, result.Issues.Count > 0 ? result.Issues : result.Steps.SelectMany(s => s.Issues).ToList());

        // Every step with issues, collapsed (the halted step open); the "All steps" table links to them.
        var failing = result.Steps.Where(s => !s.Passed).ToList();
        if (failing.Count > 0)
        {
            var byCategory = failing.SelectMany(s => s.Issues).GroupBy(i => i.Category).OrderByDescending(g => g.Count());
            sb.Append($"<h2>Steps with issues ({failing.Count})</h2><div class=\"muted\">");
            sb.Append(string.Join(" · ", byCategory.Select(g => $"{g.Key}: {g.Count()}")));
            sb.Append("</div>");
            foreach (var step in failing) RenderStepDetail(sb, step, open: step.Index == result.HaltedAt?.Index);
        }

        sb.Append("<h2>All steps</h2><div class=\"panel scroll\"><table><thead><tr><th class=\"num\">#</th><th class=\"num\">Time</th><th>Action</th><th>Ability</th><th class=\"num\">Enemies</th><th class=\"num\">Hits</th><th class=\"num\">Log dmg / engine</th><th>State</th><th>Result</th></tr></thead><tbody>");
        foreach (var step in result.Steps)
        {
            var matched = step.Hits.Where(h => h.Log != null && h.LandedTime.HasValue).ToList();
            double engineSum = matched.Sum(h => h.EngineAmount), logSum = matched.Sum(h => h.Log!.UnblockedAmount);
            bool stateOk = step.Resources.All(c => c.Matches) && step.Auras.All(a => a.Matches);
            string cls = !step.Passed ? "fail" : !step.Modeled ? "skip" : "";
            string index = step.Passed ? step.Index.ToString() : $"<a href=\"#step-{step.Index}\">{step.Index}</a>";
            sb.Append($"<tr class=\"{cls}\"><td class=\"num\">{index}</td><td class=\"num\">{step.Time:F3}</td><td>{EncodeHtml(step.Kind)}</td><td>{EncodeHtml(step.AbilityName)}</td><td class=\"num\">{step.ActiveEnemies}</td>");
            sb.Append($"<td class=\"num\">{matched.Count}/{step.Hits.Count}</td><td class=\"num\">{(engineSum > 0 ? (logSum / engineSum).ToString("F3") : "—")}</td>");
            sb.Append($"<td>{(stateOk ? "<span class=ok>match</span>" : "<span class=bad>differs</span>")}</td>");
            sb.Append($"<td>{(!step.Modeled ? "skipped (no model)" : step.Passed ? "<span class=ok>pass</span>" : $"<span class=bad>{step.Issues.Count} issue(s)</span>")}</td></tr>");
        }
        sb.Append("</tbody></table></div>");

        sb.Append("<h2>Coverage</h2><div class=\"grid\">");
        RenderCounts(sb, "Abilities the engine does not model (skipped)", result.UnmodeledAbilities, "casts");
        RenderCounts(sb, "Player auras in the log with no engine model (not checked)", result.UntrackedLogAuras, "steps active");
        RenderCounts(sb, "Auras from other players on the player (no engine effect)", result.UnmodeledExternalAuras, "applications");
        sb.Append("</div>");

        if (result.ProcStats.Count > 0)
        {
            sb.Append("<h2>Proc rolls</h2><div class=\"panel scroll\"><table><thead><tr><th>Proc</th><th class=\"num\">Rolls</th><th class=\"num\">Expected procs</th><th class=\"num\">Log procs</th></tr></thead><tbody>");
            foreach (var procStat in result.ProcStats) sb.Append($"<tr><td>{EncodeHtml(procStat.Key)}</td><td class=\"num\">{procStat.Rolls}</td><td class=\"num\">{procStat.ExpectedProcs:F1}</td><td class=\"num\">{procStat.Procs}</td></tr>");
            sb.Append("</tbody></table><div class=\"muted\">Expected procs is the sum of the engine's chances; a large gap to the log count means the proc chance or its trigger is wrong.</div></div>");
        }

        sb.Append("</main></body></html>");
        return sb.ToString();
    }

    private const double DpsWindowSeconds = 10.0;

    /// <summary>Damage over time and DPS charts (engine vs log), then a per-ability breakdown.</summary>
    private static void RenderDamageOverview(StringBuilder sb, ReplayResult result)
    {
        var engineHits = result.AllHits.Where(IsEngineHit).ToList();
        double duration = Math.Max(result.DurationSeconds,
            Math.Max(result.LogDamage.Select(d => d.Time).DefaultIfEmpty(0).Max(), engineHits.Select(h => h.LandedTime ?? h.EngineTime).DefaultIfEmpty(0).Max()));
        if (duration <= 0) return;
        int bins = (int)Math.Ceiling(duration);

        double[] Bin(IEnumerable<(double T, double Amount)> hits)
        {
            var buckets = new double[bins];
            foreach (var (second, amount) in hits) buckets[Math.Clamp((int)second, 0, bins - 1)] += amount;
            return buckets;
        }
        var logBins = Bin(result.LogDamage.Select(d => (d.Time, d.UnblockedAmount)));
        var engBins = Bin(engineHits.Select(h => (h.LandedTime ?? h.EngineTime, h.EngineAmount)));

        double[] Cumulative(double[] values)
        {
            var totals = new double[values.Length];
            double sum = 0;
            for (int i = 0; i < values.Length; i++) totals[i] = sum += values[i];
            return totals;
        }
        double[] Rolling(double[] values)
        {
            int window = (int)DpsWindowSeconds;
            var averages = new double[values.Length];
            double sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
                if (i >= window) sum -= values[i - window];
                averages[i] = sum / Math.Min(window, i + 1);
            }
            return averages;
        }

        double logTotal = logBins.Sum(), engTotal = engBins.Sum();
        sb.Append("<h2>Damage overview</h2>");
        sb.Append($"<div class=\"grid\"><div class=\"panel\"><div class=\"muted\">Log damage</div><strong>{logTotal:N0}</strong> · {logTotal / duration:N0} DPS</div>");
        sb.Append($"<div class=\"panel\"><div class=\"muted\">Engine damage (average rolls of every engine hit)</div><strong>{engTotal:N0}</strong> · {engTotal / duration:N0} DPS</div>");
        sb.Append($"<div class=\"panel\"><div class=\"muted\">Engine vs log</div><strong>{(logTotal > 0 ? engTotal / logTotal - 1 : 0):+0.0%;-0.0%;0.0%}</strong> over {duration:F0}s</div></div>");

        sb.Append("<div class=\"panel\"><div class=\"legend\"><span><i style=\"background:var(--log)\"></i>Log</span><span><i style=\"background:var(--engine)\"></i>Engine</span></div>");
        sb.Append("<strong>Damage done over time</strong>");
        Chart(sb, Cumulative(logBins), Cumulative(engBins), duration);
        sb.Append($"<strong>DPS over time</strong> <span class=\"muted\">({DpsWindowSeconds:F0}s rolling average)</span>");
        Chart(sb, Rolling(logBins), Rolling(engBins), duration);
        sb.Append("</div>");

        RenderAbilityBreakdown(sb, result, engineHits, duration);
    }

    private static void Chart(StringBuilder sb, double[] log, double[] engine, double duration)
    {
        const double Width = 1100, Height = 240, Left = 64, Right = 12, Top = 10, Bottom = 26;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        double max = Math.Max(log.DefaultIfEmpty(0).Max(), engine.DefaultIfEmpty(0).Max());
        if (max <= 0) max = 1;
        double ScaleX(double second) => Left + (Width - Left - Right) * second / Math.Max(1, log.Length - 1);
        double ScaleY(double value) => Top + (Height - Top - Bottom) * (1 - value / max);
        string FormatNumber(double value) => value.ToString("F1", inv);
        string Points(double[] values) => string.Join(" ", values.Select((y, i) => $"{FormatNumber(ScaleX(i))},{FormatNumber(ScaleY(y))}"));

        sb.Append($"<svg class=\"chart\" viewBox=\"0 0 {Width} {Height}\" role=\"img\">");
        for (int g = 0; g <= 4; g++)
        {
            double value = max * g / 4;
            sb.Append($"<line class=\"axis\" x1=\"{Left}\" x2=\"{Width - Right}\" y1=\"{FormatNumber(ScaleY(value))}\" y2=\"{FormatNumber(ScaleY(value))}\"/>");
            sb.Append($"<text x=\"{Left - 6}\" y=\"{FormatNumber(ScaleY(value) + 4)}\" text-anchor=\"end\">{Short(value)}</text>");
        }
        for (int t = 0; t <= duration; t += 60)
        {
            sb.Append($"<text x=\"{FormatNumber(ScaleX(t))}\" y=\"{Height - 6}\" text-anchor=\"middle\">{t / 60}:00</text>");
        }
        sb.Append($"<polyline fill=\"none\" stroke=\"var(--log)\" stroke-width=\"1.6\" points=\"{Points(log)}\"/>");
        sb.Append($"<polyline fill=\"none\" stroke=\"var(--engine)\" stroke-width=\"1.6\" points=\"{Points(engine)}\"/>");
        sb.Append("</svg>");
    }

    private static string Short(double value) => value >= 1e6 ? $"{value / 1e6:0.#}M" : value >= 1e3 ? $"{value / 1e3:0.#}k" : $"{value:0}";

    private static void RenderAbilityBreakdown(StringBuilder sb, ReplayResult result, List<HitPair> engineHits, double duration)
    {
        var ids = result.LogDamage.Select(d => d.AbilityId).Union(engineHits.Select(h => h.AbilityId)).Distinct();
        var rows = ids.Select(id =>
        {
            var lh = result.LogDamage.Where(d => d.AbilityId == id).ToList();
            var eh = engineHits.Where(h => h.AbilityId == id).ToList();
            string name = lh.FirstOrDefault()?.AbilityName ?? eh.First().AbilityName;
            return (Id: id, Name: name,
                LogDmg: lh.Sum(d => d.UnblockedAmount), EngDmg: eh.Sum(h => h.EngineAmount),
                LogHits: lh.Count, EngHits: eh.Count,
                LogCrits: lh.Count(d => d.Crit), EngCrits: eh.Count(h => h.Crit),
                LogCasts: result.LogCasts.GetValueOrDefault(name), EngCasts: result.EngineCasts.GetValueOrDefault(name));
        }).OrderByDescending(x => x.LogDmg).ToList();
        double logTotal = rows.Sum(x => x.LogDmg), engTotal = rows.Sum(x => x.EngDmg);

        string Diff(double eng, double log)
        {
            if (log <= 0) return "<td class=\"num\">-</td>";
            double difference = eng / log - 1;
            return $"<td class=\"num {(Math.Abs(difference) <= 0.05 ? "diff-ok" : "diff-bad")}\">{(Math.Abs(difference) < 0.0005 ? 0 : difference):+0.0%;-0.0%;0.0%}</td>";
        }
        string Pct(int count, int of) => of == 0 ? "-" : $"{(double)count / of:P1}";
        string Avg(double dmg, int count) => count == 0 ? "-" : $"{dmg / count:N0}";
        string Casts(int count) => count == 0 ? "-" : count.ToString();

        sb.Append("<h2>Damage by ability</h2><div class=\"panel scroll\"><table><thead><tr>");
        sb.Append("<th>Ability</th><th class=\"num\">ID</th><th class=\"num\">Log damage</th><th class=\"num\">Engine damage</th><th class=\"num\">Diff</th><th class=\"num\">Share</th>");
        sb.Append("<th class=\"num\">Log DPS</th><th class=\"num\">Engine DPS</th><th class=\"num\">Log hits</th><th class=\"num\">Engine hits</th>");
        sb.Append("<th class=\"num\">Log crit</th><th class=\"num\">Engine crit</th><th class=\"num\">Log avg hit</th><th class=\"num\">Engine avg hit</th>");
        sb.Append("<th class=\"num\">Log casts</th><th class=\"num\">Engine casts</th></tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append($"<tr><td>{EncodeHtml(row.Name)}</td><td class=\"num\">{row.Id}</td><td class=\"num\">{row.LogDmg:N0}</td><td class=\"num\">{row.EngDmg:N0}</td>{Diff(row.EngDmg, row.LogDmg)}");
            sb.Append($"<td class=\"num\">{(logTotal > 0 ? row.LogDmg / logTotal : 0):P1}</td><td class=\"num\">{row.LogDmg / duration:N0}</td><td class=\"num\">{row.EngDmg / duration:N0}</td>");
            sb.Append($"<td class=\"num\">{row.LogHits:N0}</td><td class=\"num\">{row.EngHits:N0}</td><td class=\"num\">{Pct(row.LogCrits, row.LogHits)}</td><td class=\"num\">{Pct(row.EngCrits, row.EngHits)}</td>");
            sb.Append($"<td class=\"num\">{Avg(row.LogDmg, row.LogHits)}</td><td class=\"num\">{Avg(row.EngDmg, row.EngHits)}</td><td class=\"num\">{Casts(row.LogCasts)}</td><td class=\"num\">{Casts(row.EngCasts)}</td></tr>");
        }
        sb.Append($"<tr class=\"total\"><td>Total</td><td></td><td class=\"num\">{logTotal:N0}</td><td class=\"num\">{engTotal:N0}</td>{Diff(engTotal, logTotal)}<td class=\"num\">100%</td>");
        sb.Append($"<td class=\"num\">{logTotal / duration:N0}</td><td class=\"num\">{engTotal / duration:N0}</td><td class=\"num\">{rows.Sum(x => x.LogHits):N0}</td><td class=\"num\">{rows.Sum(x => x.EngHits):N0}</td>");
        sb.Append($"<td class=\"num\">{Pct(rows.Sum(x => x.LogCrits), rows.Sum(x => x.LogHits))}</td><td class=\"num\">{Pct(rows.Sum(x => x.EngCrits), rows.Sum(x => x.EngHits))}</td><td></td><td></td><td></td><td></td></tr>");
        sb.Append("</tbody></table></div>");
        sb.Append("<div class=\"muted\">Engine crits follow the log for matched hits (the replay takes crits from the log); engine damage is the average roll, before the ±10% spread. Log damage is before absorbs and blocks.</div>");
    }

    private static void RenderStepDetail(StringBuilder sb, ReplayStep step, bool open)
    {
        string status = !step.Modeled ? "skipped" : step.Passed ? "<span class=ok>pass</span>" : $"<span class=bad>{step.Issues.Count} issue(s)</span>";
        sb.Append($"<details class=\"panel\" id=\"step-{step.Index}\"{(open ? " open" : "")}><summary>Step {step.Index} · {step.Time:F3}s · {EncodeHtml(step.Kind)} {EncodeHtml(step.AbilityName)}{(step.Target != null ? $" → {EncodeHtml(step.Target)}" : "")} · {status}</summary>");
        if (step.Note != null) sb.Append($"<p class=\"muted\">{EncodeHtml(step.Note)}</p>");
        if (step.Issues.Count > 0)
        {
            sb.Append("<ul>");
            foreach (var issue in step.Issues) sb.Append($"<li><span class=\"tag bad\">{issue.Category}</span>@ {issue.Time:F3}s — {EncodeHtml(issue.Message)}{EncodeHtml(RangeText(issue))}</li>");
            sb.Append("</ul>");
        }

        sb.Append("<div class=\"grid\">");
        if (step.Resources.Count > 0)
        {
            sb.Append("<div><strong>Pre-cast resources</strong><table><tr><th>Resource</th><th class=num>Engine</th><th class=num>Log</th></tr>");
            foreach (var resource in step.Resources) sb.Append($"<tr><td>{EncodeHtml(resource.Resource)}</td><td class=num>{resource.Engine:0.##}</td><td class=\"num {(resource.Matches ? "ok" : "bad")}\">{resource.Log:0.##}</td></tr>");
            sb.Append("</table></div>");
        }
        if (step.Auras.Count > 0)
        {
            sb.Append("<div><strong>Pre-cast tracked auras (stacks)</strong><table><tr><th>Aura</th><th class=num>Engine</th><th class=num>Log</th></tr>");
            foreach (var aura in step.Auras) sb.Append($"<tr><td>{EncodeHtml(aura.Aura)}</td><td class=num>{aura.EngineStacks}</td><td class=\"num {(aura.Matches ? "ok" : "bad")}\">{aura.LogStacks}</td></tr>");
            sb.Append("</table></div>");
        }
        sb.Append("<div><strong>Context</strong><div class=\"muted\">");
        sb.Append($"Active enemies: {step.ActiveEnemies}<br>");
        sb.Append($"Engine haste {step.EngineHaste:P2}{(step.HasteSources.Count > 0 ? " from " + EncodeHtml(string.Join("; ", step.HasteSources)) : "")}<br>");
        if (step.UntrackedLogAuras.Count > 0) sb.Append($"Untracked log auras: {EncodeHtml(string.Join(", ", step.UntrackedLogAuras))}<br>");
        if (step.ExternalAurasOnPlayer.Count > 0) sb.Append($"Auras from others: {EncodeHtml(string.Join(", ", step.ExternalAurasOnPlayer))}");
        sb.Append("</div></div></div>");

        if (step.Hits.Count > 0)
        {
            sb.Append("<div class=\"scroll\"><table><thead><tr><th class=num>Engine t</th><th class=num>Log t</th><th>Ability</th><th>Target</th><th>Crit</th><th class=num>Engine avg</th><th class=num>Expected low</th><th class=num>Expected high</th><th class=num>Log</th><th class=num>Deviation</th></tr></thead><tbody>");
            foreach (var hit in step.Hits)
            {
                bool bad = !hit.WithinRange;
                sb.Append($"<tr class=\"{(bad ? "fail" : "")}\"><td class=num>{hit.EngineTime:F3}</td><td class=num>{(hit.Log != null ? hit.Log.Time.ToString("F3") : "—")}</td><td>{EncodeHtml(hit.AbilityName)}</td><td>{EncodeHtml(hit.Target?.ToString() ?? "")}</td>");
                sb.Append($"<td>{(hit.Log == null ? $"({hit.CritChance:P0})" : hit.Crit ? "crit" : "")}</td><td class=num>{hit.EngineAmount:F0}</td><td class=num>{hit.ExpectedLow:F0}</td><td class=num>{hit.ExpectedHigh:F0}</td>");
                sb.Append($"<td class=num>{(hit.Log != null ? EncodeHtml(hit.Log.ComparedAmountText()) : "none")}</td><td class=num>{(hit.Deviation == 0 ? "—" : hit.Deviation.ToString("+0;-0"))}</td></tr>");
            }
            sb.Append("</tbody></table></div>");
        }
        sb.Append("</details>");
    }

    /// <summary>The expected range, observed value and deviation of an issue with a numeric check, for a line of text.</summary>
    private static string RangeText(ReplayIssue issue) =>
        issue.Expected is { } expected
            ? $" (expected {expected}, observed {(issue.Observed is { } observed ? ExpectedRange.Format(observed, expected.Unit) : "none")}{(issue.Deviation is { } deviation ? $", deviation {DeviationText(deviation, expected.Unit)}" : "")})"
            : "";

    private static string DeviationText(double deviation, string unit) => (deviation > 0 ? "+" : "") + ExpectedRange.Format(deviation, unit);

    private static void RenderCounts(StringBuilder sb, string title, Dictionary<string, int> counts, string unit)
    {
        sb.Append($"<div class=\"panel\"><strong>{EncodeHtml(title)}</strong>");
        if (counts.Count == 0)
        {
            sb.Append("<div class=\"muted\">None</div></div>");
            return;
        }
        sb.Append("<ul>");
        foreach (var (name, count) in counts.OrderByDescending(kv => kv.Value)) sb.Append($"<li>{EncodeHtml(name)} <span class=\"muted\">({count} {unit})</span></li>");
        sb.Append("</ul></div>");
    }

    private static string EncodeHtml(string text) => WebUtility.HtmlEncode(text);
}
