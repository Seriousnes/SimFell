using FellowshipSim.Core.Engine;
using FellowshipSim.Engine.Replay;
using FellowshipSim.Logs;
using Xunit.Abstractions;

namespace FellowshipSim.Tests;

/// <summary>
/// Replays every downloaded fight (data/fights) for its Mara players alone, and prints per damage ID how the engine's
/// hits compare with the log's. With no such fight downloaded it does nothing.
/// Run with: dotnet test --filter MaraReplayTests
/// </summary>
public class MaraReplayTests(ITestOutputHelper output)
{
    private const string CharacterFolderVariable = "MARA_CHARACTER_FOLDER";
    private const string StepsVariable = "MARA_STEPS";
    private const string BreakdownVariable = "MARA_BREAKDOWN";
    private const string HitsFolderVariable = "MARA_HITS_FOLDER";

    [Fact]
    public async Task Mara_Fights_Replay()
    {
        var library = new ReplayLibrary(Path.Combine(TestPaths.RepositoryRoot, TestPaths.DataFolder));
        foreach (var (reportCode, fightId) in library.ListDownloadedFights())
        {
            var raw = await library.GetRawFightAsync(null, reportCode, fightId);
            foreach (var player in ReplayScriptBuilder.Players(raw).Where(p => p.Hero == "Mara"))
            {
                var script = ReplayScript.FromJson(ReplayScriptBuilder.Build(raw, player.Id).ToJsonString());
                if (Environment.GetEnvironmentVariable(CharacterFolderVariable) is { } characterFolder)
                {
                    File.WriteAllText(Path.Combine(characterFolder, $"character_{reportCode}_{fightId}.json"), System.Text.Json.JsonSerializer.Serialize(script.Character, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    // A profile names the augmentations the engine has: the others are left out.
                    var build = System.Text.Json.JsonSerializer.Deserialize<FellowshipSim.Core.Import.CharacterBuild>(System.Text.Json.JsonSerializer.Serialize(script.Character))!;
                    build.Augmentations = build.Augmentations.Where(t => FellowshipSim.Core.Catalog.CreateAugmentation(t.Id, t.Rank) != null).ToList();
                    File.WriteAllText(Path.Combine(characterFolder, $"profile_{reportCode}_{fightId}.simc"), new FellowshipSim.Core.Import.SimcProfile(build, "").Format());
                }
                var hero = ReplayDriver.CreateHero(script);
                var options = new ReplayOptions { StopOnFirstFailure = false };
                var result = new ReplayDriver(hero, script, options).Run();
                var (engineDamage, logDamage) = ReplayReportWriter.DamageTotals(result);
                output.WriteLine($"{player.Name} {reportCode} fight {fightId}: {result.MatchedLogHits}/{result.LogHits} hits, " +
                                 $"damage {engineDamage / logDamage:P1} of log, {result.PassedSteps}/{result.Steps.Count} clean steps, " +
                                 $"{result.ForcedCasts.Count} forced casts");
                Report(result, options);
                ExactReplayAssertions.Hold(result);
            }
        }
    }

    private void Report(ReplayResult result, ReplayOptions options)
    {
        output.WriteLine("");
        output.WriteLine("id        name                  log hits  matched  engine-only  log dmg      engine dmg   in spread  ratio(log/engine)");
        var logById = result.LogDamage.GroupBy(h => h.AbilityId).ToDictionary(g => g.Key, g => g.ToList());
        var engineById = result.AllHits.GroupBy(h => h.AbilityId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var id in logById.Keys.Union(engineById.Keys).OrderByDescending(id => logById.GetValueOrDefault(id)?.Sum(h => h.TotalAmount) ?? 0))
        {
            var log = logById.GetValueOrDefault(id) ?? [];
            var engine = engineById.GetValueOrDefault(id) ?? [];
            var matched = engine.Where(h => h.Log != null && h.LandedTime.HasValue).ToList();
            var ratio = matched.Sum(h => h.EngineAmount) > 0 ? matched.Sum(h => h.Log!.UnblockedAmount) / matched.Sum(h => h.EngineAmount) : 0;
            var name = (engine.FirstOrDefault()?.AbilityName ?? log.FirstOrDefault()?.AbilityName ?? "?").PadRight(20)[..20];
            output.WriteLine($"{id,-9} {name}  {log.Count,8}  {matched.Count,7}  {engine.Count - matched.Count,11}  {log.Sum(h => h.TotalAmount),11:N0}  {engine.Sum(h => h.EngineAmount),11:N0}  " +
                             $"{matched.Count(h => h.WithinRange),9}  {ratio,6:F3}");
        }
        output.WriteLine("");
        foreach (var group in result.Steps.SelectMany(s => s.Issues).GroupBy(i => new IssueKey(i.Category, i.Subject)).OrderByDescending(g => g.Count()).Take(40))
        {
            output.WriteLine($"{group.Count(),5} [{group.Key.Category}] {group.Key.Subject}: {group.First().Message}");
        }
        output.WriteLine("");
        foreach (var unmodeled in result.UnmodeledAbilities) output.WriteLine($"unmodeled ability {unmodeled.Key}: {unmodeled.Value}");
        foreach (var untracked in result.UntrackedLogAuras.OrderByDescending(kv => kv.Value).Take(25)) output.WriteLine($"untracked log aura {untracked.Key}: {untracked.Value} steps");
        foreach (var unmodeled in result.UnmodeledCustomizations) output.WriteLine($"unmodeled {unmodeled}");
        foreach (var proc in result.ProcStats) output.WriteLine($"proc {proc.Key}: {proc.Procs}/{proc.Rolls} (expected {proc.ExpectedProcs:F1})");
        if (Environment.GetEnvironmentVariable(StepsVariable) is { } range)
        {
            // Every step between two times, with its resources, auras that disagree and issues.
            var bounds = range.Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            foreach (var step in result.Steps.Where(s => s.Time >= bounds[0] && s.Time <= bounds[1]))
            {
                output.WriteLine($"step {step.Index} @ {step.Time:F3} {step.AbilityName} {step.Note} | " +
                                 string.Join(" ", step.Resources.Select(r => $"{r.Resource}={r.Engine:F1}/{r.Log:F1}")));
                foreach (var issue in step.Issues) output.WriteLine($"    {issue.Message}");
            }
        }
        // How far the engine's resources are from the log's before the steps, below the maximum.
        foreach (var resourceGroup in result.Steps.SelectMany(s => s.Resources).Where(r => r.Resource != "Spirit" && r.Log < 195).GroupBy(r => r.Resource))
        {
            var diffs = resourceGroup.Select(r => r.Engine - r.Log).OrderBy(d => d).ToList();
            output.WriteLine($"{resourceGroup.Key}: {diffs.Count} readings, engine - log median {diffs[diffs.Count / 2]:F2}, p10 {diffs[diffs.Count / 10]:F2}, " +
                             $"p90 {diffs[diffs.Count * 9 / 10]:F2}, within tolerance {resourceGroup.Count(r => r.Matches) * 100.0 / diffs.Count:F1}%");
        }
        if (Environment.GetEnvironmentVariable(BreakdownVariable) is { } ids)
        {
            // The log/engine ratio of every matched hit, per minute of the fight.
            var all = result.AllHits.Where(h => h.Log != null && h.EngineAmount > 0 && h.Log.Overkill <= 0).ToList();
            foreach (var minuteGroup in all.GroupBy(h => (int)(h.EngineTime / 60)).OrderBy(g => g.Key))
            {
                output.WriteLine($"minute {minuteGroup.Key}: {minuteGroup.Count()} hits, ratio {minuteGroup.Sum(h => h.Log!.UnblockedAmount) / minuteGroup.Sum(h => h.EngineAmount):F3}");
            }
            // For each damage ID asked for: the log/engine ratio of matched hits with and without each aura of the hero,
            // and by the number of enemies.
            foreach (var id in ids.Split(',').Select(int.Parse))
            {
                var matched = result.AllHits.Where(h => h.AbilityId == id && h.Log != null && h.EngineAmount > 0 && h.Log.Overkill <= 0).ToList();
                double Ratio(IEnumerable<HitPair> hits) => hits.Sum(h => h.Log!.UnblockedAmount) / Math.Max(Tolerance.Comparison, hits.Sum(h => h.EngineAmount));
                output.WriteLine($"breakdown {id}: {matched.Count} hits, ratio {Ratio(matched):F3} (crit {Ratio(matched.Where(h => h.Crit)):F3}, normal {Ratio(matched.Where(h => !h.Crit)):F3})");
                foreach (var critGroup in matched.Where(h => h.Crit).GroupBy(h => h.CritChance >= 1.0))
                {
                    output.WriteLine($"  crits with engine crit chance {(critGroup.Key ? ">= 100%" : "< 100%")}: {critGroup.Count()} {Ratio(critGroup):F3}");
                }
                foreach (var abilityGroup in matched.GroupBy(h => h.AbilityName)) output.WriteLine($"  as {abilityGroup.Key}: {abilityGroup.Count()} {Ratio(abilityGroup):F3}");
                foreach (var cloneGroup in matched.Where(h => h.AbilityName.EndsWith("(shadowy clone)")).GroupBy(h => h.State?.Hero.Auras.Any(a => a.LogId == 1002245) == true)) output.WriteLine($"  clone with Assassin's Guile {cloneGroup.Key}: {cloneGroup.Count()} {Ratio(cloneGroup):F3}");
                foreach (var aura in matched.SelectMany(h => h.State?.Hero.Auras.Where(a => a.LogId > 0).Select(a => a.LogId) ?? []).Distinct().OrderBy(a => a))
                {
                    var with = matched.Where(h => h.State!.Hero.Auras.Any(a => a.LogId == aura)).ToList();
                    var without = matched.Except(with).ToList();
                    if (with.Count < 5 || without.Count < 5) continue;
                    output.WriteLine($"  aura {aura}: with {with.Count} {Ratio(with):F3}, without {without.Count} {Ratio(without):F3}");
                }
                var together = matched.GroupBy(h => Math.Round(h.EngineTime, 3)).ToDictionary(g => g.Key, g => g.Count());
                foreach (var togetherGroup in matched.GroupBy(h => together[Math.Round(h.EngineTime, 3)]).OrderBy(g => g.Key))
                {
                    output.WriteLine($"  {togetherGroup.Key} hit together: {togetherGroup.Count()} {Ratio(togetherGroup):F3} (crit {Ratio(togetherGroup.Where(h => h.Crit)):F3}, normal {Ratio(togetherGroup.Where(h => !h.Crit)):F3})");
                }
            }
        }
        foreach (var forced in result.ForcedCasts.GroupBy(f => f.Split(':')[0]).OrderByDescending(g => g.Count()).Take(15))
        {
            output.WriteLine($"forced {forced.Count()}: {forced.First()}");
        }
        var folder = Environment.GetEnvironmentVariable(HitsFolderVariable);
        if (folder == null) return;
        var dump = Path.Combine(folder, $"hits_{result.ReportCode}_{result.FightId}.csv");
        var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
        File.WriteAllLines(dump, result.AllHits.Select(h => string.Format(invariantCulture, "{0:F3},{1},{2},{3:F1},{4},{5},{6},{7:F3}",
            h.EngineTime, h.AbilityId, h.Target == null ? "" : $"{h.Target.Id}.{h.Target.Instance}", h.EngineAmount, h.Crit ? 1 : 0,
            h.Log == null ? "" : h.Log.UnblockedAmount.ToString("F0", invariantCulture), h.Log == null ? "" : h.Log.Time.ToString("F3", invariantCulture), h.TickFraction)));
    }
}
