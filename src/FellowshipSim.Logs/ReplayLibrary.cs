using System.Text.Json;
using System.Text.Json.Nodes;
using FellowshipSim.Logs.Replay;

namespace FellowshipSim.Logs;

/// <summary>A replay review that has been run: what was replayed and how closely the engine matched the log.</summary>
public sealed record ReviewSummary(
    string Id,
    string ReportCode,
    int FightId,
    string FightName,
    DateTime? FightDate,
    int PlayerId,
    string PlayerName,
    string Hero,
    int PassedSteps,
    int TotalSteps,
    int MatchedHits,
    int LogHits,
    double EngineDamage,
    double LogDamage,
    int ForcedCasts,
    DateTime CreatedAt)
{
    public string HtmlFile => $"{Id}.html";
}

/// <summary>
/// The local library of fights and replay reviews, kept in a data folder:
/// <c>fights/</c> raw fights downloaded from FellowshipLogs, <c>scripts/</c> replay scripts built from them, and
/// <c>reviews/</c> the review pages with a summary of each.
/// </summary>
public sealed class ReplayLibrary
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public ReplayLibrary(string dataDirectory)
    {
        DataDirectory = Path.GetFullPath(dataDirectory);
        Directory.CreateDirectory(FightsDirectory);
        Directory.CreateDirectory(ScriptsDirectory);
        Directory.CreateDirectory(ReviewsDirectory);
    }

    public string DataDirectory { get; }
    public string FightsDirectory => Path.Combine(DataDirectory, "fights");
    public string ScriptsDirectory => Path.Combine(DataDirectory, "scripts");
    public string ReviewsDirectory => Path.Combine(DataDirectory, "reviews");

    public static string ReviewId(string reportCode, int fightId, int playerId) => $"{reportCode}_fight{fightId}_p{playerId}";

    private string RawFightPath(string reportCode, int fightId) => Path.Combine(FightsDirectory, $"{reportCode}_fight{fightId}.raw.json");

    /// <summary>The fight's raw events, downloaded from FellowshipLogs the first time and read from disk after that.</summary>
    public async Task<JsonObject> GetRawFightAsync(FellowshipLogsClient? client, string reportCode, int fightId,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        string path = RawFightPath(reportCode, fightId);
        if (File.Exists(path))
        {
            progress?.Report("Using the downloaded fight");
            return JsonNode.Parse(await File.ReadAllTextAsync(path, ct))!.AsObject();
        }
        if (client == null) throw new FellowshipLogsException("Set your FellowshipLogs API client ID and secret to download fights.");
        progress?.Report("Downloading the fight from FellowshipLogs");
        var raw = await client.DownloadFightAsync(reportCode, fightId, progress, ct);
        await File.WriteAllTextAsync(path, raw.ToJsonString(), ct);
        return raw;
    }

    /// <summary>Downloads (if needed) the fight, builds the player's replay script, replays it and writes the review.</summary>
    public async Task<ReviewSummary> RunReviewAsync(FellowshipLogsClient? client, string reportCode, int fightId, int playerId,
        ReplayOptions? options = null, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var raw = await GetRawFightAsync(client, reportCode, fightId, progress, ct);
        var player = ReplayScriptBuilder.Players(raw).FirstOrDefault(p => p.Id == playerId)
            ?? throw new ArgumentException($"Player {playerId} did not take part in fight {fightId}.");
        if (!ReplayDriver.SupportsHero(player.Hero))
        {
            throw new NotSupportedException($"{player.Name} plays {player.Hero}, which is not supported yet (supported: {string.Join(", ", ReplayDriver.SupportedHeroes)}).");
        }

        progress?.Report("Building the replay script");
        string id = ReviewId(reportCode, fightId, playerId);
        var scriptJson = ReplayScriptBuilder.Build(raw, playerId).ToJsonString();
        await File.WriteAllTextAsync(Path.Combine(ScriptsDirectory, $"{id}.replay.json"), scriptJson, ct);

        progress?.Report("Replaying the fight");
        var summary = Review(ReplayScript.FromJson(scriptJson), playerId, options);
        progress?.Report("Done");
        return summary;
    }

    /// <summary>Replays a script and writes its review page and summary.</summary>
    public ReviewSummary Review(ReplayScript script, int playerId, ReplayOptions? options = null)
    {
        options ??= new ReplayOptions { StopOnFirstFailure = false };
        var hero = ReplayDriver.CreateHero(script);
        var result = new ReplayDriver(hero, script, options).Run();

        string id = ReviewId(script.ReportCode, script.FightId, playerId);
        var (engineDamage, logDamage) = ReplayReportWriter.DamageTotals(result);
        var summary = new ReviewSummary(id, script.ReportCode, script.FightId, script.FightName, script.FightDate, playerId,
            result.PlayerName, script.Player.Hero, result.PassedSteps, result.Steps.Count, result.MatchedLogHits, result.LogHits,
            engineDamage, logDamage, result.ForcedCasts.Count, DateTime.UtcNow);

        const string navigation = "<nav class=\"fightnav\"><a href=\"/\">&larr; All reviews</a></nav>";
        ReplayReportWriter.Write(result, Path.Combine(ReviewsDirectory, summary.HtmlFile), navigation);
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}.summary.json"), JsonSerializer.Serialize(summary, Indented));
        // The same review as plain text: issues grouped by kind, and the damage calibration per ability.
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}.summary.txt"), ReplayReportWriter.Summary(result));
        // Every engine hit with the log hit it matched, for checking damage formulas in a spreadsheet.
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}.hits.csv"), HitsCsv(result));
        return summary;
    }

    private static string HitsCsv(ReplayResult result)
    {
        var sb = new System.Text.StringBuilder("time,ability_id,ability,target,engine_amount,engine_crit,crit_chance,log_amount,log_crit,log_time,tick_fraction,player_auras\n");
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var h in result.AllHits)
        {
            string name = h.AbilityName.Replace(',', ' ');
            // The engine's buffs on the player when the hit landed: log IDs, then engine-only auras by name.
            string auras = h.Context == null ? "" : string.Join(" ",
                h.Context.AuraLogIds.Select(id => id.ToString(inv)).Concat(h.Context.InternalAuras.Select(n => n.Replace(' ', '_').Replace(',', '_'))));
            sb.Append(string.Format(inv, "{0:F3},{1},{2},{3},{4:F1},{5},{6:F4},{7},{8},{9},{10:F3},{11}\n",
                h.EngineTime, h.AbilityId, name, h.Target == null ? "" : $"{h.Target.Id}.{h.Target.Instance}",
                h.EngineAmount, h.Crit ? 1 : 0, h.CritChance,
                h.Log == null ? "" : h.Log.TotalAmount.ToString("F0", inv), h.Log == null ? "" : (h.Log.Crit ? "1" : "0"),
                h.Log == null ? "" : h.Log.Time.ToString("F3", inv), h.TickFraction, auras));
        }
        return sb.ToString();
    }

    /// <summary>Every review run so far, newest first.</summary>
    public IReadOnlyList<ReviewSummary> ListReviews() =>
        Directory.GetFiles(ReviewsDirectory, "*.summary.json")
            .Select(f => JsonSerializer.Deserialize<ReviewSummary>(File.ReadAllText(f)))
            .Where(s => s != null && File.Exists(Path.Combine(ReviewsDirectory, s.HtmlFile)))
            .OrderByDescending(s => s!.CreatedAt)
            .ToList()!;

    /// <summary>Fights already downloaded (report code, fight ID).</summary>
    public IReadOnlyList<(string ReportCode, int FightId)> ListDownloadedFights() =>
        Directory.GetFiles(FightsDirectory, "*.raw.json")
            .Select(f => Path.GetFileName(f).Replace(".raw.json", ""))
            .Select(n => n.Split("_fight"))
            .Where(p => p.Length == 2 && int.TryParse(p[1], out _))
            .Select(p => (p[0], int.Parse(p[1])))
            .ToList();
}
