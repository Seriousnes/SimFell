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

    private static readonly HashSet<string> AuraTypes =
    [
        "applybuff", "applybuffstack", "removebuff", "removebuffstack", "refreshbuff",
        "applydebuff", "applydebuffstack", "removedebuff", "removedebuffstack", "refreshdebuff"
    ];

    /// <summary>Smallest change in an enemy's health fraction between two health samples.</summary>
    private const double HealthSampleStep = 0.01;

    /// <summary>A cast event this soon before a begincast of the same ability is a press that did not go through (ms).</summary>
    private const double LoneCastBeforeBeginCastMs = 500;

    /// <summary>Longest a begincast can stay open before it is taken as abandoned (ms).</summary>
    private const double MaxCastWindowMs = 6000;

    /// <summary>Order of commands at the same moment.</summary>
    private static readonly Dictionary<string, int> KindOrder = new()
    {
        ["enemy_arise"] = 0, ["enemy_health"] = 1, ["external_aura"] = 1, ["cancel_cast"] = 2, ["stop_channel"] = 3,
        ["begin_cast"] = 4, ["complete_cast"] = 4, ["cast"] = 4, ["player_revive"] = 1, ["player_death"] = 8, ["enemy_demise"] = 9,
    };

    /// <summary>The players of a raw fight (actor ID, name, hero).</summary>
    public static IReadOnlyList<ReportPlayer> Players(JsonObject raw)
    {
        var friendly = raw["fight"]?["friendlyPlayers"]?.AsArray().Select(x => x!.GetValue<int>()).ToHashSet() ?? [];
        return (raw["master_data"]?["actors"]?.AsArray() ?? [])
            .Where(a => a?["type"]?.GetValue<string>() == "Player" && friendly.Contains(a["id"]!.GetValue<int>()))
            .Select(a => new ReportPlayer(a!["id"]!.GetValue<int>(), a["name"]?.GetValue<string>() ?? "?", a["subType"]?.GetValue<string>() ?? ""))
            .ToList();
    }

    public static JsonObject Build(JsonObject raw, int playerId)
    {
        var fight = raw["fight"]!.AsObject();
        double f0 = Num(fight["startTime"]);
        double fightEnd = Num(fight["endTime"]);
        var actors = new Dictionary<int, JsonNode>();
        foreach (var actor in raw["master_data"]!["actors"]!.AsArray()) actors[actor!["id"]!.GetValue<int>()] = actor;
        var abilityNames = new Dictionary<long, string>();
        foreach (var ability in raw["master_data"]!["abilities"]!.AsArray()) abilityNames[(long)Num(ability!["gameID"])] = ability["name"]?.GetValue<string>() ?? "";
        var events = Dedupe(raw["events"]!.AsArray());

        double Rel(double ts) => Math.Round((ts - f0) / 1000.0, 3);
        bool IsEnemy(int? id) => id is { } actorId && actorId != -1 && actors.TryGetValue(actorId, out var actor)
            && actor["type"]?.GetValue<string>() == "NPC" && actor["name"]?.GetValue<string>() != "Environment";
        string AbilityName(long id) => abilityNames.TryGetValue(id, out var knownName) ? knownName : $"ability_{id}";
        JsonObject Unit(int? id, int instance) => new() { ["id"] = id, ["instance"] = instance };

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
            foreach (var prefix in new[] { "source", "target" })
            {
                int? id = Int(logEvent[$"{prefix}ID"]);
                if (!IsEnemy(id)) continue;
                var key = new UnitKey(id!.Value, Instance(logEvent, prefix));
                double ts = Num(logEvent["timestamp"]);
                if (!firstSeen.ContainsKey(key)) { firstSeen[key] = ts; order.Add(key); }
                lastSeen[key] = ts;
                nameOf[key] = actors[key.Id]["name"]?.GetValue<string>() ?? "";
                if (logEvent[$"{prefix}Resources"]?["maxHitPoints"] is { } mhp && Num(mhp) > 0)
                {
                    maxHp[key] = Math.Max(maxHp.GetValueOrDefault(key), Num(mhp));
                }
            }
            if (Str(logEvent["type"]) == "death" && IsEnemy(Int(logEvent["targetID"])))
            {
                deathAt.TryAdd(new UnitKey(Int(logEvent["targetID"])!.Value, Instance(logEvent, "target")), Num(logEvent["timestamp"]));
            }
        }

        var enemies = new JsonArray();
        foreach (var key in order)
        {
            bool died = deathAt.ContainsKey(key);
            double endT = died ? deathAt[key] : lastSeen[key];
            enemies.Add(new JsonObject
            {
                ["id"] = key.Id, ["instance"] = key.Instance, ["name"] = nameOf[key], ["max_hp"] = maxHp.GetValueOrDefault(key),
                ["arise_t"] = Rel(firstSeen[key]), ["demise_t"] = Rel(endT), ["demise_reason"] = died ? "death" : "last_seen",
            });
            commands.Add(new JsonObject
            {
                ["t"] = Rel(firstSeen[key]), ["kind"] = "enemy_arise", ["enemy"] = Unit(key.Id, key.Instance),
                ["name"] = nameOf[key], ["max_hp"] = maxHp.GetValueOrDefault(key),
            });
            // An enemy still alive at the end of the fight is not demised.
            if (died || endT < fightEnd - 2000)
            {
                commands.Add(new JsonObject
                {
                    ["t"] = Rel(endT), ["kind"] = "enemy_demise", ["enemy"] = Unit(key.Id, key.Instance),
                    ["reason"] = died ? "death" : "last_seen",
                });
            }
        }

        // ---- Enemy health (from everyone's damage; health-dependent effects need it) ---------
        var lastFraction = new Dictionary<UnitKey, double>();
        foreach (var logEvent in events)
        {
            if (Str(logEvent["type"]) != "damage" || !IsEnemy(Int(logEvent["targetID"]))) continue;
            var res = logEvent["targetResources"];
            if (res?["hitPoints"] is not { } hpNode || res["maxHitPoints"] is not { } maxNode || Num(maxNode) == 0) continue;
            double hp = Num(hpNode), maxNow = Num(maxNode);
            // A killing blow logs the health below 0 as an unsigned 32-bit number (1p7AXmHfwV9CYMxR fight 30 @ 157.097:
            // 4294950962 of 726971).
            if (hp > maxNow) hp = 0;
            var key = new UnitKey(Int(logEvent["targetID"])!.Value, Instance(logEvent, "target"));
            double fraction = hp / maxNow;
            if (lastFraction.TryGetValue(key, out var last) && Math.Abs(fraction - last) < HealthSampleStep) continue;
            lastFraction[key] = fraction;
            commands.Add(new JsonObject
            {
                ["t"] = Rel(Num(logEvent["timestamp"])), ["kind"] = "enemy_health", ["enemy"] = Unit(key.Id, key.Instance),
                ["hp"] = hp, ["max_hp"] = maxNode.DeepClone(),
            });
        }

        // ---- Player deaths -------------------------------------------------------------------
        // A dead player's events log their health below 0 as an unsigned 32-bit number (above their maximum). Many returns
        // log no resurrect event (a release, or a wipe): the player is back at their first event with health in range.
        bool Alive(JsonNode logEvent, string prefix) => logEvent[$"{prefix}Resources"] is { } unitResources && unitResources["hitPoints"] is { } hpNode
            && unitResources["maxHitPoints"] is { } maxNode && Num(hpNode) > 0 && Num(hpNode) <= Num(maxNode);
        bool dead = false;
        foreach (var logEvent in events)
        {
            string type = Str(logEvent["type"]);
            bool onPlayer = Int(logEvent["targetID"]) == playerId, byPlayer = Int(logEvent["sourceID"]) == playerId;
            if (!dead)
            {
                if (type != "death" || !onPlayer) continue;
                dead = true;
                commands.Add(new JsonObject { ["t"] = Rel(Num(logEvent["timestamp"])), ["kind"] = "player_death" });
                continue;
            }
            if ((type == "resurrect" && onPlayer) || (byPlayer && Alive(logEvent, "source")) || (onPlayer && Alive(logEvent, "target")))
            {
                dead = false;
                commands.Add(new JsonObject { ["t"] = Rel(Num(logEvent["timestamp"])), ["kind"] = "player_revive" });
            }
        }

        // ---- Player actions ------------------------------------------------------------------
        var playerEvents = events.Where(e => Int(e["sourceID"]) == playerId
            && Str(e["type"]) is "begincast" or "cast" or "beginchannel" or "endchannel").ToList();

        // The log has no cancel event: a begincast that never completes was cancelled. The exact moment is unknown, so it
        // is placed at the player's first action after the cast began (actions used while casting without breaking it,
        // e.g. off-GCD spells, are only known to be such once the cast completes).
        JsonObject? openCast = null;
        double openTs = 0;
        var actionsSinceOpen = new List<double>();
        void CancelOpen(double fallbackT)
        {
            var cancel = (JsonObject)openCast!.DeepClone();
            cancel["t"] = actionsSinceOpen.Count > 0 ? actionsSinceOpen[0] : fallbackT;
            cancel["kind"] = "cancel_cast";
            cancel["inferred_time"] = true;
            commands.Add(cancel);
        }

        var ignoredCasts = new JsonArray();
        for (int i = 0; i < playerEvents.Count; i++)
        {
            var logEvent = playerEvents[i];
            long gid = (long)Num(logEvent["abilityGameID"]);
            JsonObject Base() => new()
            {
                ["t"] = Rel(Num(logEvent["timestamp"])), ["ability_id"] = gid, ["ability_name"] = AbilityName(gid),
                ["target"] = Unit(Int(logEvent["targetID"]) ?? -1, Instance(logEvent, "target")),
            };
            double eventTime = Rel(Num(logEvent["timestamp"]));
            switch (Str(logEvent["type"]))
            {
                case "begincast":
                    if (openCast != null) CancelOpen(eventTime);
                    openCast = Base();
                    openTs = Num(logEvent["timestamp"]);
                    actionsSinceOpen.Clear();
                    commands.Add(With(Base(), "kind", "begin_cast"));
                    break;

                case "cast":
                    if (logEvent["fake"]?.GetValue<bool>() == true) break; // duplicate activation marker emitted alongside begincast
                    if (openCast != null && (long)Num(openCast["ability_id"]) == gid)
                    {
                        commands.Add(With(Base(), "kind", "complete_cast"));
                        openCast = null;
                        break;
                    }
                    if (openCast != null && Num(logEvent["timestamp"]) - openTs > MaxCastWindowMs)
                    {
                        CancelOpen(eventTime);
                        openCast = null;
                    }
                    if (openCast != null) actionsSinceOpen.Add(eventTime);
                    // A lone cast event right before the same ability's begincast is not a real activation (the game logs
                    // it when a press does not go through; e.g. Chronoshift @ 124.882 then begincast @ 125.047).
                    bool lone = playerEvents.Skip(i + 1).Take(5).Any(n => Str(n["type"]) == "begincast"
                        && (long)Num(n["abilityGameID"]) == gid && Num(n["timestamp"]) - Num(logEvent["timestamp"]) <= LoneCastBeforeBeginCastMs);
                    if (lone)
                    {
                        ignoredCasts.Add(With(Base(), "reason", "cast event right before its own begincast"));
                        break;
                    }
                    // Instant cast. If a cast is open, the engine decides whether this ability may be used while casting.
                    commands.Add(With(Base(), "kind", "cast"));
                    break;

                case "beginchannel":
                    obsChannels.Add(With(Base(), "kind", "begin_channel"));
                    break;

                case "endchannel":
                    obsChannels.Add(With(Base(), "kind", "end_channel"));
                    commands.Add(With(Base(), "kind", "stop_channel"));
                    break;
            }
        }
        if (openCast != null) CancelOpen(Rel(fightEnd));

        // ---- Auras, damage, resources --------------------------------------------------------
        foreach (var logEvent in events)
        {
            string type = Str(logEvent["type"]);
            int? src = Int(logEvent["sourceID"]), tgt = Int(logEvent["targetID"]);
            if (AuraTypes.Contains(type))
            {
                long gid = (long)Num(logEvent["abilityGameID"]);
                var rec = new JsonObject
                {
                    ["t"] = Rel(Num(logEvent["timestamp"])), ["type"] = type, ["ability_id"] = gid, ["ability_name"] = AbilityName(gid),
                    ["source"] = Unit(src, Instance(logEvent, "source")), ["target"] = Unit(tgt, Instance(logEvent, "target")),
                    ["stack"] = logEvent["stack"]?.DeepClone(), ["duration_ms"] = logEvent["duration"]?.DeepClone(),
                    ["extra_ability_id"] = logEvent["extraAbilityGameID"]?.DeepClone(),
                };
                if (tgt == playerId && src == playerId) obsAuras.Add(rec);
                // Buffs from others on the player, and debuffs from others on enemies: engine inputs.
                else if (tgt == playerId || (IsEnemy(tgt) && src != playerId)) commands.Add(With(rec, "kind", "external_aura"));
                else if (IsEnemy(tgt) && src == playerId) obsAuras.Add(rec);
            }
            else if (type == "damage" && src == playerId && IsEnemy(tgt))
            {
                // Only damage to enemies: party mechanics logged as the player hurting themselves (Xavian's Stagger) are not ours.
                long gid = (long)Num(logEvent["abilityGameID"]);
                obsDamage.Add(new JsonObject
                {
                    ["t"] = Rel(Num(logEvent["timestamp"])), ["ability_id"] = gid, ["ability_name"] = AbilityName(gid),
                    ["target"] = Unit(tgt, Instance(logEvent, "target")),
                    ["amount"] = logEvent["amount"]?.DeepClone() ?? 0, ["unmitigated"] = logEvent["unmitigatedAmount"]?.DeepClone(),
                    ["absorbed"] = logEvent["absorbed"]?.DeepClone() ?? 0, ["blocked"] = logEvent["blocked"]?.DeepClone() ?? 0, ["overkill"] = logEvent["overkill"]?.DeepClone() ?? 0,
                    ["crit"] = Int(logEvent["hitType"]) is HitTypeCrit or HitTypeGrievousCrit, ["hit_type"] = logEvent["hitType"]?.DeepClone(), ["tick"] = logEvent["tick"]?.GetValue<bool>() ?? false,
                });
            }

            if (src == playerId && logEvent["sourceResources"]?["resources"] is JsonArray { Count: > 0 } resources)
            {
                var byType = new JsonObject();
                foreach (var resource in resources)
                {
                    byType[Int(resource!["type"])!.Value.ToString()] = new JsonObject { ["amount"] = resource["amount"]?.DeepClone(), ["max"] = resource["max"]?.DeepClone() };
                }
                obsResources.Add(new JsonObject
                {
                    ["t"] = Rel(Num(logEvent["timestamp"])), ["event"] = type, ["ability_id"] = logEvent["abilityGameID"]?.DeepClone(),
                    ["activation"] = logEvent["activation"]?.GetValue<bool>() == true, ["resources"] = byType,
                });
            }
        }

        var sortedCommands = commands
            .OrderBy(c => Num(c["t"]))
            .ThenBy(c => KindOrder.GetValueOrDefault(Str(c["kind"]), 5))
            .ToList();

        // Every player's augmentation ranks (buffs other players give scale with the giver's rank).
        var players = new JsonObject();
        foreach (var logEvent in events.Where(e => Str(e["type"]) == "combatantinfo"))
        {
            var gearTraits = new Dictionary<string, int>();
            foreach (var item in logEvent["gear"]?.AsArray() ?? [])
            {
                foreach (var tr in item?["traits"]?.AsArray() ?? [])
                {
                    string name = Str(tr!["name"]);
                    gearTraits[name] = gearTraits.GetValueOrDefault(name) + (Int(tr["rank"]) ?? 1);
                }
            }
            var weaponTraits = new JsonObject();
            foreach (var weaponTrait in logEvent["weaponTraits"]?.AsArray() ?? []) weaponTraits[Int(weaponTrait!["id"])!.Value.ToString()] = Int(weaponTrait["points"]) ?? 0;
            int sid = Int(logEvent["sourceID"])!.Value;
            players[sid.ToString()] = new JsonObject
            {
                ["name"] = actors.TryGetValue(sid, out var who) ? who["name"]?.DeepClone() : null,
                ["gear_traits"] = new JsonObject(gearTraits.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value))),
                // weapon-tree augmentation IDs are 3,000,000 + datamine augmentation ID
                ["weapon_traits"] = weaponTraits,
            };
        }

        actors.TryGetValue(playerId, out var player);
        string? playerName = player?["name"]?.GetValue<string>();
        string? hero = player?["subType"]?.GetValue<string>();
        var combatant = events.FirstOrDefault(e => Str(e["type"]) == "combatantinfo" && Int(e["sourceID"]) == playerId);

        return new JsonObject
        {
            ["schema"] = Schema,
            ["report_code"] = raw["report_code"]?.DeepClone(),
            ["fight_id"] = fight["id"]?.DeepClone(),
            ["fight_name"] = fight["name"]?.DeepClone(),
            ["fight_date"] = FightDate(raw),
            ["duration_s"] = Rel(fightEnd),
            ["dungeon_modifiers"] = events.FirstOrDefault(e => Str(e["type"]) == "dungeonstart")?["affixes"]?.DeepClone() ?? new JsonArray(),
            ["player"] = new JsonObject { ["id"] = playerId, ["name"] = playerName, ["hero"] = hero },
            ["combatant_info"] = combatant?.DeepClone(),
            ["players"] = players,
            // The character as it was in this fight (its combatantinfo event).
            ["character"] = combatant == null ? null
                : JsonSerializer.SerializeToNode(CharacterReader.FromCombatantInfo(combatant.AsObject(), playerName ?? "player", hero ?? "")),
            ["enemies"] = enemies,
            ["commands"] = new JsonArray(sortedCommands.Select(c => (JsonNode?)c).ToArray()),
            ["ignored_casts"] = ignoredCasts,
            ["observations"] = new JsonObject
            {
                ["damage"] = obsDamage,
                ["auras"] = obsAuras,
                ["resources"] = obsResources,
                ["channels"] = obsChannels,
            },
        };
    }

    /// <summary>When the fight was logged (UTC, ISO 8601), or null if the raw fight has no report start time.</summary>
    private static string? FightDate(JsonObject raw)
    {
        if (raw["report_start_time"] is not { } start) return null;
        double ms = Num(start) + Num(raw["fight"]?["startTime"]);
        return DateTimeOffset.FromUnixTimeMilliseconds((long)ms).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
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
        return list.OrderBy(e => Num(e["timestamp"])).ToList();
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

    private static int Instance(JsonNode logEvent, string prefix) => Int(logEvent[$"{prefix}Instance"]) ?? 1;
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
