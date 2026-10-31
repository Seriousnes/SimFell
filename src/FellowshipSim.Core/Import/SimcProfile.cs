using System.Globalization;
using System.Text;
using FellowshipSim.Core.APL;
using FellowshipSim.Core.Customization.Aspects;
using FellowshipSim.Core.Customization.Augmentations;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Customization.StatEnchants;
using FellowshipSim.Core.Customization.ProtectiveEnchants;
using FellowshipSim.Core.Customization.Sigils;

namespace FellowshipSim.Core.Import;

/// <summary>
/// A character profile in simc style: one <c>key=value</c> per line, names as action-list names ("winters_blessing").
/// Stats are the gear's raw ones: Main Stat, Stamina and ratings as the items list them, without the hero's base stats,
/// stat enchants (<c>stat_enchants</c> lists those), gem nodes, sigils or relics, which the engine adds; <c>gems</c> is
/// the sockets' and gear bonuses' gem power, without Covetous Greed. A profile written before gives the ratings as the
/// character sheet's percentages (<c>crit=30%</c>), and its Main Stat and gem power with their bonuses: it reads as it
/// is, each percentage as the rating that gives it, and is marked (<c>stats_from_old_profile=true</c>) as likely wrong
/// until its stats are edited.
/// <code>
/// rime="sww"
/// primary_stat=161
/// stamina=306
/// crit_rating=89
/// haste_rating=71
/// talents=avalanche/coalescing_frost/...
/// aspects=martial_initiative:2/...
/// sigils=hunters_focus:1/...
/// protective_enchant=the_oak:1
/// glyphs=the_monarch:1/...
/// gems=sapphire:1500/...
/// gem_sockets=splendid_amethyst/large_emerald/...
/// gear_gem_bonuses=sapphire:6/amethyst:5/...
/// relic1=orb_of_inner_seas,id=5926
/// relic2=orb_of_gathering_storms,id=5924
/// legendary=frostwyrms_spite,id=5899
/// weapon=ashas_chronoshift_spire
/// actions+=/frost_bolt
/// </code>
/// Lines starting with '#' are comments. <c>actions</c> lines are the profile's action list.
/// </summary>
public sealed record SimcProfile(CharacterBuild Build, string ActionList)
{
    private const char CommentMarker = '#';
    private const string NameOption = "";
    private const string NumberFormat = "0.##";
    private const string ActionsKey = "actions";
    private const string TrueText = "true";
    private const string IdOption = "id";
    private const string PrimaryStatKey = "primary_stat";
    private const string StaminaKey = "stamina";
    private const string ArmorKey = "armor";
    private const string CritRatingKey = "crit_rating";
    private const string HasteRatingKey = "haste_rating";
    private const string ExpertiseRatingKey = "expertise_rating";
    private const string SpiritRatingKey = "spirit_rating";
    private const string StatsFromOldProfileKey = "stats_from_old_profile";
    private const string CritKey = "crit";
    private const string HasteKey = "haste";
    private const string ExpertiseKey = "expertise";
    private const string SpiritKey = "spirit";
    private const string TalentsKey = "talents";
    private const string AspectsKey = "aspects";
    private const string SigilsKey = "sigils";
    private const string ProtectiveEnchantKey = "protective_enchant";
    private const string GlyphsKey = "glyphs";
    private const string GemsKey = "gems";
    private const string GemSocketsKey = "gem_sockets";
    private const string GearGemBonusesKey = "gear_gem_bonuses";
    private const string StatEnchantsKey = "stat_enchants";
    private const string StatEnchantCountsKey = "stat_enchant_counts";
    private const string Relic1Key = "relic1";
    private const string Relic2Key = "relic2";
    private const string LegendaryKey = "legendary";
    private const string WeaponKey = "weapon";
    public static SimcProfile Load(string path) => Parse(File.ReadAllText(path));

    public static SimcProfile Parse(string text)
    {
        var build = new CharacterBuild();
        var actions = new StringBuilder();
        var percents = new HashSet<string>();
        Hero? hero = null;
        var lineNumber = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(CommentMarker)) continue;
            if (line.StartsWith(ActionsKey, StringComparison.Ordinal))
            {
                actions.AppendLine(line);
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0) throw Error(lineNumber, line, "expected key=value");
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            try
            {
                if (hero == null)
                {
                    var heroName = Catalog.Heroes.Keys.FirstOrDefault(h => AplNames.Slug(h) == key)
                                   ?? throw new FormatException("the first line names the hero: <hero>=\"<name>\"");
                    build.Hero = heroName;
                    build.Name = value.Trim('"');
                    hero = Catalog.CreateHero(heroName);
                    continue;
                }
                Read(build, hero, key, value, percents);
            }
            catch (FormatException exception)
            {
                throw Error(lineNumber, line, exception.Message);
            }
        }
        if (hero == null) throw new FormatException("The profile has no hero line.");
        // The ratings read from percentages count the stat enchants; a build's ratings are without them.
        if (percents.Contains(CritKey)) build.CritRating = Math.Max(0, build.CritRating - build.EnchantRating(StatType.CritRating));
        if (percents.Contains(HasteKey)) build.HasteRating = Math.Max(0, build.HasteRating - build.EnchantRating(StatType.HasteRating));
        if (percents.Contains(ExpertiseKey)) build.ExpertiseRating = Math.Max(0, build.ExpertiseRating - build.EnchantRating(StatType.ExpertiseRating));
        if (percents.Contains(SpiritKey)) build.SpiritRating = Math.Max(0, build.SpiritRating - build.EnchantRating(StatType.SpiritRating));
        if (percents.Count > 0) build.StatsFromOldProfile = true;
        return new SimcProfile(build, actions.ToString());
    }

    private static void Read(CharacterBuild build, Hero hero, string key, string value, HashSet<string> percents)
    {
        switch (key)
        {
            case PrimaryStatKey: build.PrimaryStat = Number(value); break;
            case StaminaKey: build.Stamina = Number(value); break;
            case ArmorKey: build.Armor = Number(value); break;
            case CritRatingKey: build.CritRating = Number(value); break;
            case HasteRatingKey: build.HasteRating = Number(value); break;
            case ExpertiseRatingKey: build.ExpertiseRating = Number(value); break;
            case SpiritRatingKey: build.SpiritRating = Number(value); break;
            case StatsFromOldProfileKey: build.StatsFromOldProfile = value == TrueText; break;
            case CritKey:
                percents.Add(CritKey);
                build.CritRating = StatDiminishingReturns.ToRating(Percent(value) - StatBlock.BaseCritChance);
                break;
            case HasteKey:
                percents.Add(HasteKey);
                build.HasteRating = StatDiminishingReturns.ToRating(Percent(value));
                break;
            case ExpertiseKey:
                percents.Add(ExpertiseKey);
                build.ExpertiseRating = StatDiminishingReturns.ToRating(Percent(value));
                break;
            case SpiritKey:
                percents.Add(SpiritKey);
                build.SpiritRating = StatDiminishingReturns.ToRating(Percent(value) - StatBlock.BaseSpirit);
                break;
            case TalentsKey:
                var tree = hero.TalentTreeSlots.Select(s => Catalog.CreateTalent(s.TalentId)).OfType<Talent>().Select(t => new NamedId(t.Name, t.Id));
                foreach (var name in List(value)) build.Talents.Add(IdOf(name, tree, "talent"));
                break;
            case AspectsKey: AddAugmentations<Aspect>(build, value, "aspect"); break;
            case SigilsKey: AddAugmentations<Sigil>(build, value, "sigil"); break;
            case ProtectiveEnchantKey: AddAugmentations<ProtectiveEnchant>(build, value, "protective enchant"); break;
            case GlyphsKey:
                foreach (var (name, rank) in Ranked(value))
                {
                    build.Glyphs.Add(new BuildEntry(IdOf(name, Catalog.AllGlyphs.Select(g => new NamedId(g.Name, g.Id)), "glyph"), rank));
                }
                break;
            case GemsKey:
                foreach (var entry in List(value))
                {
                    var (name, power) = Pair(entry);
                    var colors = Enum.GetValues<GemColor>().Where(c => AplNames.Slug(c.ToString()) == name).ToList();
                    if (colors.Count == 0) throw new FormatException($"no gem '{name}'");
                    build.GemPower[colors[0]] = power;
                }
                break;
            case GemSocketsKey:
                foreach (var name in List(value))
                {
                    build.SocketedGemIds.Add((GemItem.All.FirstOrDefault(g => AplNames.Slug(g.Name) == name) ?? throw new FormatException($"no gem '{name}'")).Id);
                }
                break;
            case GearGemBonusesKey:
                foreach (var (name, count) in Ranked(value))
                {
                    var colors = Enum.GetValues<GemColor>().Where(c => AplNames.Slug(c.ToString()) == name).ToList();
                    if (colors.Count == 0) throw new FormatException($"no gem colour '{name}'");
                    build.GearGemBonuses[colors[0]] = count;
                }
                break;
            case StatEnchantsKey:
                foreach (var (name, rating) in Ranked(value))
                {
                    var enchant = StatEnchant.All.FirstOrDefault(e => e.Key == name) ?? throw new FormatException($"no stat enchant '{name}'");
                    build.StatEnchants.Add(new BuildEntry(enchant.Id, rating));
                }
                break;
            case StatEnchantCountsKey:
                foreach (var (name, count) in Ranked(value))
                {
                    var enchant = StatEnchant.All.FirstOrDefault(e => e.Key == name) ?? throw new FormatException($"no stat enchant '{name}'");
                    build.StatEnchantCounts[enchant.Id] = count;
                }
                break;
            case Relic1Key: build.Relic1ItemId = RelicItemId(value); break;
            case Relic2Key: build.Relic2ItemId = RelicItemId(value); break;
            case LegendaryKey:
            {
                var options = Options(value);
                var itemId = (int)Number(options.GetValueOrDefault(IdOption) ?? throw new FormatException("a legendary is <power>,id=<item id>"));
                var legendary = Catalog.CreateLegendary(build.Hero, itemId) ?? throw new FormatException($"no legendary item {itemId} for {build.Hero}");
                if (AplNames.Slug(legendary.Name) != options[NameOption]) throw new FormatException($"item {itemId} grants {AplNames.Slug(legendary.Name)}");
                build.LegendaryItemIds.Add(itemId);
                break;
            }
            case WeaponKey:
                build.WeaponAbilityId = Catalog.AllWeapons.FirstOrDefault(w => AplNames.Slug(w.Name) == value)?.WeaponAbilityId
                                        ?? throw new FormatException($"no weapon '{value}'");
                break;
            default:
                throw new FormatException($"unknown key '{key}'");
        }
    }

    /// <summary>The profile text of <see cref="Build"/>, then <see cref="ActionList"/>.</summary>
    public string Format()
    {
        var build = Build;
        var builder = new StringBuilder();
        builder.AppendLine($"{AplNames.Slug(build.Hero)}=\"{build.Name}\"");
        builder.AppendLine($"{PrimaryStatKey}={Text(build.PrimaryStat)}");
        if (build.Stamina > 0) builder.AppendLine($"{StaminaKey}={Text(build.Stamina)}");
        if (build.Armor > 0) builder.AppendLine($"{ArmorKey}={Text(build.Armor)}");
        builder.AppendLine($"{CritRatingKey}={Text(build.CritRating)}");
        builder.AppendLine($"{HasteRatingKey}={Text(build.HasteRating)}");
        builder.AppendLine($"{ExpertiseRatingKey}={Text(build.ExpertiseRating)}");
        builder.AppendLine($"{SpiritRatingKey}={Text(build.SpiritRating)}");
        if (build.StatsFromOldProfile) builder.AppendLine($"{StatsFromOldProfileKey}={TrueText}");
        if (build.Talents.Count > 0)
        {
            builder.AppendLine($"{TalentsKey}={string.Join('/', build.Talents.Select(id => AplNames.Slug(Catalog.CreateTalent(id)?.Name ?? throw new InvalidOperationException($"Talent {id} is not modeled"))))}");
        }
        var augmentations = build.Augmentations
            .Select(a => new EntryAugmentation(a, Catalog.CreateAugmentation(a.Id, a.Rank) ?? throw new InvalidOperationException($"Augmentation {a.Id} is not in the catalog")))
            .ToList();
        void AppendAugmentations<T>(string key) where T : Augmentation
        {
            var ofKind = augmentations.Where(a => a.Augmentation is T).ToList();
            if (ofKind.Count > 0) builder.AppendLine($"{key}={string.Join('/', ofKind.Select(a => $"{AplNames.Slug(a.Augmentation.Name)}:{a.Entry.Rank}"))}");
        }
        AppendAugmentations<Aspect>(AspectsKey);
        AppendAugmentations<Sigil>(SigilsKey);
        AppendAugmentations<ProtectiveEnchant>(ProtectiveEnchantKey);
        if (build.Glyphs.Count > 0)
        {
            builder.AppendLine($"{GlyphsKey}={string.Join('/', build.Glyphs.Select(g => $"{AplNames.Slug(Catalog.CreateGlyph(g.Id, g.Rank)?.Name ?? throw new InvalidOperationException($"Glyph {g.Id} is not in the catalog"))}:{g.Rank}"))}");
        }
        if (build.GemPower.Count > 0)
        {
            builder.AppendLine($"{GemsKey}={string.Join('/', build.GemPower.Select(g => $"{AplNames.Slug(g.Key.ToString())}:{Text(g.Value)}"))}");
        }
        if (build.SocketedGemIds.Count > 0)
        {
            builder.AppendLine($"{GemSocketsKey}={string.Join('/', build.SocketedGemIds.Select(id => AplNames.Slug((GemItem.Find(id) ?? throw new InvalidOperationException($"Gem {id} is not known")).Name)))}");
        }
        if (build.GearGemBonuses.Count > 0)
        {
            builder.AppendLine($"{GearGemBonusesKey}={string.Join('/', build.GearGemBonuses.Select(g => $"{AplNames.Slug(g.Key.ToString())}:{g.Value}"))}");
        }
        if (build.StatEnchants.Count > 0)
        {
            builder.AppendLine($"{StatEnchantsKey}={string.Join('/', build.StatEnchants.Select(e => $"{(StatEnchant.Find(e.Id) ?? throw new InvalidOperationException($"Stat enchant {e.Id} is not known")).Key}:{Text(e.Rank)}"))}");
        }
        if (build.StatEnchantCounts.Count > 0)
        {
            builder.AppendLine($"{StatEnchantCountsKey}={string.Join('/', build.StatEnchantCounts.Select(e => $"{(StatEnchant.Find(e.Key) ?? throw new InvalidOperationException($"Stat enchant {e.Key} is not known")).Key}:{e.Value}"))}");
        }
        RelicLine(builder, Relic1Key, build.Relic1ItemId);
        RelicLine(builder, Relic2Key, build.Relic2ItemId);
        foreach (var itemId in build.LegendaryItemIds)
        {
            var legendary = Catalog.CreateLegendary(build.Hero, itemId) ?? throw new InvalidOperationException($"Legendary item {itemId} is not in the catalog");
            builder.AppendLine($"{LegendaryKey}={AplNames.Slug(legendary.Name)},{IdOption}={itemId}");
        }
        if (build.WeaponAbilityId != 0)
        {
            var weapon = Catalog.CreateWeapon(build.WeaponAbilityId) ?? throw new InvalidOperationException($"Weapon ability {build.WeaponAbilityId} is not in the catalog");
            builder.AppendLine($"{WeaponKey}={AplNames.Slug(weapon.Name)}");
        }
        if (ActionList.Length > 0)
        {
            builder.AppendLine();
            builder.Append(ActionList);
        }
        return builder.ToString();
    }

    private static FormatException Error(int lineNumber, string line, string problem) =>
        new($"Profile line {lineNumber} '{line}': {problem}.");

    /// <summary>Ranked augmentations of one kind (<typeparamref name="T"/>): "name:rank/...".</summary>
    private static void AddAugmentations<T>(CharacterBuild build, string value, string kind) where T : Augmentation
    {
        var known = Catalog.AllAugmentations.OfType<T>().Select(a => new NamedId(a.Name, a.Id)).ToList();
        foreach (var (name, rank) in Ranked(value)) build.Augmentations.Add(new BuildEntry(IdOf(name, known, kind), rank));
    }

    private static IEnumerable<string> List(string value) => value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<NamedRank> Ranked(string value) =>
        List(value).Select(Pair).Select(p => new NamedRank(p.Name, (int)p.Value));

    /// <summary>"name:value" or "name=value".</summary>
    private static NamedValue Pair(string entry)
    {
        var at = entry.IndexOfAny([':', '=']);
        if (at <= 0) throw new FormatException($"expected name:value in '{entry}'");
        return new NamedValue(entry[..at].Trim(), Number(entry[(at + 1)..]));
    }

    /// <summary>The item ID of an "item,id=item id" relic slot, checked against the item's name.</summary>
    private static int RelicItemId(string value)
    {
        var options = Options(value);
        var itemId = (int)Number(options.GetValueOrDefault(IdOption) ?? throw new FormatException("a relic is <item>,id=<item id>"));
        var relic = Catalog.RelicItemOf(itemId) ?? throw new FormatException($"no relic item {itemId}");
        if (AplNames.Slug(relic.Name) != options[NameOption]) throw new FormatException($"item {itemId} is {AplNames.Slug(relic.Name)}");
        return itemId;
    }

    private static void RelicLine(StringBuilder builder, string key, int itemId)
    {
        if (itemId == 0) return;
        var relic = Catalog.RelicItemOf(itemId) ?? throw new InvalidOperationException($"Relic item {itemId} is not in the catalog");
        builder.AppendLine($"{key}={AplNames.Slug(relic.Name)},{IdOption}={itemId}");
    }

    /// <summary>"name,key=value,...": the name under the empty key.</summary>
    private static Dictionary<string, string> Options(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        var options = new Dictionary<string, string> { [NameOption] = parts[0] };
        foreach (var part in parts.Skip(1))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) throw new FormatException($"expected key=value in '{part}'");
            options[part[..eq]] = part[(eq + 1)..];
        }
        return options;
    }

    private static int IdOf(string name, IEnumerable<NamedId> known, string kind) =>
        known.FirstOrDefault(k => AplNames.Slug(k.Name) == name) is { Id: > 0 } found
            ? found.Id
            : throw new FormatException($"no {kind} '{name}'");

    private static double Number(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"'{text}' is not a number");

    private static double Percent(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.EndsWith('%')) throw new FormatException($"'{text}' is not a percentage such as 24%");
        return Number(trimmed[..^1]) / Percentage.PerFraction;
    }

    private static string Text(double value) => value.ToString(NumberFormat, CultureInfo.InvariantCulture);

    /// <summary>A talent, glyph or augmentation by name and ID.</summary>
    private readonly record struct NamedId(string Name, int Id);

    /// <summary>A name and the rank written after it.</summary>
    private readonly record struct NamedRank(string Name, int Rank);

    /// <summary>A name and the number written after it.</summary>
    private readonly record struct NamedValue(string Name, double Value);

    /// <summary>A build entry and the augmentation it names.</summary>
    private readonly record struct EntryAugmentation(BuildEntry Entry, Augmentation Augmentation);
}
