using System.Text.Json.Nodes;

namespace FellowshipSim.Engine.Replay;

/// <summary>A replay review of one player in a raw fight: the replay script, the replay, and the review's files.</summary>
public static class ReplayReview
{
    /// <summary>
    /// Builds the player's replay script from the raw fight, replays it and renders the review.
    /// <paramref name="cancellationToken"/> stops it between steps with an <see cref="OperationCanceledException"/>.
    /// </summary>
    public static ReviewArtifacts Run(JsonObject raw, int playerId, ReplayOptions? options = null, IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var player = ReplayScriptBuilder.Players(raw).FirstOrDefault(p => p.Id == playerId)
            ?? throw new ArgumentException($"Player {playerId} did not take part in fight {raw["fight"]?["id"]}.");
        if (!ReplayDriver.SupportsHero(player.Hero))
        {
            throw new NotSupportedException($"{player.Name} plays {player.Hero}, which is not supported yet (supported: {string.Join(", ", ReplayDriver.SupportedHeroes)}).");
        }

        progress?.Report("Building the replay script");
        var scriptJson = ReplayScriptBuilder.Build(raw, playerId).ToJsonString();

        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report("Replaying the fight");
        return Run(ReplayScript.FromJson(scriptJson), scriptJson, playerId, options, cancellationToken);
    }

    /// <summary>Replays a script and renders its review.</summary>
    public static ReviewArtifacts Run(ReplayScript script, string scriptJson, int playerId, ReplayOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ReplayOptions { StopOnFirstFailure = false };
        var hero = ReplayDriver.CreateHero(script);
        var result = new ReplayDriver(hero, script, options).Run(cancellationToken);

        string id = ReplayLibrary.ReviewId(script.ReportCode, script.FightId, playerId);
        var (engineDamage, logDamage) = ReplayReportWriter.DamageTotals(result);
        var summary = new ReviewSummary(id, script.ReportCode, script.FightId, script.FightName, script.FightDate, playerId,
            result.PlayerName, script.Player.Hero, result.PassedSteps, result.Steps.Count, result.MatchedLogHits, result.LogHits,
            engineDamage, logDamage, result.ForcedCasts.Count, DateTime.UtcNow);

        return new ReviewArtifacts(summary, scriptJson, ReplayReportWriter.Render(result), ReplayReportWriter.Summary(result), HitsCsv(result));
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
}
