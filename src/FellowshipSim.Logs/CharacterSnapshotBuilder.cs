using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;

namespace FellowshipSim.Logs;

/// <summary>
/// Builds the character a fight was played with (the CharacterSnapshotDto shape the importer reads) from the player's
/// combatantinfo event: stats, gems, talents, blessings, traits, sets, legendary and weapon.
/// <para>
/// Character-sheet percentages are derived from the logged ratings: every hero has 5% base Critical Strike and 10% base
/// Spirit, ratings convert at 0.16% per point through the diminishing-returns curve, and the Amethyst/Emerald gem nodes
/// unlocked by the gem powers add their Critical Strike / Expertise bonuses (reproduces the logged sheet exactly).
/// </para>
/// </summary>
public static class CharacterSnapshotBuilder
{
    private const double PercentPerRating = 0.0016;
    private const double BaseCrit = 0.05;
    private const double BaseSpirit = 0.10;
    private const int SetPiecesForBonus = 2;
    private const int MaxTraitRank = 4;

    private static readonly double[] Breakpoints = [0.10, 0.15, 0.20, 0.25];
    private static readonly double[] Multipliers = [1.0, 0.98, 0.96 * 0.98, 0.94 * 0.96 * 0.98, 0.92 * 0.94 * 0.96 * 0.98];

    private static readonly string[] GemColours = ["Sapphire", "Amethyst", "Emerald", "Ruby", "Diamond", "Topaz"];

    /// <summary>Gem nodes that always raise a sheet percentage (not conditional ones like Sealed Fate or First Strike).</summary>
    private static readonly (string Colour, string NodePrefix, string PlaceholderKey, string Stat)[] SheetGemBonuses =
    [
        ("Amethyst", "Killer Instinct", "CritChanceBonus", "crit"),
        ("Emerald", "Tactician's Accumen", "ExpertiseBonus", "expertise"),
    ];

    /// <summary>Legendary items by hero and gear slot (quality 6 items).</summary>
    private static readonly Dictionary<string, Dictionary<int, string>> HeroSlotLegendary = new()
    {
        ["rime"] = new() { [3] = "frostwyrms_spite", [9] = "undulating_spirit", [10] = "undulating_spirit", [5] = "skandis_decree" },
    };

    private static readonly Dictionary<string, string> TraitKeys = new()
    {
        ["amethyst splinters"] = "amethyst_splinters", ["brave machinations"] = "brave_machinations", ["diamond strike"] = "diamond_strike",
        ["divine mediation"] = "divine_mediation", ["emerald judgement"] = "emerald_judgement", ["first man standing"] = "first_man_standing",
        ["grounded spirit"] = "grounded_spirit", ["heart of stone"] = "heart_of_stone", ["heroic brand"] = "heroic_brand",
        ["hidden power"] = "hidden_power", ["hunter's focus"] = "hunters_focus", ["hunters focus"] = "hunters_focus",
        ["focused haste"] = "hunters_focus", ["inspired allegiance"] = "inspired_allegiance", ["iron spikes"] = "iron_spikes",
        ["kindling"] = "kindling", ["king of the hill"] = "king_of_the_hill", ["latent resurgence"] = "latent_resurgence",
        ["martial initiative"] = "martial_initiative", ["navigator's intuition"] = "navigators_intuition", ["patient soul"] = "patient_soul",
        ["ruby storm"] = "ruby_storm", ["sapphire aurastone"] = "sapphire_aurastone", ["seized opportunity"] = "seized_opportunity",
        ["stalwart readiness"] = "stalwart_readiness", ["treasure hunter's delight"] = "treasure_hunters_delight",
        ["vengeful soul"] = "vengeful_soul", ["visions of grandeur"] = "visions_of_grandeur", ["willful momentum"] = "willful_momentum",
    };

    /// <summary>Set names (matched in order, ignoring punctuation) to set keys.</summary>
    private static readonly (string Name, string Key)[] SetKeys =
    [
        ("dark prophecy", "dark_prophecy"), ("drakheim's absolution", "drakheims_absolution"), ("drakheims absolution", "drakheims_absolution"),
        ("seal of the heskyr", "seal_of_the_heskyr"), ("death's grasp", "deaths_grasp"), ("deaths grasp", "deaths_grasp"),
        ("draconic might", "draconic_might"), ("eldrin's deceit", "eldrin_deceit"), ("eldrins deceit", "eldrin_deceit"),
        ("eldrin's fury", "eldrin_fury"), ("eldrins fury", "eldrin_fury"), ("haunting lament", "haunting_lament"),
        ("sin warding", "sin_warding"), ("sinthara's veil", "sintharas_veil"), ("sintharas veil", "sintharas_veil"),
        ("torment of bael'aurum", "torment_of_baelaurum"), ("torment of baelaurum", "torment_of_baelaurum"),
        ("bael'aurum", "torment_of_baelaurum"), ("baelaurum", "torment_of_baelaurum"), ("tuzari grace", "tuzari_grace"),
        ("tuzari's grace", "tuzari_grace"), ("undulating spirit", "undulating_spirit"),
    ];

    /// <summary>Weapon names (matched in order as part of the item name; the last match wins) to weapon keys.</summary>
    private static readonly (string Name, string Key)[] WeaponKeys =
    [
        ("Chronoshift", "chronoshift"), ("Chrono", "chronoshift"), ("Icicles of Anzhyr", "icicles_of_anzhyr"), ("Icicles", "icicles_of_anzhyr"),
        ("Anzhyr", "icicles_of_anzhyr"), ("Voidbringer", "voidbringers_touch"), ("Voidbringer's Touch", "voidbringers_touch"),
        ("Voidbringers Touch", "voidbringers_touch"), ("Void-touched", "voidbringers_touch"), ("Voidtouched", "voidbringers_touch"),
        ("Void-Touched", "voidbringers_touch"), ("Nature's Fury", "natures_fury"), ("Natures Fury", "natures_fury"),
        ("Skybolt", "twilight_skybolt"), ("Twilight Skybolt", "twilight_skybolt"), ("Fated Strike", "fated_strike"),
        ("Sahril's Wrath", "sahrils_wrath"), ("Sahrils Wrath", "sahrils_wrath"), ("Sahril's Aegis", "sahrils_aegis"),
        ("Sahrils Aegis", "sahrils_aegis"), ("Sahril", "sahrils_wrath"), ("Earthbreaker", "earthbreaker"),
        ("Alzerac's Shackle", "alzeracs_shackle"), ("Alzeracs Shackle", "alzeracs_shackle"),
        ("Repository of Frozen Light", "repository_of_frozen_light"), ("Frozen Light", "repository_of_frozen_light"),
        ("Zeraleth's Hunger", "zeraleths_hunger"), ("Zeraleths Hunger", "zeraleths_hunger"),
    ];

    /// <summary>The character snapshot for <paramref name="combatant"/> (a combatantinfo event).</summary>
    public static JsonObject FromCombatantInfo(JsonObject combatant, string playerName, string hero, GameData data)
    {
        var gemPower = GemColours.ToDictionary(c => c, c => Num(combatant[c.ToLowerInvariant()]));

        // ---- Character sheet ----
        var sheet = new Dictionary<string, double>
        {
            ["crit"] = BaseCrit + Diminishing(Num(combatant["crit"]) * PercentPerRating),
            ["haste"] = Diminishing(Num(combatant["haste"]) * PercentPerRating),
            ["expertise"] = Diminishing(Num(combatant["expertise"]) * PercentPerRating),
            ["spirit"] = BaseSpirit + Diminishing(Num(combatant["spirit"]) * PercentPerRating),
        };
        foreach (var (colour, node, key, stat) in SheetGemBonuses) sheet[stat] += GemBonus(data, colour, node, key, gemPower[colour]);
        double Pct(string stat) => Math.Round(sheet[stat] * 10000);

        // ---- Gear: traits, blessings, sets, weapon, legendary ----
        var traits = new Dictionary<string, int>();
        var blessings = new Dictionary<string, int>();
        var setCounts = new Dictionary<string, int>();
        string? weapon = null, legendary = null;
        var gear = combatant["gear"]?.AsArray() ?? [];
        for (int slot = 0; slot < gear.Count; slot++)
        {
            var item = gear[slot];
            if (item == null) continue;
            foreach (var t in item["traits"]?.AsArray() ?? [])
            {
                string name = Str(t?["name"]).ToLowerInvariant();
                string key = TraitKeys.GetValueOrDefault(name) ?? SanitizeName(name);
                traits[key] = traits.GetValueOrDefault(key) + (int)(Num(t?["rank"], 1));
            }
            foreach (var b in item["blessings"]?.AsArray() ?? [])
            {
                string key = SanitizeName(Str(b?["name"]));
                blessings[key] = blessings.GetValueOrDefault(key) + (int)Num(b?["level"], 1);
            }
            if (item["set"] is { } set)
            {
                string setName = set is JsonObject ? Str(set["name"]) : set.ToString();
                string clean = AlphaNumeric(setName.ToLowerInvariant());
                foreach (var (name, key) in SetKeys)
                {
                    if (!clean.Contains(AlphaNumeric(name))) continue;
                    setCounts[key] = setCounts.GetValueOrDefault(key) + 1;
                    break;
                }
            }
            string itemName = Str(item["name"]).ToLowerInvariant();
            foreach (var (name, key) in WeaponKeys)
            {
                if (itemName.Contains(name.ToLowerInvariant())) weapon = key;
            }
            if (Num(item["quality"]) >= 6 && legendary == null)
            {
                legendary = HeroSlotLegendary.GetValueOrDefault(hero.ToLowerInvariant())?.GetValueOrDefault(slot);
            }
        }

        // Weapon and neck tree traits are datamine trait IDs + 3,000,000, with the points spent in them.
        foreach (var t in (combatant["weaponTraits"]?.AsArray() ?? []).Concat(combatant["neckTraits"]?.AsArray() ?? []))
        {
            int id = (int)Num(t?["id"]);
            int points = (int)Num(t?["points"]);
            if (id < 3_000_000 || points <= 0) continue;
            string? name = NameOf(data, "trait", id - 3_000_000)?.ToLowerInvariant();
            if (name == null || !TraitKeys.TryGetValue(name, out var key)) continue;
            traits[key] = Math.Min(MaxTraitRank, traits.GetValueOrDefault(key) + points);
        }

        // Talents are datamine talent IDs + 2,000,000.
        var talents = new JsonArray();
        foreach (var t in combatant["talents"]?.AsArray() ?? [])
        {
            string? name = NameOf(data, "talent", (int)Num(t?["id"]) - 2_000_000);
            if (!string.IsNullOrEmpty(name)) talents.Add(SanitizeName(name));
        }

        var gems = new JsonObject();
        foreach (var colour in GemColours)
        {
            if (gemPower[colour] != 0) gems[$"{colour.ToLowerInvariant()}_power"] = gemPower[colour];
        }

        return new JsonObject
        {
            ["Name"] = playerName,
            ["HeroClass"] = hero,
            ["Intellect"] = Num(combatant["intellect"]),
            ["Stamina"] = Num(combatant["stamina"]),
            ["Armor"] = Num(combatant["armor"]),
            ["HasteRating"] = Num(combatant["haste"]),
            ["ExpertiseRating"] = Num(combatant["expertise"]),
            ["CritRating"] = Num(combatant["crit"]),
            ["Spirit"] = Num(combatant["spirit"]),
            ["BaseCritPercent"] = Pct("crit") / 10000.0,
            ["BaseHastePercent"] = Pct("haste") / 10000.0,
            ["VersatilityPercent"] = 0.0,
            ["Gems"] = gems,
            ["Blessings"] = ToJson(blessings),
            ["WeaponTraits"] = ToJson(traits.ToDictionary(kv => kv.Key, kv => Math.Min(MaxTraitRank, kv.Value))),
            ["Talents"] = talents,
            ["Sets"] = new JsonArray(setCounts.Where(kv => kv.Value >= SetPiecesForBonus).Select(kv => kv.Key)
                .OrderBy(k => k, StringComparer.Ordinal).Select(k => (JsonNode?)k).ToArray()),
            ["Legendary"] = legendary ?? "",
            ["Weapon"] = weapon ?? "",
            // Character-sheet values, kept for verifying the engine's stat derivation.
            ["SheetPercents"] = new JsonObject
            {
                ["crit"] = Pct("crit") / 10000.0,
                ["haste"] = Pct("haste") / 10000.0,
                ["expertise"] = Pct("expertise") / 10000.0,
                ["spirit"] = Pct("spirit") / 10000.0,
            },
        };
    }

    /// <summary>A rating percentage after diminishing returns (the same curve as the engine's StatDiminishingReturns).</summary>
    public static double Diminishing(double unmitigated)
    {
        if (unmitigated <= Breakpoints[0]) return unmitigated;
        double result = Breakpoints[0], remaining = unmitigated - result, current = result;
        for (int i = 1; i < Breakpoints.Length; i++)
        {
            double previous = current;
            current += (Breakpoints[i] - Breakpoints[i - 1]) / Multipliers[i];
            double segment = current - previous;
            if (remaining <= segment) return result + remaining * Multipliers[i];
            result += segment * Multipliers[i];
            remaining -= segment;
        }
        return result + remaining * Multipliers[^1];
    }

    /// <summary>The bonus of the highest unlocked node of a gem colour carrying placeholder <paramref name="key"/>.</summary>
    private static double GemBonus(GameData data, string colour, string nodePrefix, string key, double power)
    {
        (double Required, double Value)? best = null;
        foreach (var node in data.All("gem-trait"))
        {
            if (Str(node, "gem") != colour || !Str(node, "name").StartsWith(nodePrefix, StringComparison.Ordinal)) continue;
            double required = node.TryGetProperty("requiredPower", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetDouble() : 0;
            if (power < required || !node.TryGetProperty("placeholders", out var placeholders)) continue;
            foreach (var p in placeholders.EnumerateArray())
            {
                if (Str(p, "key") != key) continue;
                double value = p.GetProperty("value").GetDouble();
                if (Str(p, "format") == "delta") value -= 1.0;
                if (best == null || required > best.Value.Required) best = (required, value);
            }
        }
        return best?.Value ?? 0.0;
    }

    private static string? NameOf(GameData data, string type, int id) =>
        data.TryRaw(type, id, out var raw) && raw.TryGetProperty("name", out var n) ? n.GetString() : null;

    private static string SanitizeName(string name) =>
        Regex.Replace(name.ToLowerInvariant().Replace(' ', '_').Replace("'", ""), "[^a-zA-Z0-9_]", "");

    private static string AlphaNumeric(string s) => Regex.Replace(s, "[^a-z0-9]", "");

    private static JsonObject ToJson(Dictionary<string, int> values) =>
        new(values.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value)));

    private static string Str(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static string Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : "";

    private static double Num(JsonNode? n, double fallback = 0)
    {
        if (n is not JsonValue v) return fallback;
        if (v.TryGetValue<double>(out var d)) return d;
        if (v.TryGetValue<long>(out var l)) return l;
        return fallback;
    }
}
