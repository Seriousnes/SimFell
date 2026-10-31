using FellowshipSim.Core.Customization.StatEnchants;
using System.Text.Json.Nodes;
using FellowshipSim.Core;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Import;
using static FellowshipSim.Engine.Replay.ReplayScriptBuilder;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// Reads the build a fight was played with from the player's combatantinfo event: the gear's own stats, and talents,
/// augmentations, glyphs, gem sources, relic passive, legendary items and weapon by game ID.
/// <para>
/// The stats are the gear's raw ones, each item's listed attributes summed, before the hero's base stats, stat
/// enchants, gem nodes, sigils and relics, which the engine adds as it imports the build. The combatantinfo totals
/// (crit, strength, ...) include them all, so they are no input here.
/// </para>
/// </summary>
public static class CharacterReader
{
    public const int MaxAugmentationRank = 4;

    /// <summary>Item quality from which an item is legendary.</summary>
    public const int LegendaryItemQuality = 6; // UNVERIFIED

    private const string AttributesProperty = "attributes";
    private const string ValueProperty = "value";
    private const string HealthProperty = "health";
    private const string ArmorProperty = "armor";
    private const string TalentsProperty = "talents";
    private const string BlessingsProperty = "blessings";
    private const string LevelProperty = "level";
    private const string GemProperty = "gem";
    private const string GemPowersProperty = "gemPowers";
    private const string QualityProperty = "quality";
    private const string NeckTraitsProperty = "neckTraits";

    /// <summary>The names logs give the gear's stat attributes.</summary>
    private const string CritAttribute = "Critical Strike", HasteAttribute = "Haste", ExpertiseAttribute = "Expertise",
        SpiritAttribute = "Spirit", StaminaAttribute = "Stamina";
    private static readonly string[] PrimaryAttributes = ["Strength", "Agility", "Intellect"];

    public static CharacterBuild FromCombatantInfo(JsonObject combatant, string playerName, string hero)
    {
        // The gear's own stats: every attribute each item lists.
        var gear = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in combatant[GearProperty]?.AsArray() ?? [])
        {
            foreach (var attribute in item?[AttributesProperty]?.AsArray() ?? [])
            {
                if (Str(attribute?[NameProperty]) is { } name) gear[name] = gear.GetValueOrDefault(name) + Num(attribute?[ValueProperty]);
            }
        }

        var build = new CharacterBuild
        {
            Name = playerName,
            Hero = hero,
            // Only the hero's own primary stat is on their gear.
            PrimaryStat = PrimaryAttributes.Max(gear.GetValueOrDefault),
            Stamina = gear.GetValueOrDefault(StaminaAttribute),
            MaxHealth = Num(combatant[HealthProperty]),
            Armor = Num(combatant[ArmorProperty]),
            CritRating = gear.GetValueOrDefault(CritAttribute),
            HasteRating = gear.GetValueOrDefault(HasteAttribute),
            ExpertiseRating = gear.GetValueOrDefault(ExpertiseAttribute),
            SpiritRating = gear.GetValueOrDefault(SpiritAttribute),
        };

        // Talents: talent ID + 2,000,000.
        foreach (var talent in combatant[TalentsProperty]?.AsArray() ?? [])
        {
            int id = (int)Num(talent?[IdProperty]) - LogIds.TalentOffset;
            if (id > 0) build.Talents.Add(id);
        }

        // Gear: augmentations (augmentation ID + 3,000,000), glyphs, the relics and legendary items. Glyphs carry per-item
        // IDs in logs, so they are matched to the game's glyphs by name.
        var augmentations = new List<RankedEntry>();
        var glyphs = new List<RankedEntry>();
        var statEnchants = new List<RankedEntry>();
        // How many slots each stat enchant fills: on an item one per 6 rating, rounded up (6 is one, 10 and 12 are two;
        // combat logs: with it every non-legendary item holds exactly 3 slots, and 40 of 46 players of patch 0.5.0.0 fill
        // the 44), and one per weapon tree point.
        var statEnchantCounts = new Dictionary<int, int>();
        foreach (var item in combatant[GearProperty]?.AsArray() ?? [])
        {
            if (item == null) continue;
            int itemId = (int)Num(item[IdProperty]);
            if (Catalog.RelicItemOf(itemId) != null)
            {
                // Relic slots fill in gear order.
                if (build.Relic1ItemId == 0) build.Relic1ItemId = itemId;
                else build.Relic2ItemId = itemId;
            }
            foreach (var trait in item[TraitsProperty]?.AsArray() ?? [])
            {
                int id = (int)Num(trait?[IdProperty]) - LogIds.AugmentationOffset;
                int rank = (int)Num(trait?[RankProperty], 1);
                // A stat enchant's rank is its rating, added to the item's other enchants of the same ID.
                if (StatEnchant.Find(id) != null)
                {
                    AddRank(statEnchants, id, rank, Str(trait?[NameProperty]));
                    statEnchantCounts[id] = statEnchantCounts.GetValueOrDefault(id) + (int)Math.Ceiling(rank / StatEnchantSlotRating);
                }
                else AddRank(augmentations, id, rank, Str(trait?[NameProperty]));
            }
            foreach (var blessing in item[BlessingsProperty]?.AsArray() ?? [])
            {
                string name = Str(blessing?[NameProperty]) ?? "";
                int index = glyphs.FindIndex(x => x.Name == name);
                if (index < 0) glyphs.Add(new RankedEntry(Catalog.GlyphIdByName(name), (int)Num(blessing?[LevelProperty], 1), name));
                else glyphs[index] = new RankedEntry(glyphs[index].Id, glyphs[index].Rank + (int)Num(blessing?[LevelProperty], 1), name);
            }
            // The gem in the item's socket, and the item's gem power bonuses (each names its colour).
            if (GemItem.Find((int)Num(item[GemProperty]?[IdProperty])) is { } gem) build.SocketedGemIds.Add(gem.Id);
            foreach (var bonus in item[GemPowersProperty]?.AsArray() ?? [])
            {
                if (Enum.TryParse(Str(bonus?[NameProperty]), out GemColor color)) build.GearGemBonuses[color] = build.GearGemBonuses.GetValueOrDefault(color) + 1;
            }
            if (Num(item[QualityProperty]) >= LegendaryItemQuality) build.LegendaryItemIds.Add(itemId);
        }

        // Weapon and neck trees: augmentation IDs + 3,000,000 with the points spent; the weapon ability is listed by its own ID.
        foreach (var trait in (combatant[WeaponTraitsProperty]?.AsArray() ?? []).Concat(combatant[NeckTraitsProperty]?.AsArray() ?? []))
        {
            int id = (int)Num(trait?[IdProperty]);
            int points = (int)Num(trait?[PointsProperty]);
            if (points <= 0) continue;
            // The weapon tree has a node per stat enchant (65 to 70); each point is one more slot of that enchant, 6 rating
            // to each of its stats (combat logs: the combatantinfo ratings are the gear's, the enchants' and the gem nodes'
            // with 6 per point; at 1 per point every build with a point fell 5 short).
            if (id >= LogIds.AugmentationOffset && StatEnchant.Find(id - LogIds.AugmentationOffset) != null)
            {
                AddRank(statEnchants, id - LogIds.AugmentationOffset, points * (int)StatEnchantSlotRating, null);
                statEnchantCounts[id - LogIds.AugmentationOffset] = statEnchantCounts.GetValueOrDefault(id - LogIds.AugmentationOffset) + points;
            }
            else if (id >= LogIds.AugmentationOffset) AddRank(augmentations, id - LogIds.AugmentationOffset, points, null, cap: true);
            else if (build.WeaponAbilityId == 0) build.WeaponAbilityId = id;
        }

        build.Augmentations.AddRange(augmentations.Select(t => new BuildEntry(t.Id, Math.Min(MaxAugmentationRank, t.Rank), t.Name)));
        build.Glyphs.AddRange(glyphs.Select(b => new BuildEntry(b.Id, b.Rank, b.Name)));
        build.StatEnchants.AddRange(statEnchants.Select(e => new BuildEntry(e.Id, e.Rank, e.Name)));
        foreach (var (id, count) in statEnchantCounts) build.StatEnchantCounts[id] = count;
        // The gem power of the sockets and the gear's bonuses, before the relics' multiplier (Covetous Greed).
        foreach (var (color, power) in GemItem.PowerByColor(build.SocketedGemIds, build.GearGemBonuses, 1.0)) build.GemPower[color] = power;
        return build;
    }

    /// <summary>Adds ranks to an entry (or a new entry at the end); tree points cap the total at the maximum rank.</summary>
    /// <summary>The rating of one stat enchant slot on an item (combat logs: 6 at the current item level).</summary>
    private const double StatEnchantSlotRating = 6.0;

    private static void AddRank(List<RankedEntry> list, int id, int rank, string? name, bool cap = false)
    {
        if (id <= 0) return;
        int index = list.FindIndex(e => e.Id == id);
        if (index < 0)
        {
            list.Add(new RankedEntry(id, cap ? Math.Min(MaxAugmentationRank, rank) : rank, name));
            return;
        }
        int total = list[index].Rank + rank;
        list[index] = new RankedEntry(id, cap ? Math.Min(MaxAugmentationRank, total) : total, list[index].Name ?? name);
    }

    private static string? Str(JsonNode? node) => node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text) ? text : null;

    private static double Num(JsonNode? node, double fallback = 0)
    {
        if (node is not JsonValue jsonValue) return fallback;
        if (jsonValue.TryGetValue<double>(out var number)) return number;
        if (jsonValue.TryGetValue<long>(out var integer)) return integer;
        return fallback;
    }

    private readonly record struct RankedEntry(int Id, int Rank, string? Name);
}
