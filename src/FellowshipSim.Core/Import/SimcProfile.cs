using System.Globalization;
using System.Text;
using FellowshipSim.Core.APL;
using FellowshipSim.Core.Customization.Gems;

namespace FellowshipSim.Core.Import;

/// <summary>
/// A character profile in simc style: one <c>key=value</c> per line, names as action-list names ("winters_blessing").
/// Stats are exact percentages; each rating is the one that gives its percentage.
/// <code>
/// rime="sww"
/// primary_stat=300
/// crit=30%
/// haste=24%
/// talents=avalanche/coalescing_frost/...
/// traits=martial_initiative:2/...
/// blessings=the_monarch:1/...
/// gems=sapphire:1500/...
/// set_bonus=dark_prophecy_2pc=1
/// legendary=frostwyrms_spite,id=5899
/// weapon=ashas_chronoshift_spire
/// actions+=/frost_bolt
/// </code>
/// Lines starting with '#' are comments. <c>actions</c> lines are the profile's action list.
/// </summary>
public sealed record SimcProfile(CharacterBuild Build, string ActionList)
{
    private const string SetBonusSuffix = "_2pc";

    public static SimcProfile Load(string path) => Parse(File.ReadAllText(path));

    public static SimcProfile Parse(string text)
    {
        var build = new CharacterBuild();
        var actions = new StringBuilder();
        Hero? hero = null;
        int lineNumber = 0;
        foreach (string raw in text.Split('\n'))
        {
            lineNumber++;
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("actions", StringComparison.Ordinal))
            {
                actions.AppendLine(line);
                continue;
            }
            int eq = line.IndexOf('=');
            if (eq <= 0) throw Error(lineNumber, line, "expected key=value");
            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
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
                Read(build, hero, key, value);
            }
            catch (FormatException e)
            {
                throw Error(lineNumber, line, e.Message);
            }
        }
        if (hero == null) throw new FormatException("The profile has no hero line.");
        return new SimcProfile(build, actions.ToString());
    }

    private static void Read(CharacterBuild build, Hero hero, string key, string value)
    {
        switch (key)
        {
            case "primary_stat": build.PrimaryStat = Number(value); break;
            case "stamina": build.Stamina = Number(value); break;
            case "armor": build.Armor = Number(value); break;
            case "crit":
                build.SheetPercents["crit"] = Percent(value);
                build.CritRating = StatDiminishingReturns.ToRating(Percent(value) - StatBlock.BaseCritChance);
                break;
            case "haste":
                build.SheetPercents["haste"] = Percent(value);
                build.HasteRating = StatDiminishingReturns.ToRating(Percent(value));
                break;
            case "expertise":
                build.SheetPercents["expertise"] = Percent(value);
                build.ExpertiseRating = StatDiminishingReturns.ToRating(Percent(value));
                break;
            case "spirit":
                build.SheetPercents["spirit"] = Percent(value);
                build.SpiritRating = StatDiminishingReturns.ToRating(Percent(value) - StatBlock.BaseSpirit);
                break;
            case "talents":
                var tree = hero.TalentTreeSlots.Select(s => Catalog.CreateTalent(s.TalentId)).OfType<Talent>().Select(t => (t.Name, t.Id));
                foreach (string name in List(value)) build.Talents.Add(IdOf(name, tree, "talent"));
                break;
            case "traits":
                foreach (var (name, rank) in Ranked(value))
                {
                    build.Traits.Add(new BuildEntry(IdOf(name, Catalog.AllTraits.Select(t => (t.Name, t.Id)), "trait"), rank));
                }
                break;
            case "blessings":
                foreach (var (name, rank) in Ranked(value))
                {
                    build.Blessings.Add(new BuildEntry(IdOf(name, Catalog.AllBlessings.Select(b => (b.Name, b.Id)), "blessing"), rank));
                }
                break;
            case "gems":
                foreach (string entry in List(value))
                {
                    var (name, power) = Pair(entry);
                    var colors = Enum.GetValues<GemColor>().Where(c => AplNames.Slug(c.ToString()) == name).ToList();
                    if (colors.Count == 0) throw new FormatException($"no gem '{name}'");
                    build.GemPower[colors[0]] = power;
                }
                break;
            case "set_bonus":
            {
                var (pieces, enabled) = Pair(value);
                if (!pieces.EndsWith(SetBonusSuffix)) throw new FormatException($"a set bonus is <set>{SetBonusSuffix}=1");
                string name = pieces[..^SetBonusSuffix.Length];
                var set = Catalog.AllSets.FirstOrDefault(s => AplNames.Slug(s.Name) == name)
                          ?? throw new FormatException($"no set '{name}'");
                if (enabled != 0) build.SetBonusIds.AddRange(Enumerable.Repeat(set.SetBonusIds[0], Customization.Sets.SetBonus.PiecesForBonus));
                break;
            }
            case "legendary":
            {
                var options = Options(value);
                int itemId = (int)Number(options.GetValueOrDefault("id") ?? throw new FormatException("a legendary is <power>,id=<item id>"));
                var legendary = Catalog.CreateLegendary(build.Hero, itemId) ?? throw new FormatException($"no legendary item {itemId} for {build.Hero}");
                if (AplNames.Slug(legendary.Name) != options[""]) throw new FormatException($"item {itemId} grants {AplNames.Slug(legendary.Name)}");
                build.LegendaryItemIds.Add(itemId);
                break;
            }
            case "weapon":
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
        var s = new StringBuilder();
        s.AppendLine($"{AplNames.Slug(build.Hero)}=\"{build.Name}\"");
        s.AppendLine($"primary_stat={Text(build.PrimaryStat)}");
        if (build.Stamina > 0) s.AppendLine($"stamina={Text(build.Stamina)}");
        if (build.Armor > 0) s.AppendLine($"armor={Text(build.Armor)}");
        // The log's character sheet when there is one, otherwise the percentages of the ratings.
        double Sheet(string stat, double baseValue, double rating) =>
            build.SheetPercents.TryGetValue(stat, out double sheet) ? sheet : baseValue + StatDiminishingReturns.FromRating(rating);
        s.AppendLine($"crit={PercentText(Sheet("crit", StatBlock.BaseCritChance, build.CritRating))}");
        s.AppendLine($"haste={PercentText(Sheet("haste", 0, build.HasteRating))}");
        s.AppendLine($"expertise={PercentText(Sheet("expertise", 0, build.ExpertiseRating))}");
        s.AppendLine($"spirit={PercentText(Sheet("spirit", StatBlock.BaseSpirit, build.SpiritRating))}");
        if (build.Talents.Count > 0)
        {
            s.AppendLine($"talents={string.Join('/', build.Talents.Select(id => AplNames.Slug(Catalog.CreateTalent(id)?.Name ?? throw new InvalidOperationException($"Talent {id} is not modeled"))))}");
        }
        if (build.Traits.Count > 0)
        {
            s.AppendLine($"traits={string.Join('/', build.Traits.Select(t => $"{AplNames.Slug(Catalog.CreateTrait(t.Id, t.Rank)?.Name ?? throw new InvalidOperationException($"Trait {t.Id} is not in the catalog"))}:{t.Rank}"))}");
        }
        if (build.Blessings.Count > 0)
        {
            s.AppendLine($"blessings={string.Join('/', build.Blessings.Select(b => $"{AplNames.Slug(Catalog.CreateBlessing(b.Id, b.Rank)?.Name ?? throw new InvalidOperationException($"Blessing {b.Id} is not in the catalog"))}:{b.Rank}"))}");
        }
        if (build.GemPower.Count > 0)
        {
            s.AppendLine($"gems={string.Join('/', build.GemPower.Select(g => $"{AplNames.Slug(g.Key.ToString())}:{Text(g.Value)}"))}");
        }
        foreach (var set in build.SetBonusIds.Select(Catalog.SetOf).OfType<Type>().GroupBy(t => t)
                     .Where(g => g.Count() >= Customization.Sets.SetBonus.PiecesForBonus).Select(g => Catalog.CreateSet(g.Key)))
        {
            s.AppendLine($"set_bonus={AplNames.Slug(set.Name)}{SetBonusSuffix}=1");
        }
        foreach (int itemId in build.LegendaryItemIds)
        {
            var legendary = Catalog.CreateLegendary(build.Hero, itemId) ?? throw new InvalidOperationException($"Legendary item {itemId} is not in the catalog");
            s.AppendLine($"legendary={AplNames.Slug(legendary.Name)},id={itemId}");
        }
        if (build.WeaponAbilityId != 0)
        {
            var weapon = Catalog.CreateWeapon(build.WeaponAbilityId) ?? throw new InvalidOperationException($"Weapon ability {build.WeaponAbilityId} is not in the catalog");
            s.AppendLine($"weapon={AplNames.Slug(weapon.Name)}");
        }
        if (ActionList.Length > 0)
        {
            s.AppendLine();
            s.Append(ActionList);
        }
        return s.ToString();
    }

    private static FormatException Error(int lineNumber, string line, string problem) =>
        new($"Profile line {lineNumber} '{line}': {problem}.");

    private static IEnumerable<string> List(string value) => value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<(string Name, int Rank)> Ranked(string value) =>
        List(value).Select(Pair).Select(p => (p.Name, (int)p.Value));

    /// <summary>"name:value" or "name=value".</summary>
    private static (string Name, double Value) Pair(string entry)
    {
        int at = entry.IndexOfAny([':', '=']);
        if (at <= 0) throw new FormatException($"expected name:value in '{entry}'");
        return (entry[..at].Trim(), Number(entry[(at + 1)..]));
    }

    /// <summary>"name,key=value,...": the name under the empty key.</summary>
    private static Dictionary<string, string> Options(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        var options = new Dictionary<string, string> { [""] = parts[0] };
        foreach (string part in parts.Skip(1))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0) throw new FormatException($"expected key=value in '{part}'");
            options[part[..eq]] = part[(eq + 1)..];
        }
        return options;
    }

    private static int IdOf(string name, IEnumerable<(string Name, int Id)> known, string kind) =>
        known.FirstOrDefault(k => AplNames.Slug(k.Name) == name) is { Id: > 0 } found
            ? found.Id
            : throw new FormatException($"no {kind} '{name}'");

    private static double Number(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : throw new FormatException($"'{text}' is not a number");

    private static double Percent(string text)
    {
        string trimmed = text.Trim();
        if (!trimmed.EndsWith('%')) throw new FormatException($"'{text}' is not a percentage such as 24%");
        return Number(trimmed[..^1]) / 100.0;
    }

    private static string Text(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string PercentText(double fraction) => $"{Text(Math.Round(fraction * 100, 2))}%";
}
