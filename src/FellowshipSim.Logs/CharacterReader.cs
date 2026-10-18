using System.Text.Json.Nodes;
using FellowshipSim.Core;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Import;

namespace FellowshipSim.Logs;

/// <summary>
/// Reads the build a fight was played with from the player's combatantinfo event: character-sheet stats, and talents,
/// traits, blessings, gem powers, set pieces, legendary items and weapon by game ID.
/// <para>
/// Character-sheet percentages are derived from the logged ratings: every hero has 5% base Critical Strike and 10% base
/// Spirit, ratings convert at 0.16% per point through the diminishing-returns curve, and the Amethyst/Emerald gem nodes
/// unlocked by the gem powers add their Critical Strike / Expertise bonuses (reproduces the logged sheet exactly).
/// </para>
/// </summary>
public static class CharacterReader
{
    public const double PercentPerRating = 0.0016;
    public const double BaseCrit = 0.05;
    public const double BaseSpirit = 0.10;
    public const int MaxTraitRank = 4;

    private static readonly double[] Breakpoints = [0.10, 0.15, 0.20, 0.25];
    private static readonly double[] Multipliers = [1.0, 0.98, 0.96 * 0.98, 0.94 * 0.96 * 0.98, 0.92 * 0.94 * 0.96 * 0.98];

    /// <summary>Gem colours in the order the build lists them.</summary>
    private static readonly GemColor[] GemOrder = [GemColor.Sapphire, GemColor.Amethyst, GemColor.Emerald, GemColor.Ruby, GemColor.Diamond, GemColor.Topaz];

    public static CharacterBuild FromCombatantInfo(JsonObject combatant, string playerName, string hero)
    {
        var gemPower = GemOrder.ToDictionary(c => c, c => Num(combatant[c.ToString().ToLowerInvariant()]));

        // ---- Character sheet: always-on gem nodes raise the sheet's percentages.
        double crit = BaseCrit + Diminishing(Num(combatant["crit"]) * PercentPerRating)
                      + GemBonus(Amethyst.AllNodes, "Killer Instinct", "CritChanceBonus", gemPower[GemColor.Amethyst]);
        double haste = Diminishing(Num(combatant["haste"]) * PercentPerRating);
        double expertise = Diminishing(Num(combatant["expertise"]) * PercentPerRating)
                           + GemBonus(Emerald.AllNodes, "Tactician's Accumen", "ExpertiseBonus", gemPower[GemColor.Emerald]);
        double spirit = BaseSpirit + Diminishing(Num(combatant["spirit"]) * PercentPerRating);
        static double Sheet(double value) => Math.Round(value * 10000) / 10000.0;

        var build = new CharacterBuild
        {
            Name = playerName,
            Hero = hero,
            // Only the hero's own primary stat is non-zero.
            PrimaryStat = new[] { "strength", "agility", "intellect" }.Max(k => Num(combatant[k])),
            Stamina = Num(combatant["stamina"]),
            Armor = Num(combatant["armor"]),
            CritRating = Num(combatant["crit"]),
            HasteRating = Num(combatant["haste"]),
            ExpertiseRating = Num(combatant["expertise"]),
            SpiritRating = Num(combatant["spirit"]),
            SheetPercents = new(StringComparer.OrdinalIgnoreCase)
            {
                ["crit"] = Sheet(crit),
                ["haste"] = Sheet(haste),
                ["expertise"] = Sheet(expertise),
                ["spirit"] = Sheet(spirit),
            },
        };

        // Talents: talent ID + 2,000,000.
        foreach (var t in combatant["talents"]?.AsArray() ?? [])
        {
            int id = (int)Num(t?["id"]) - LogIds.TalentOffset;
            if (id > 0) build.Talents.Add(id);
        }

        // Gear: traits (trait ID + 3,000,000), blessings, set pieces and legendary items. Blessings carry per-item IDs in
        // logs, so they are matched to the game's blessings by name.
        var traits = new List<(int Id, int Rank, string? Name)>();
        var blessings = new List<(int Id, int Rank, string? Name)>();
        foreach (var item in combatant["gear"]?.AsArray() ?? [])
        {
            if (item == null) continue;
            foreach (var t in item["traits"]?.AsArray() ?? [])
            {
                AddRank(traits, (int)Num(t?["id"]) - LogIds.TraitOffset, (int)Num(t?["rank"], 1), Str(t?["name"]));
            }
            foreach (var b in item["blessings"]?.AsArray() ?? [])
            {
                string name = Str(b?["name"]) ?? "";
                int i = blessings.FindIndex(x => x.Name == name);
                if (i < 0) blessings.Add((Catalog.BlessingIdByName(name), (int)Num(b?["level"], 1), name));
                else blessings[i] = (blessings[i].Id, blessings[i].Rank + (int)Num(b?["level"], 1), name);
            }
            if (item["set"] is JsonObject set && Num(set["id"]) > 0) build.SetBonusIds.Add((int)Num(set["id"]));
            if (Num(item["quality"]) >= 6) build.LegendaryItemIds.Add((int)Num(item["id"]));
        }

        // Weapon and neck trees: trait IDs + 3,000,000 with the points spent; the weapon ability is listed by its own ID.
        foreach (var t in (combatant["weaponTraits"]?.AsArray() ?? []).Concat(combatant["neckTraits"]?.AsArray() ?? []))
        {
            int id = (int)Num(t?["id"]);
            int points = (int)Num(t?["points"]);
            if (points <= 0) continue;
            if (id >= LogIds.TraitOffset) AddRank(traits, id - LogIds.TraitOffset, points, null, cap: true);
            else if (build.WeaponAbilityId == 0) build.WeaponAbilityId = id;
        }

        build.Traits.AddRange(traits.Select(t => new BuildEntry(t.Id, Math.Min(MaxTraitRank, t.Rank), t.Name)));
        build.Blessings.AddRange(blessings.Select(b => new BuildEntry(b.Id, b.Rank, b.Name)));
        foreach (var colour in GemOrder)
        {
            if (gemPower[colour] != 0) build.GemPower[colour] = gemPower[colour];
        }
        return build;
    }

    /// <summary>Adds ranks to an entry (or a new entry at the end); tree points cap the total at the maximum rank.</summary>
    private static void AddRank(List<(int Id, int Rank, string? Name)> list, int id, int rank, string? name, bool cap = false)
    {
        if (id <= 0) return;
        int i = list.FindIndex(e => e.Id == id);
        if (i < 0)
        {
            list.Add((id, cap ? Math.Min(MaxTraitRank, rank) : rank, name));
            return;
        }
        int total = list[i].Rank + rank;
        list[i] = (id, cap ? Math.Min(MaxTraitRank, total) : total, list[i].Name ?? name);
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

    /// <summary>The bonus of the highest unlocked node of a gem whose name starts with <paramref name="namePrefix"/>.</summary>
    private static double GemBonus(IEnumerable<GemNode> nodes, string namePrefix, string key, double power) =>
        nodes.Where(n => n.Name.StartsWith(namePrefix, StringComparison.Ordinal) && power >= n.RequiredPower && n.Values.ContainsKey(key))
            .OrderBy(n => n.RequiredPower)
            .LastOrDefault()?.Value(key) ?? 0.0;

    private static string? Str(JsonNode? n) => n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static double Num(JsonNode? n, double fallback = 0)
    {
        if (n is not JsonValue v) return fallback;
        if (v.TryGetValue<double>(out var d)) return d;
        if (v.TryGetValue<long>(out var l)) return l;
        return fallback;
    }
}
