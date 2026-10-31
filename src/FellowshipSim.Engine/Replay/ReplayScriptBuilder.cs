using FellowshipSim.Core.Engine;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// Turns a raw fight (every log event of one fight, see <see cref="FellowshipLogsReports.DownloadFightAsync"/>) into a
/// hero-agnostic replay script for one player:
/// <list type="bullet">
/// <item><c>commands</c>: what the engine is told to do, in time order (begin_cast, complete_cast, cancel_cast, cast,
/// stop_channel, enemy_arise, enemy_demise, enemy_health, player_death, player_revive, and external_aura: buffs and
/// debuffs other actors put on the player or on enemies, which the engine cannot produce itself).</item>
/// <item><c>observations</c>: what actually happened, used only to check the engine (the player's damage to enemies,
/// the player's own auras, resource snapshots, channel markers).</item>
/// </list>
/// Abilities are identified by their log IDs; nothing here is specific to a hero.
/// </summary>
public static class ReplayScriptBuilder
{
    /// <summary>
    /// Combat log hit types (FellowshipAnalyzer HitType): 0 a miss, 1 a normal hit, 2 a critical strike, 7 a dodge, 22 a
    /// grievous critical strike (crit chance beyond 100%). A miss and a dodge deal no damage.
    /// </summary>
    public const int HitTypeMiss = 0;
    public const int HitTypeDodge = 7;
    public const int HitTypeCrit = 2;
    public const int HitTypeGrievousCrit = 22;

    public const string Schema = FellowshipSim.Engine.Replay.ReplayScript.SchemaVersion;

    /// <summary>The log actor type of a player.</summary>
    private const string PlayerActorType = "Player";

    /// <summary>The log actor type of a non-player character.</summary>
    private const string NpcActorType = "NPC";

    /// <summary>The name of the log actor for the environment. It is not an enemy.</summary>
    private const string EnvironmentActorName = "Environment";

    /// <summary>Property names of the combat log and of the replay script.</summary>
    private const string TimestampProperty = "timestamp";
    internal const string NameProperty = "name";
    private const string TimeProperty = "t";
    internal const string IdProperty = "id";
    private const string TypeProperty = "type";
    private const string TargetIdProperty = "targetID";
    private const string KindProperty = "kind";
    private const string SourceIdProperty = FellowshipSim.Logs.FellowshipLogsReports.SourceIdProperty;
    private const string AbilityGameIdProperty = "abilityGameID";
    private const string AbilityIdProperty = "ability_id";
    private const string AmountProperty = "amount";
    private const string TargetProperty = "target";
    private const string ResourcesProperty = "resources";
    private const string MaxHitPointsProperty = "maxHitPoints";
    private const string MaxHpProperty = "max_hp";
    private const string MasterDataProperty = FellowshipSim.Logs.FellowshipLogsReports.MasterDataProperty;
    internal const string FightProperty = FellowshipSim.Logs.FellowshipLogsReports.FightProperty;
    private const string EnemyProperty = "enemy";
    private const string AbilityNameProperty = "ability_name";
    private const string TickProperty = "tick";
    private const string SubTypeProperty = FellowshipSim.Logs.FellowshipLogsReports.SubTypeProperty;
    private const string StartTimeProperty = FellowshipSim.Logs.FellowshipLogsReports.StartTimeProperty;
    private const string StackProperty = "stack";
    private const string ReportCodeProperty = FellowshipSim.Logs.FellowshipLogsReports.ReportCodeProperty;
    private const string OverkillProperty = "overkill";
    private const string MaxProperty = "max";
    private const string InstanceProperty = "instance";
    private const string HitTypeProperty = "hitType";
    private const string HitPointsProperty = "hitPoints";
    private const string BlockedProperty = "blocked";
    private const string ActorsProperty = FellowshipSim.Logs.FellowshipLogsReports.ActorsProperty;
    private const string ActivationProperty = "activation";
    private const string AbsorbedProperty = "absorbed";
    internal const string WeaponTraitsProperty = "weaponTraits";
    private const string ScriptWeaponTraitsProperty = "weapon_traits";
    private const string UnmitigatedAmountProperty = "unmitigatedAmount";
    private const string UnmitigatedProperty = "unmitigated";
    internal const string TraitsProperty = "traits";
    private const string TargetResourcesProperty = "targetResources";
    private const string SourceResourcesProperty = "sourceResources";
    private const string SourceProperty = "source";
    private const string SchemaProperty = "schema";
    private const string ReportStartTimeProperty = FellowshipSim.Logs.FellowshipLogsReports.ReportStartTimeProperty;
    private const string ReasonProperty = "reason";
    internal const string RankProperty = "rank";
    internal const string PointsProperty = "points";
    private const string PlayersProperty = "players";
    private const string PlayerProperty = "player";
    private const string ObservationsProperty = "observations";
    private const string InferredTimeProperty = "inferred_time";
    private const string IgnoredCastsProperty = "ignored_casts";
    private const string HpProperty = "hp";
    private const string ScriptHitTypeProperty = "hit_type";
    private const string HeroProperty = "hero";
    private const string GearTraitsProperty = "gear_traits";
    internal const string GearProperty = "gear";
    private const string GameIdProperty = "gameID";
    private const string FriendlyPlayersProperty = FellowshipSim.Logs.FellowshipLogsReports.FriendlyPlayersProperty;
    private const string FakeProperty = "fake";
    private const string ExtraAbilityGameIdProperty = "extraAbilityGameID";
    private const string ExtraAbilityIdProperty = "extra_ability_id";
    private const string EventsProperty = FellowshipSim.Logs.FellowshipLogsReports.EventsProperty;
    private const string EventProperty = "event";
    private const string EnemiesProperty = "enemies";
    private const string EndTimeProperty = FellowshipSim.Logs.FellowshipLogsReports.EndTimeProperty;
    private const string DurationSProperty = "duration_s";
    private const string DurationMsProperty = "duration_ms";
    private const string DurationProperty = "duration";
    private const string DungeonModifiersProperty = "dungeon_modifiers";
    private const string DemiseTProperty = "demise_t";
    private const string DemiseReasonProperty = "demise_reason";
    private const string DamageProperty = "damage";
    private const string CritProperty = "crit";
    private const string CommandsProperty = "commands";
    private const string CombatantInfoProperty = "combatant_info";
    private const string CharacterProperty = FellowshipSim.Logs.FellowshipLogsReports.CharacterProperty;
    private const string ChannelsProperty = "channels";
    private const string AurasProperty = "auras";
    private const string AriseTProperty = "arise_t";
    private const string AffixesProperty = "affixes";
    private const string AbilitiesProperty = "abilities";
    private const string FightIdProperty = "fight_id";
    private const string FightNameProperty = "fight_name";
    private const string FightDateProperty = "fight_date";

    /// <summary>Decimal places of a replay script time in seconds.</summary>
    private const int ScriptTimeDecimals = 3;

    /// <summary>An enemy last seen this long before the fight's end (ms) is demised at that moment; a later one is still alive.</summary>
    private const double EnemyAliveMarginMs = 2000;

    /// <summary>Events after a cast event searched for a begincast of the same ability.</summary>
    private const int LoneCastLookaheadEvents = 5;

    /// <summary>Order of a command kind that <see cref="KindOrder"/> does not list.</summary>
    private const int UnlistedKindOrder = 5;

    /// <summary>Smallest change in an enemy's health fraction between two health samples.</summary>
    private const double HealthSampleStep = 0.01;

    /// <summary>A cast event this soon before a begincast of the same ability is a press that did not go through (ms).</summary>
    private const double LoneCastBeforeBeginCastMs = 500;

    /// <summary>Longest a begincast can stay open before it is taken as abandoned (ms).</summary>
    private const double MaxCastWindowMs = 6000;

    /// <summary>One combat log event with its <c>type</c> parsed once.</summary>
    private sealed record LogEvent(JsonObject Json, LogEventType Type)
    {
        public JsonNode? this[string key] => Json[key];
    }

    private static bool IsAura(LogEventType type) => type is LogEventType.ApplyBuff or LogEventType.ApplyBuffStack or LogEventType.RemoveBuff
        or LogEventType.RemoveBuffStack or LogEventType.RefreshBuff or LogEventType.ApplyDebuff or LogEventType.ApplyDebuffStack
        or LogEventType.RemoveDebuff or LogEventType.RemoveDebuffStack or LogEventType.RefreshDebuff;

    /// <summary>Order of commands at the same moment.</summary>
    private static readonly Dictionary<string, int> KindOrder = new()
    {
        [ReplayCommandKindText.EnemyArise] = 0, [ReplayCommandKindText.EnemyHealth] = 1, [ReplayCommandKindText.ExternalAura] = 1,
        [ReplayCommandKindText.CancelCast] = 2, [ReplayCommandKindText.StopChannel] = 3, [ReplayCommandKindText.BeginCast] = 4,
        [ReplayCommandKindText.CompleteCast] = 4, [ReplayCommandKindText.Cast] = 4, [ReplayCommandKindText.PlayerRevive] = 1,
        [ReplayCommandKindText.PlayerDeath] = 8, [ReplayCommandKindText.EnemyDemise] = 9,
    };

    /// <summary>The players of a raw fight (actor ID, name, hero).</summary>
    public static IReadOnlyList<ReportPlayer> Players(JsonObject raw)
    {
        var friendly = raw[FightProperty]?[FriendlyPlayersProperty]?.AsArray().Select(x => x!.GetValue<int>()).ToHashSet() ?? [];
        return (raw[MasterDataProperty]?[ActorsProperty]?.AsArray() ?? [])
            .Where(a => a?[TypeProperty]?.GetValue<string>() == PlayerActorType && friendly.Contains(a[IdProperty]!.GetValue<int>()))
            .Select(a => new ReportPlayer(a![IdProperty]!.GetValue<int>(), a[NameProperty]?.GetValue<string>() ?? "?", a[SubTypeProperty]?.GetValue<string>() ?? ""))
            .ToList();
    }

    public static JsonObject Build(JsonObject raw, int playerId)
    {
        var fight = raw[FightProperty]!.AsObject();
        var fightStart = Num(fight[StartTimeProperty]);
        var fightEnd = Num(fight[EndTimeProperty]);
        var actors = new Dictionary<int, JsonNode>();
        foreach (var actor in raw[MasterDataProperty]![ActorsProperty]!.AsArray()) actors[actor![IdProperty]!.GetValue<int>()] = actor;
        var abilityNames = new Dictionary<long, string>();
        foreach (var ability in raw[MasterDataProperty]![AbilitiesProperty]!.AsArray()) abilityNames[(long)Num(ability![GameIdProperty])] = ability[NameProperty]?.GetValue<string>() ?? "";
        var events = Dedupe(raw[EventsProperty]!.AsArray()).Select(e => new LogEvent(e, LogEventTypes.Parse(Str(e[TypeProperty])))).ToList();

        double Rel(double timestamp) => Math.Round((timestamp - fightStart) / SimTime.MillisecondsPerSecond, ScriptTimeDecimals);
        bool IsEnemy(int? id) => id is { } actorId && actorId != -1 && actors.TryGetValue(actorId, out var actor)
            && actor[TypeProperty]?.GetValue<string>() == NpcActorType && actor[NameProperty]?.GetValue<string>() != EnvironmentActorName;
        string AbilityName(long id) => abilityNames.TryGetValue(id, out var knownName) ? knownName : $"ability_{id}";
        JsonObject Unit(int? id, int instance) => new() { [IdProperty] = id, [InstanceProperty] = instance };

        var commands = new List<JsonObject>();
        var obsDamage = new JsonArray();
        var obsAuras = new JsonArray();
        var obsResources = new JsonArray();
        var obsChannels = new JsonArray();

        // ---- Enemy lifetimes ---------------------------------------------------------------
        var order = new List<UnitKey>();
        var firstSeen = new Dictionary<UnitKey, double>();
        var lastSeen = new Dictionary<UnitKey, double>();
        var deathAt = new Dictionary<UnitKey, double>();
        var maxHp = new Dictionary<UnitKey, double>();
        var nameOf = new Dictionary<UnitKey, string>();
        foreach (var logEvent in events)
        {
            foreach (var prefix in new[] { SourceProperty, TargetProperty })
            {
                var id = Int(logEvent[$"{prefix}ID"]);
                if (!IsEnemy(id)) continue;
                var key = new UnitKey(id!.Value, Instance(logEvent, prefix));
                var timestamp = Num(logEvent[TimestampProperty]);
                if (!firstSeen.ContainsKey(key)) { firstSeen[key] = timestamp; order.Add(key); }
                lastSeen[key] = timestamp;
                nameOf[key] = actors[key.Id][NameProperty]?.GetValue<string>() ?? "";
                if (logEvent[$"{prefix}Resources"]?[MaxHitPointsProperty] is { } mhp && Num(mhp) > 0)
                {
                    maxHp[key] = Math.Max(maxHp.GetValueOrDefault(key), Num(mhp));
                }
            }
            if (logEvent.Type == LogEventType.Death && IsEnemy(Int(logEvent[TargetIdProperty])))
            {
                deathAt.TryAdd(new UnitKey(Int(logEvent[TargetIdProperty])!.Value, Instance(logEvent, TargetProperty)), Num(logEvent[TimestampProperty]));
            }
        }

        var enemies = new JsonArray();
        foreach (var key in order)
        {
            var died = deathAt.ContainsKey(key);
            var removalReason = (died ? EnemyRemovalReason.Death : EnemyRemovalReason.LastSeen).LogText();
            var endT = died ? deathAt[key] : lastSeen[key];
            enemies.Add(new JsonObject
            {
                [IdProperty] = key.Id, [InstanceProperty] = key.Instance, [NameProperty] = nameOf[key], [MaxHpProperty] = maxHp.GetValueOrDefault(key),
                [AriseTProperty] = Rel(firstSeen[key]), [DemiseTProperty] = Rel(endT), [DemiseReasonProperty] = removalReason,
            });
            commands.Add(new JsonObject
            {
                [TimeProperty] = Rel(firstSeen[key]), [KindProperty] = ReplayCommandKind.EnemyArise.LogText(), [EnemyProperty] = Unit(key.Id, key.Instance),
                [NameProperty] = nameOf[key], [MaxHpProperty] = maxHp.GetValueOrDefault(key),
            });
            // An enemy still alive at the end of the fight is not demised.
            if (died || endT < fightEnd - EnemyAliveMarginMs)
            {
                commands.Add(new JsonObject
                {
                    [TimeProperty] = Rel(endT), [KindProperty] = ReplayCommandKind.EnemyDemise.LogText(), [EnemyProperty] = Unit(key.Id, key.Instance),
                    [ReasonProperty] = removalReason,
                });
            }
        }

        // ---- Enemy health (from everyone's damage; health-dependent effects need it) ---------
        var lastFraction = new Dictionary<UnitKey, double>();
        foreach (var logEvent in events)
        {
            if (logEvent.Type != LogEventType.Damage || !IsEnemy(Int(logEvent[TargetIdProperty]))) continue;
            var res = logEvent[TargetResourcesProperty];
            if (res?[HitPointsProperty] is not { } hpNode || res[MaxHitPointsProperty] is not { } maxNode || Num(maxNode) == 0) continue;
            var health = Num(hpNode);
            var maxNow = Num(maxNode);
            // A killing blow logs the health below 0 as an unsigned 32-bit number (1p7AXmHfwV9CYMxR fight 30 @ 157.097:
            // 4294950962 of 726971).
            if (health > maxNow) health = 0;
            var key = new UnitKey(Int(logEvent[TargetIdProperty])!.Value, Instance(logEvent, TargetProperty));
            var fraction = health / maxNow;
            if (lastFraction.TryGetValue(key, out var last) && Math.Abs(fraction - last) < HealthSampleStep) continue;
            lastFraction[key] = fraction;
            commands.Add(new JsonObject
            {
                [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [KindProperty] = ReplayCommandKind.EnemyHealth.LogText(), [EnemyProperty] = Unit(key.Id, key.Instance),
                [HpProperty] = health, [MaxHpProperty] = maxNode.DeepClone(),
            });
        }

        // ---- Player deaths -------------------------------------------------------------------
        // A dead player's events log their health below 0 as an unsigned 32-bit number (above their maximum). Many returns
        // log no resurrect event (a release, or a wipe): the player is back at their first event with health in range.
        bool Alive(LogEvent logEvent, string prefix) => logEvent[$"{prefix}Resources"] is { } unitResources && unitResources[HitPointsProperty] is { } hpNode
            && unitResources[MaxHitPointsProperty] is { } maxNode && Num(hpNode) > 0 && Num(hpNode) <= Num(maxNode);
        var dead = false;
        foreach (var logEvent in events)
        {
            var type = logEvent.Type;
            var onPlayer = Int(logEvent[TargetIdProperty]) == playerId;
            var byPlayer = Int(logEvent[SourceIdProperty]) == playerId;
            if (!dead)
            {
                if (type != LogEventType.Death || !onPlayer) continue;
                dead = true;
                commands.Add(new JsonObject { [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [KindProperty] = ReplayCommandKind.PlayerDeath.LogText() });
                continue;
            }
            if ((type == LogEventType.Resurrect && onPlayer) || (byPlayer && Alive(logEvent, SourceProperty)) || (onPlayer && Alive(logEvent, TargetProperty)))
            {
                dead = false;
                commands.Add(new JsonObject { [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [KindProperty] = ReplayCommandKind.PlayerRevive.LogText() });
            }
        }

        // ---- Player actions ------------------------------------------------------------------
        var playerEvents = events.Where(e => Int(e[SourceIdProperty]) == playerId
            && e.Type is LogEventType.BeginCast or LogEventType.Cast or LogEventType.BeginChannel or LogEventType.EndChannel).ToList();

        // The log has no cancel event: a begincast that never completes was cancelled. The exact moment is unknown, so it
        // is placed at the player's first action after the cast began (actions used while casting without breaking it,
        // e.g. off-GCD spells, are only known to be such once the cast completes).
        JsonObject? openCast = null;
        var openTs = 0.0;
        var actionsSinceOpen = new List<double>();
        void CancelOpen(double fallbackT)
        {
            var cancel = (JsonObject)openCast!.DeepClone();
            cancel[TimeProperty] = actionsSinceOpen.Count > 0 ? actionsSinceOpen[0] : fallbackT;
            cancel[KindProperty] = ReplayCommandKind.CancelCast.LogText();
            cancel[InferredTimeProperty] = true;
            commands.Add(cancel);
        }

        var ignoredCasts = new JsonArray();
        for (var i = 0; i < playerEvents.Count; i++)
        {
            var logEvent = playerEvents[i];
            var gid = (long)Num(logEvent[AbilityGameIdProperty]);
            JsonObject Base() => new()
            {
                [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [AbilityIdProperty] = gid, [AbilityNameProperty] = AbilityName(gid),
                [TargetProperty] = Unit(Int(logEvent[TargetIdProperty]) ?? -1, Instance(logEvent, TargetProperty)),
            };
            var eventTime = Rel(Num(logEvent[TimestampProperty]));
            switch (logEvent.Type)
            {
                case LogEventType.BeginCast:
                    if (openCast != null) CancelOpen(eventTime);
                    openCast = Base();
                    openTs = Num(logEvent[TimestampProperty]);
                    actionsSinceOpen.Clear();
                    commands.Add(With(Base(), KindProperty, ReplayCommandKind.BeginCast.LogText()));
                    break;

                case LogEventType.Cast:
                    if (logEvent[FakeProperty]?.GetValue<bool>() == true) break; // duplicate activation marker emitted alongside begincast
                    if (openCast != null && (long)Num(openCast[AbilityIdProperty]) == gid)
                    {
                        commands.Add(With(Base(), KindProperty, ReplayCommandKind.CompleteCast.LogText()));
                        openCast = null;
                        break;
                    }
                    if (openCast != null && Num(logEvent[TimestampProperty]) - openTs > MaxCastWindowMs)
                    {
                        CancelOpen(eventTime);
                        openCast = null;
                    }
                    if (openCast != null) actionsSinceOpen.Add(eventTime);
                    // A lone cast event right before the same ability's begincast is not a real activation (the game logs
                    // it when a press does not go through; e.g. Chronoshift @ 124.882 then begincast @ 125.047).
                    var lone = playerEvents.Skip(i + 1).Take(LoneCastLookaheadEvents).Any(n => n.Type == LogEventType.BeginCast
                        && (long)Num(n[AbilityGameIdProperty]) == gid && Num(n[TimestampProperty]) - Num(logEvent[TimestampProperty]) <= LoneCastBeforeBeginCastMs);
                    if (lone)
                    {
                        ignoredCasts.Add(With(Base(), ReasonProperty, "cast event right before its own begincast"));
                        break;
                    }
                    // Instant cast. If a cast is open, the engine decides whether this ability may be used while casting.
                    commands.Add(With(Base(), KindProperty, ReplayCommandKind.Cast.LogText()));
                    break;

                case LogEventType.BeginChannel:
                    obsChannels.Add(With(Base(), KindProperty, ReplayCommandKind.BeginChannel.LogText()));
                    break;

                case LogEventType.EndChannel:
                    obsChannels.Add(With(Base(), KindProperty, ReplayCommandKind.EndChannel.LogText()));
                    commands.Add(With(Base(), KindProperty, ReplayCommandKind.StopChannel.LogText()));
                    break;
            }
        }
        if (openCast != null) CancelOpen(Rel(fightEnd));

        // ---- Auras, damage, resources --------------------------------------------------------
        foreach (var logEvent in events)
        {
            var type = logEvent.Type;
            var src = Int(logEvent[SourceIdProperty]);
            var tgt = Int(logEvent[TargetIdProperty]);
            if (IsAura(type))
            {
                var gid = (long)Num(logEvent[AbilityGameIdProperty]);
                var rec = new JsonObject
                {
                    [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [TypeProperty] = Str(logEvent[TypeProperty]), [AbilityIdProperty] = gid, [AbilityNameProperty] = AbilityName(gid),
                    [SourceProperty] = Unit(src, Instance(logEvent, SourceProperty)), [TargetProperty] = Unit(tgt, Instance(logEvent, TargetProperty)),
                    [StackProperty] = logEvent[StackProperty]?.DeepClone(), [DurationMsProperty] = logEvent[DurationProperty]?.DeepClone(),
                    [ExtraAbilityIdProperty] = logEvent[ExtraAbilityGameIdProperty]?.DeepClone(),
                };
                if (tgt == playerId && src == playerId) obsAuras.Add(rec);
                // Buffs from others on the player, and debuffs from others on enemies: engine inputs.
                else if (tgt == playerId || (IsEnemy(tgt) && src != playerId)) commands.Add(With(rec, KindProperty, ReplayCommandKind.ExternalAura.LogText()));
                else if (IsEnemy(tgt) && src == playerId) obsAuras.Add(rec);
            }
            else if (type == LogEventType.Damage && src == playerId && IsEnemy(tgt))
            {
                // Only damage to enemies: party mechanics logged as the player hurting themselves (Xavian's Stagger) are not ours.
                var gid = (long)Num(logEvent[AbilityGameIdProperty]);
                obsDamage.Add(new JsonObject
                {
                    [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [AbilityIdProperty] = gid, [AbilityNameProperty] = AbilityName(gid),
                    [TargetProperty] = Unit(tgt, Instance(logEvent, TargetProperty)),
                    [AmountProperty] = logEvent[AmountProperty]?.DeepClone() ?? 0, [UnmitigatedProperty] = logEvent[UnmitigatedAmountProperty]?.DeepClone(),
                    [AbsorbedProperty] = logEvent[AbsorbedProperty]?.DeepClone() ?? 0, [BlockedProperty] = logEvent[BlockedProperty]?.DeepClone() ?? 0, [OverkillProperty] = logEvent[OverkillProperty]?.DeepClone() ?? 0,
                    [CritProperty] = Int(logEvent[HitTypeProperty]) is HitTypeCrit or HitTypeGrievousCrit, [ScriptHitTypeProperty] = logEvent[HitTypeProperty]?.DeepClone(), [TickProperty] = logEvent[TickProperty]?.GetValue<bool>() ?? false,
                });
            }

            if (src == playerId && logEvent[SourceResourcesProperty]?[ResourcesProperty] is JsonArray { Count: > 0 } resources)
            {
                var byType = new JsonObject();
                foreach (var resource in resources)
                {
                    byType[Int(resource![TypeProperty])!.Value.ToString()] = new JsonObject { [AmountProperty] = resource[AmountProperty]?.DeepClone(), [MaxProperty] = resource[MaxProperty]?.DeepClone() };
                }
                obsResources.Add(new JsonObject
                {
                    [TimeProperty] = Rel(Num(logEvent[TimestampProperty])), [EventProperty] = Str(logEvent[TypeProperty]), [AbilityIdProperty] = logEvent[AbilityGameIdProperty]?.DeepClone(),
                    [ActivationProperty] = logEvent[ActivationProperty]?.GetValue<bool>() == true, [ResourcesProperty] = byType,
                });
            }
        }

        var sortedCommands = commands
            .OrderBy(c => Num(c[TimeProperty]))
            .ThenBy(c => KindOrder.GetValueOrDefault(Str(c[KindProperty]), UnlistedKindOrder))
            .ToList();

        // Every player's augmentation ranks (buffs other players give scale with the giver's rank).
        var players = new JsonObject();
        foreach (var logEvent in events.Where(e => e.Type == LogEventType.CombatantInfo))
        {
            var gearTraits = new Dictionary<string, int>();
            foreach (var item in logEvent[GearProperty]?.AsArray() ?? [])
            {
                foreach (var trait in item?[TraitsProperty]?.AsArray() ?? [])
                {
                    var name = Str(trait![NameProperty]);
                    gearTraits[name] = gearTraits.GetValueOrDefault(name) + (Int(trait[RankProperty]) ?? 1);
                }
            }
            var weaponTraits = new JsonObject();
            foreach (var weaponTrait in logEvent[WeaponTraitsProperty]?.AsArray() ?? []) weaponTraits[Int(weaponTrait![IdProperty])!.Value.ToString()] = Int(weaponTrait[PointsProperty]) ?? 0;
            var sid = Int(logEvent[SourceIdProperty])!.Value;
            players[sid.ToString()] = new JsonObject
            {
                [NameProperty] = actors.TryGetValue(sid, out var who) ? who[NameProperty]?.DeepClone() : null,
                [GearTraitsProperty] = new JsonObject(gearTraits.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value))),
                // weapon-tree augmentation IDs are 3,000,000 + datamine augmentation ID
                [ScriptWeaponTraitsProperty] = weaponTraits,
            };
        }

        actors.TryGetValue(playerId, out var player);
        var playerName = player?[NameProperty]?.GetValue<string>();
        var hero = player?[SubTypeProperty]?.GetValue<string>();
        var combatant = events.FirstOrDefault(e => e.Type == LogEventType.CombatantInfo && Int(e[SourceIdProperty]) == playerId);

        return new JsonObject
        {
            [SchemaProperty] = Schema,
            [ReportCodeProperty] = raw[ReportCodeProperty]?.DeepClone(),
            [FightIdProperty] = fight[IdProperty]?.DeepClone(),
            [FightNameProperty] = fight[NameProperty]?.DeepClone(),
            [FightDateProperty] = FightDate(raw),
            [DurationSProperty] = Rel(fightEnd),
            [DungeonModifiersProperty] = events.FirstOrDefault(e => e.Type == LogEventType.DungeonStart)?[AffixesProperty]?.DeepClone() ?? new JsonArray(),
            [PlayerProperty] = new JsonObject { [IdProperty] = playerId, [NameProperty] = playerName, [HeroProperty] = hero },
            [CombatantInfoProperty] = combatant?.Json.DeepClone(),
            [PlayersProperty] = players,
            // The character as it was in this fight (its combatantinfo event).
            [CharacterProperty] = combatant == null ? null
                : JsonSerializer.SerializeToNode(CharacterReader.FromCombatantInfo(combatant.Json, playerName ?? "player", hero ?? "")),
            [EnemiesProperty] = enemies,
            [CommandsProperty] = new JsonArray(sortedCommands.Select(c => (JsonNode?)c).ToArray()),
            [IgnoredCastsProperty] = ignoredCasts,
            [ObservationsProperty] = new JsonObject
            {
                [DamageProperty] = obsDamage,
                [AurasProperty] = obsAuras,
                [ResourcesProperty] = obsResources,
                [ChannelsProperty] = obsChannels,
            },
        };
    }

    /// <summary>When the fight was logged (UTC, ISO 8601), or null if the raw fight has no report start time.</summary>
    private static string? FightDate(JsonObject raw)
    {
        if (raw[ReportStartTimeProperty] is not { } start) return null;
        var milliseconds = Num(start) + Num(raw[FightProperty]?[StartTimeProperty]);
        return DateTimeOffset.FromUnixTimeMilliseconds((long)milliseconds).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>Page boundaries in the API overlap: drops exact duplicate events, then orders by time.</summary>
    private static List<JsonObject> Dedupe(JsonArray events)
    {
        var seen = new HashSet<string>();
        var list = new List<JsonObject>();
        foreach (var node in events)
        {
            if (node is not JsonObject logEvent) continue;
            if (seen.Add(Canonical(logEvent))) list.Add(logEvent);
        }
        return list.OrderBy(e => Num(e[TimestampProperty])).ToList();
    }

    private static string Canonical(JsonNode? node) => node switch
    {
        JsonObject jsonObject => "{" + string.Join(",", jsonObject.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"\"{kv.Key}\":{Canonical(kv.Value)}")) + "}",
        JsonArray jsonArray => "[" + string.Join(",", jsonArray.Select(Canonical)) + "]",
        null => "null",
        _ => node.ToJsonString(),
    };

    private static JsonObject With(JsonObject jsonObject, string key, JsonNode? value)
    {
        jsonObject[key] = value;
        return jsonObject;
    }

    private static int Instance(LogEvent logEvent, string prefix) => Int(logEvent[$"{prefix}Instance"]) ?? 1;
    private static string Str(JsonNode? node) => node?.GetValue<string>() ?? "";
    private static int? Int(JsonNode? node) => node == null ? null : (int)Num(node);
    private static double Num(JsonNode? node)
    {
        if (node is not JsonValue jsonValue) return 0;
        if (jsonValue.TryGetValue<double>(out var number)) return number;
        if (jsonValue.TryGetValue<long>(out var longValue)) return longValue;
        if (jsonValue.TryGetValue<int>(out var intValue)) return intValue;
        return 0;
    }
}
