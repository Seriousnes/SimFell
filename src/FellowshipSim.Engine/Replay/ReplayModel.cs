namespace FellowshipSim.Engine.Replay;

public class ReplayOptions
{
    /// <summary>Default of <see cref="PartialTickFractionTolerance"/>.</summary>
    public const double DefaultPartialTickFractionTolerance = 0.1;

    /// <summary>Default of <see cref="DefaultTravelSeconds"/>.</summary>
    public const double FallbackTravelSeconds = 0.5;

    /// <summary>Default of <see cref="PartialTickRatio"/>.</summary>
    public const double DefaultPartialTickRatio = 0.6;

    /// <summary>Default of <see cref="PartialMismatchPenaltySeconds"/>.</summary>
    public const double DefaultPartialMismatchPenaltySeconds = 0.05;

    /// <summary>Default of <see cref="GlobalCooldownToleranceSeconds"/>.</summary>
    public const double DefaultGlobalCooldownToleranceSeconds = 0.1;

    /// <summary>Default of <see cref="EnemyAriseLeadSeconds"/>.</summary>
    public const double DefaultEnemyAriseLeadSeconds = 0.1;

    /// <summary>Default of <see cref="ChannelStopLeadSeconds"/>.</summary>
    public const double DefaultChannelStopLeadSeconds = 0.010;

    /// <summary>Default of <see cref="AvoidedTargetPenaltySeconds"/>.</summary>
    public const double DefaultAvoidedTargetPenaltySeconds = 0.2;

    /// <summary>Default of <see cref="HitEarlySeconds"/>.</summary>
    public const double DefaultHitEarlySeconds = 0.15;

    /// <summary>Default of <see cref="HitLateSeconds"/>.</summary>
    public const double DefaultHitLateSeconds = 2.0;

    /// <summary>Default of <see cref="HitGroupSeconds"/>.</summary>
    public const double DefaultHitGroupSeconds = 0.010;

    /// <summary>Default of <see cref="SettleSeconds"/>.</summary>
    public const double DefaultSettleSeconds = 2.1;

    /// <summary>Default of <see cref="ProcEarlySeconds"/>.</summary>
    public const double DefaultProcEarlySeconds = 0.05;

    /// <summary>Default of <see cref="ProcAuraWindowSeconds"/>.</summary>
    public const double DefaultProcAuraWindowSeconds = 0.1;

    /// <summary>Default of <see cref="SameTickSeconds"/>.</summary>
    public const double DefaultSameTickSeconds = 0.005;

    /// <summary>Default of <see cref="CastTimeToleranceSeconds"/>.</summary>
    public const double DefaultCastTimeToleranceSeconds = 0.1;

    /// <summary>Default of <see cref="AuraToleranceSeconds"/>.</summary>
    public const double DefaultAuraToleranceSeconds = 0.1;

    /// <summary>Default of <see cref="ResourceSnapshotWindowSeconds"/>.</summary>
    public const double DefaultResourceSnapshotWindowSeconds = 0.002;

    /// <summary>Window (s) in which a Chain Lightning log hit may land after the engine hit (<see cref="HitLateSecondsById"/>).</summary>
    public const double ChainLightningHitLateSeconds = 0.25;

    /// <summary>Window (s) of a Multishot multi-target hit (<see cref="HitGroupSecondsById"/>).</summary>
    public const double MultishotHitGroupSeconds = 0.2;

    /// <summary>
    /// Free dice: the engine rolls its own crits, procs and damage spread with this seed; the log only supplies the
    /// casts, enemies and situational auras, and nothing is compared step by step (see <see cref="FreeDiceReplay"/>).
    /// </summary>
    public int? FreeDiceSeed { get; set; }

    /// <summary>Stop at the first step with any issue, for review.</summary>
    public bool StopOnFirstFailure { get; set; } = true;
    public int? MaxSteps { get; set; }

    /// <summary>Uniform damage spread of the game (+/- fraction of the average roll).</summary>
    public double DamageSpread { get; set; } = FellowshipSim.Core.Engine.RandomOutcomeSource.DamageSpread;
    /// <summary>
    /// Extra room on the high side of the damage band for an unexplained offset (0 = none). The former ~3.4% offset is
    /// explained by Rime's class tuning aura.
    /// </summary>
    public double UnexplainedHighMargin { get; set; } = 0.0;

    /// <summary>Unmatched hits on a target that dies in the log before they would land hit nothing.</summary>
    public bool MissHitsOnDyingTargets { get; set; } = true;

    /// <summary>
    /// Damage IDs whose hits the log never shows landing hit nothing (no damage, procs or counters), on any target.
    /// Combat logs: 12 of 1,248 Frost Bolt casts (1029) in 15 Rime fights complete with no Frost Bolt hit on their living
    /// target, and
    /// The Vehement's counter (effect 3194) does not count them (4Ln6PQZCRzc7Wpta fight 77 @ 116.780, fight 78 @
    /// 1311.371, GWChJ8P7tn6fHgV3 fight 22 @ 597.179, Rzg1nQNHpm2fhYtB fight 23 @ 981.460).
    /// </summary>
    public HashSet<int> UnmatchedHitsMissIds { get; set; } = [Core.Heroes.Rime.Abilities.FrostBolt.AbilityId];
    /// <summary>
    /// Abilities whose cast the game ends at its completion with nothing done, by the log ID of the debuff a completed
    /// cast applies: a cast the log completes with no application of it by the player within a server frame is cancelled
    /// as it would complete. Bursting Ice (1031): at the cast's end its target must pass the alignment filters, else the
    /// ability is cancelled before the commit of its cooldown and its one application, 1395 (game-data-authority,
    /// GA_Rime_CastedDebuffAoeDamage ExecuteUbergraph: OnFinished 1693, OnAbilityLateTargetValidationFailed and
    /// K2_CancelAbility 1344 / 1124, FellowshipCommitHeroAbility 1821, the application 2320). Combat logs: 7 of 797
    /// completed Bursting Ice casts in 15 Rime fights apply no 1395 to their living target, and in 5 of them the next
    /// Bursting Ice begins 1.1-7.2s later, inside its 10s cooldown (4Ln6PQZCRzc7Wpta fight 78 @ 252.915,
    /// D1fBhVPkwYKXWbpr fight 1 @ 123.825, GWChJ8P7tn6fHgV3 fight 22 @ 320.357).
    /// </summary>
    public Dictionary<int, int> CastProofDebuffIds { get; set; } = new()
    {
        [Core.Heroes.Rime.Abilities.BurstingIce.AbilityId] = Core.Domain.LogIds.Effect(Core.Heroes.Rime.Abilities.BurstingIce.DebuffEffectId),
    };
    /// <summary>
    /// A partial final tick's fraction depends on where the game's 33ms server frames fall, about +/-0.1 of a full tick;
    /// its damage may differ from the engine's by that much of a full tick on top of the spread.
    /// </summary>
    public double PartialTickFractionTolerance { get; set; } = DefaultPartialTickFractionTolerance;
    /// <summary>
    /// A hit that picks its own target takes the free log hit landing closest to its release plus the ID's typical
    /// travel time (learned from matched hits; this until there are any). A log hit on the hero's current target counts
    /// this much further away, leaving it for hits that can only go there (e.g. Cascading Blitz swallows share the
    /// Frost Swallows ID).
    /// </summary>
    public double DefaultTravelSeconds { get; set; } = FallbackTravelSeconds;
    /// <summary>
    /// A group dealing under this fraction of the previous hits (median) looks like a partial tick; a partial tick
    /// matching a full-looking group (or the reverse) counts this many seconds further away.
    /// </summary>
    public double PartialTickRatio { get; set; } = DefaultPartialTickRatio;
    public double PartialMismatchPenaltySeconds { get; set; } = DefaultPartialMismatchPenaltySeconds;
    /// <summary>
    /// Cast everything the log casts, 1:1: when the engine would refuse (cooldown, resources, global cooldown, still
    /// casting) the cast is made anyway and listed as forced, without failing the step.
    /// </summary>
    public bool ForceCasts { get; set; } = true;
    /// <summary>A cast may come this much before the engine's global cooldown ends (server frames and latency).</summary>
    public double GlobalCooldownToleranceSeconds { get; set; } = DefaultGlobalCooldownToleranceSeconds;
    /// <summary>Enemies join this long before their first event in the log (their arrival is only known from it).</summary>
    public double EnemyAriseLeadSeconds { get; set; } = DefaultEnemyAriseLeadSeconds;
    /// <summary>
    /// A channel stops this long before its endchannel event: the stop is logged a few milliseconds into its server frame
    /// and the frame's tick does not happen. Combat logs (15 Rime fights of patch 0.5.0.0, 93 Freezing Torrent ticks the
    /// engine had within a frame before a logged stop): 62 of 64 ticks 0-8ms before it have no log tick, 28 of 29 ticks
    /// 12-32ms before it do.
    /// </summary>
    public double ChannelStopLeadSeconds { get; set; } = DefaultChannelStopLeadSeconds;
    public double AvoidedTargetPenaltySeconds { get; set; } = DefaultAvoidedTargetPenaltySeconds;
    /// <summary>Absolute slack for integer rounding of small hits.</summary>
    public double DamageAbsoluteTolerance { get; set; } = 1.0;

    /// <summary>A log hit may land this long before the engine hit (server tick quantization).</summary>
    public double HitEarlySeconds { get; set; } = DefaultHitEarlySeconds;
    /// <summary>A log hit may land this long after the engine hit (projectile travel).</summary>
    public double HitLateSeconds { get; set; } = DefaultHitLateSeconds;
    /// <summary>
    /// Damage IDs that do not travel, with how late their log hits may land instead of <see cref="HitLateSeconds"/>: a
    /// longer window lets an engine hit with no log hit of its own take a later cast's. Combat logs (q3y7pWgXrjN2dftw fight
    /// 12): Chain Lightning lands at the engine's time, a free one's chain 0.2s later (0.50s after the proc, not 0.30s).
    /// </summary>
    public Dictionary<int, double> HitLateSecondsById { get; set; } = new()
    {
        [Core.Domain.LogIds.Effect(Core.Heroes.Tariq.Abilities.ChainLightning.DamageEffectId)] = ChainLightningHitLateSeconds,
    };
    /// <summary>How late a log hit with damage ID <paramref name="logId"/> may land after the engine hit.</summary>
    public double LateSecondsOf(int logId) => HitLateSecondsById.TryGetValue(logId, out var seconds) ? seconds : HitLateSeconds;
    /// <summary>Log hits with the same damage ID landing within this window belong to one multi-target hit.</summary>
    /// <remarks>
    /// Combat logs (Y4wpb2f3gc8LqJDX fight 25, Gunde): a pulse on 20-27 enemies is logged over up to 9ms; separate pulses
    /// land at least a server frame (33ms) apart.
    /// </remarks>
    public double HitGroupSeconds { get; set; } = DefaultHitGroupSeconds;

    /// <summary>
    /// Damage IDs whose multi-target hits land over a longer window than <see cref="HitGroupSeconds"/>, with that window.
    /// Combat logs (fXyzwGcQkTrLD34R fight 4, pWmcV42K39GHTLQX fight 23): a Multishot's arrows land over up to 0.1s, and
    /// casts come a global cooldown apart.
    /// </summary>
    public Dictionary<int, double> HitGroupSecondsById { get; set; } = new() { [Core.Heroes.Elarion.Abilities.Multishot.AbilityId] = MultishotHitGroupSeconds };

    /// <summary>The window of one multi-target hit with damage ID <paramref name="logId"/>.</summary>
    public double GroupSecondsOf(int logId) => HitGroupSecondsById.TryGetValue(logId, out var seconds) ? seconds : HitGroupSeconds;
    /// <summary>A log hit still unmatched this long after it landed is reported as missing from the engine.</summary>
    public double SettleSeconds { get; set; } = DefaultSettleSeconds;

    public double ProcEarlySeconds { get; set; } = DefaultProcEarlySeconds;
    /// <summary>An aura a proc applies may appear in the log this long after the roll.</summary>
    public double ProcAuraWindowSeconds { get; set; } = DefaultProcAuraWindowSeconds;
    /// <summary>Events in the same server tick can be logged this far apart.</summary>
    public double SameTickSeconds { get; set; } = DefaultSameTickSeconds;
    public double CastTimeToleranceSeconds { get; set; } = DefaultCastTimeToleranceSeconds;
    /// <summary>Aura state may differ from the log by event ordering within this window.</summary>
    public double AuraToleranceSeconds { get; set; } = DefaultAuraToleranceSeconds;
    public double ResourceSnapshotWindowSeconds { get; set; } = DefaultResourceSnapshotWindowSeconds;
    /// <summary>A periodic tick may fire this far from the engine's predicted tick time.</summary>
    public double TickTimeToleranceSeconds { get; set; } = PeriodicEffect.ServerFrameSeconds;
}

public enum IssueCategory
{
    Casts,
    Hits,
    Damage,
    Resources,
    Auras,
    Procs,
    GlobalCooldown,
    Cooldown,
    Ticks
}

/// <summary>The range the engine computes for a logged value. <see cref="Low"/> or <see cref="High"/> is infinite for a one-sided range.</summary>
/// <param name="Unit">The values' unit.</param>
/// <param name="ResourceName">The resource's name when <paramref name="Unit"/> is <see cref="RangeUnit.Resource"/>.</param>
public sealed record ExpectedRange(double Low, double High, RangeUnit Unit, string? ResourceName = null)
{
    public bool Contains(double value) => value >= Low && value <= High;

    /// <summary>The bound <paramref name="value"/> is outside, or the nearer bound for a value inside.</summary>
    public double NearerBound(double value) =>
        value < Low ? Low : value > High ? High : (value - Low <= High - value ? Low : High);

    public override string ToString() =>
        double.IsNegativeInfinity(Low) && double.IsPositiveInfinity(High) ? "any"
        : double.IsPositiveInfinity(High) ? $"{Format(Low)} or more"
        : double.IsNegativeInfinity(Low) ? $"{Format(High)} or less"
        : Low == High ? Format(Low)
        : $"{Format(Low)} to {Format(High)}";

    /// <summary>A value in <see cref="Unit"/>: seconds to the millisecond, everything else to two decimals.</summary>
    public string Format(double value) =>
        Unit == RangeUnit.Seconds
            ? value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "s"
            : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + (Unit == RangeUnit.Damage ? "" : " " + Unit.DisplayText(ResourceName));

    /// <summary>A value in the unit of <paramref name="range"/>; a value with no range has no unit.</summary>
    public static string Format(double value, ExpectedRange? range) =>
        range?.Format(value) ?? value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " ";
}

/// <param name="Subject">What the issue is about (ability or aura name), for grouping.</param>
public sealed record ReplayIssue(double Time, int Step, IssueCategory Category, string Message, string? Subject = null)
{
    /// <summary>The range the engine computes; null for a check with no numeric range.</summary>
    public ExpectedRange? Expected { get; init; }
    /// <summary>The log's value.</summary>
    public double? Observed { get; init; }
    /// <summary>The engine's state at the check.</summary>
    public EngineState? State { get; init; }

    /// <summary><see cref="Observed"/> minus the bound it is outside (0 inside the range); null with no <see cref="Expected"/>.</summary>
    public double? Deviation => Expected is { } expected && Observed is { } observed
        ? observed < expected.Low ? observed - expected.Low : observed > expected.High ? observed - expected.High : 0.0
        : null;

    /// <summary><see cref="Deviation"/> over the nearer bound; null when that bound is 0 or infinite.</summary>
    public double? RelativeDeviation
    {
        get
        {
            if (Deviation is not { } deviation || Observed is not { } observed) return null;
            var bound = Expected!.NearerBound(observed);
            return bound == 0 || double.IsInfinity(bound) ? null : deviation / Math.Abs(bound);
        }
    }

    public override string ToString()
    {
        var text = $"[{Category}] step {Step} @ {Time:F3}s: {Message}";
        if (Expected is { } expected)
        {
            text += $" (expected {expected}, observed {(Observed is { } observed ? expected.Format(observed) : "none")}";
            if (Deviation is { } deviation) text += $", deviation {(deviation > 0 ? "+" : "")}{expected.Format(deviation)}";
            if (RelativeDeviation is { } relativeDeviation) text += $" ({relativeDeviation:+0.0%;-0.0%;0.0%})";
            text += ")";
        }
        return text;
    }
}

public sealed class ReplayStep
{
    public int Index { get; init; }
    public double Time { get; init; }
    public ReplayCommandKind Kind { get; init; }
    public int AbilityId { get; init; }
    public string AbilityName { get; init; } = string.Empty;
    public string? Target { get; init; }
    /// <summary>False when the engine has no model of the ability; the step is skipped.</summary>
    public bool Modeled { get; set; } = true;
    public string? Note { get; set; }

    public List<ResourceCheck> Resources { get; } = new();
    public List<AuraCheck> Auras { get; } = new();
    public List<HitPair> Hits { get; } = new();
    public List<ReplayIssue> Issues { get; } = new();
    public List<string> UntrackedLogAuras { get; set; } = new();
    public List<string> ExternalAurasOnPlayer { get; set; } = new();
    public int ActiveEnemies { get; set; }
    /// <summary>The engine's haste before the step, and the auras contributing to it.</summary>
    public double EngineHaste { get; set; }
    public List<string> HasteSources { get; set; } = new();

    public bool Passed => Issues.Count == 0;
}

public sealed record ResourceCheck(string Resource, double Engine, double Log, double Tolerance)
{
    public bool Matches => Math.Abs(Engine - Log) <= Tolerance + FellowshipSim.Core.Engine.Tolerance.Comparison;
}

/// <param name="LogId">The aura's log ID (0 when unknown).</param>
public sealed record AuraCheck(string Aura, int EngineStacks, int LogStacks, int LogId = 0)
{
    public bool Matches => EngineStacks == LogStacks;
}

public sealed class ReplayResult
{
    /// <summary>
    /// The engine's damage: the sum of <see cref="HitPair.EngineAmount"/> in an exact replay, the damage dealt in free dice.
    /// </summary>
    public double EngineTotalDamage { get; set; }
    /// <summary>The damage the replay dealt (<see cref="SimulationContext.TotalDamage"/>): the log total in an exact replay.</summary>
    public double DamageDealt { get; set; }
    /// <summary>Log hits no engine hit matched, dealt by the replay (<see cref="SimulationContext.DealLoggedHit"/>).</summary>
    public int LoggedHitsDealt { get; set; }
    /// <summary>Engine hits on a target that dies in the log before they could land (no damage, no issue).</summary>
    public int MissedHits { get; set; }

    public string ReportCode { get; init; } = string.Empty;
    public int FightId { get; init; }
    public string FightName { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public string Hero { get; init; } = string.Empty;
    public int TotalSteps { get; init; }

    public List<ReplayStep> Steps { get; } = new();
    public ReplayStep? HaltedAt { get; set; }
    public List<ReplayIssue> HaltIssues { get; } = new();
    /// <summary>Every issue the verifier reported, in the order reported (those before the first step too).</summary>
    public List<ReplayIssue> Issues { get; } = new();
    public Dictionary<string, int> UnmodeledAbilities { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> UntrackedLogAuras { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> UnmodeledExternalAuras { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ProcRollStats> ProcStats { get; } = new();
    public List<HitPair> AllHits { get; } = new();
    /// <summary>Talents, augmentations, glyphs, gem nodes and relic passive of the character the engine does not model.</summary>
    public List<string> UnmodeledCustomizations { get; } = new();
    public int LogHits { get; set; }
    /// <summary>Casts the engine would have refused but made to follow the log (with the reason).</summary>
    public List<string> ForcedCasts { get; } = new();
    /// <summary>Fight length in seconds.</summary>
    public double DurationSeconds { get; set; }
    /// <summary>Every damage hit the player dealt according to the log (for the damage overview).</summary>
    public List<LogDamage> LogDamage { get; } = new();
    /// <summary>Completed casts per ability name: in the log, and in the engine.</summary>
    public Dictionary<string, int> LogCasts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> EngineCasts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int MatchedLogHits { get; set; }

    public int PassedSteps => Steps.Count(s => s.Passed);
    /// <summary>The whole fight replayed and every step passed.</summary>
    public bool Completed => HaltedAt == null && Steps.All(s => s.Passed);
}
