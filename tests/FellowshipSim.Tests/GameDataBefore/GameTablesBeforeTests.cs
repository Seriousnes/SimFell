using System.Globalization;
using System.Reflection;
using FellowshipSim.Core;
using FellowshipSim.Core.Customization.Augmentations;
using FellowshipSim.Core.Customization.Glyphs;
using FellowshipSim.Core.Customization.Legendaries;
using FellowshipSim.Core.Customization.RelicPassives;
using FellowshipSim.Core.Customization.Weapons;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Import;
using FellowshipSim.UI.Build;
using Xunit.Abstractions;
using Now = FellowshipSim.Core.Import;

namespace FellowshipSim.Tests.GameDataBefore;

/// <summary>
/// The generated icon URLs and hero data against tools/generate-game-icons.cs's GameIcons.cs and GameHeroData.cs: for each
/// key of each of those tables, the generated entity constant or the generated table entry equals the script's row (a
/// texture name against its URL).
/// </summary>
public class GameTablesBeforeTests(ITestOutputHelper output)
{
    private static readonly Type[] CoreTypes = typeof(Catalog).Assembly.GetTypes();

    private static string Url(string texture) => GameIcons.Url(texture);

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static IEnumerable<(string Key, string Value)> Rows<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> table, Func<TValue, string> value) =>
        table.Select(r => (Convert.ToString(r.Key, CultureInfo.InvariantCulture)!, value(r.Value)));

    private static string Render(IEnumerable<TalentSlot> tree) => string.Join(", ", tree);

    private static string Render(IReadOnlyDictionary<int, string> weapons) => string.Join(", ", weapons.OrderBy(w => w.Key).Select(w => $"{w.Key}={w.Value}"));

    private static object? Member(Type type, string name) =>
        type.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)?.GetValue(null);

    /// <summary>The generated <paramref name="key"/> and <paramref name="value"/> members of each class deriving from <typeparamref name="T"/>.</summary>
    private static IEnumerable<(string Key, string Value)> Entities<T>(string key, string value, Func<object, string>? render = null) =>
        CoreTypes.Where(t => typeof(T).IsAssignableFrom(t))
            .Select(t => (Key: Member(t, key), Value: Member(t, value)))
            .Where(m => m.Key != null && m.Value != null)
            .Select(m => (Convert.ToString(m.Key, CultureInfo.InvariantCulture)!, render != null ? render(m.Value!) : (string)m.Value!));

    private static IEnumerable<(string Key, string Value)> None => [];

    private List<GameTableComparison.Result> Results()
    {
        string[] augmentationTypes = ["Aspect", "Sigil", "ProtectiveEnchant"];
        var results = new List<GameTableComparison.Result>
        {
            GameTableComparison.Compare("Talents", Rows(GameIcons.Talents, Url), Entities<Talent>("TalentId", "TalentIcon"), Rows(Now.GameIcons.Talents, v => v)),
            GameTableComparison.Compare("Augmentations", Rows(GameIcons.Augmentations, Url),
                augmentationTypes.SelectMany(t => Entities<Augmentation>(t + "Id", t + "Icon")), Rows(Now.GameIcons.Augmentations, v => v)),
            GameTableComparison.Compare("Glyphs", Rows(GameIcons.Glyphs, Url), Entities<Glyph>("GlyphId", "GlyphIcon"), Rows(Now.GameIcons.Glyphs, v => v)),
            GameTableComparison.Compare("LegendaryPowers", Rows(GameIcons.LegendaryPowers, Url), Entities<Legendary>("PowerId", "PowerIcon"),
                Rows(Now.GameIcons.LegendaryPowers, v => v)),
            GameTableComparison.Compare("WeaponAbilities", Rows(GameIcons.WeaponAbilities, Url),
                Entities<Weapon>("WeaponPowerAbilityId", "WeaponPowerAbilityIcon"), Rows(Now.GameIcons.WeaponAbilities, v => v)),
            GameTableComparison.Compare("WeaponHeroes", Rows(GameIcons.WeaponHeroes, v => string.Join(", ", v)), None,
                Rows(Now.GameIcons.WeaponHeroes, v => string.Join(", ", v))),
            GameTableComparison.Compare("StatEnchants", Rows(GameIcons.StatEnchants, Url), None, Rows(Now.GameIcons.StatEnchants, v => v)),
            GameTableComparison.Compare("Abilities", Rows(GameIcons.Abilities, Url), None, Rows(Now.GameIcons.Abilities, v => v)),
            GameTableComparison.Compare("Effects", Rows(GameIcons.Effects, Url), None, Rows(Now.GameIcons.Effects, v => v)),
            GameTableComparison.Compare("Names", Rows(GameIcons.Names, Url), None, Rows(Now.GameIcons.Names, v => v), ignoreCase: true),
            GameTableComparison.Compare("GemItems", Rows(GameIcons.GemItems, Url), None, Rows(Now.GameIcons.GemItems, v => v)),
            GameTableComparison.Compare("RelicItems", Rows(GameIcons.RelicItems, Url),
                CoreTypes.Where(t => typeof(RelicPassive).IsAssignableFrom(t)).SelectMany(t => (Member(t, "RelicPassiveItems") as RelicItem[]) ?? [])
                    .Where(i => i.Icon != null).Select(i => (Int(i.Id), i.Icon!)),
                Rows(Now.GameIcons.RelicItems, v => v)),
            GameTableComparison.Compare("AugmentationMaxRanks", Rows(GameIcons.AugmentationMaxRanks, Int), None, Rows(Now.GameIcons.AugmentationMaxRanks, Int)),
            GameTableComparison.Compare("GlyphMaxRanks", Rows(GameIcons.GlyphMaxRanks, Int), None, Rows(Now.GameIcons.GlyphMaxRanks, Int)),
            GameTableComparison.Compare("Gems", Rows(GameIcons.Gems, Url), None, Rows(Now.GameIcons.Gems, v => v)),
            GameTableComparison.Compare("Heroes", Rows(GameIcons.Heroes, Url), None, Rows(Now.GameIcons.Heroes, v => v), ignoreCase: true),
            GameTableComparison.Compare("LegendaryItems", Rows(GameIcons.LegendaryItems, Url), None, Rows(Now.GameIcons.LegendaryItems, v => v), ignoreCase: true),
            GameTableComparison.Compare("GameHeroData.MainStats", Rows(GameHeroData.MainStats, v => v.ToString()),
                Entities<Hero>("HeroName", "HeroMainStat", v => v.ToString()!), Rows(Now.GameHeroData.MainStats, v => v.ToString()), ignoreCase: true),
            GameTableComparison.Compare("GameHeroData.TalentTrees", Rows(GameHeroData.TalentTrees, Render),
                Entities<Hero>("HeroName", "HeroTalentTree", v => Render((TalentSlot[])v)), Rows(Now.GameHeroData.TalentTrees, Render), ignoreCase: true),
            // A talent class with a generated half has TalentIcon; a class with none (Blood & Thunder) has TalentId and
            // TalentName of its own.
            GameTableComparison.Compare("GameHeroData.TalentNames", Rows(GameHeroData.TalentNames, v => v),
                Entities<Talent>("TalentId", "TalentName").Where(t => Entities<Talent>("TalentId", "TalentIcon").Any(i => i.Key == t.Key)),
                Rows(Now.GameHeroData.TalentNames, v => v)),
            GameTableComparison.Compare("GameHeroData.WeaponAbilityNames", Rows(GameHeroData.WeaponAbilityNames, v => v), None,
                Rows(Now.GameHeroData.WeaponAbilityNames, v => v)),
            GameTableComparison.Compare("GameHeroData.Weapons", Rows(GameHeroData.Weapons, Render),
                Entities<Hero>("HeroName", "HeroWeapons", v => Render((IReadOnlyDictionary<int, string>)v)), Rows(Now.GameHeroData.Weapons, Render), ignoreCase: true),
            GameTableComparison.Compare("GameHeroData.Legendaries", Rows(GameHeroData.Legendaries, Render), None, Rows(Now.GameHeroData.Legendaries, Render),
                ignoreCase: true),
        };
        foreach (var result in results) output.WriteLine(result.ToString());
        return results;
    }

    /// <summary>
    /// Every row of today's tables is its generated entity constant or table entry; the one addition is effect 3368, Reduced
    /// Power (Tex_red_16), which today's tables have no row for.
    /// </summary>
    [Fact]
    public void Every_Row_Of_The_Script_Is_Generated()
    {
        var results = Results();
        Assert.All(results, r => Assert.True(r.Differences.Count == 0, r.ToString()));
        Assert.All(results.Where(r => r.Table != "Effects"), r => Assert.True(r.Additions.Count == 0, r.ToString()));
        Assert.Equal([$"3368 = {Url("Tex_red_16")}"], results.Single(r => r.Table == "Effects").Additions);
    }

    /// <summary>
    /// A damage source or aura's icon by its talent or augmentation log ID is today's row of its ID, from the engine
    /// talent or augmentation or the generated table.
    /// </summary>
    [Fact]
    public void A_Log_Ids_Icon_Is_The_Scripts_Row()
    {
        const string noName = "\u0000";
        foreach (var (id, icon) in GameIcons.Talents)
        {
            Assert.Equal(Url(icon), BuildCatalog.IconUrl(LogIds.TalentOffset + id, noName));
        }
        foreach (var (id, icon) in GameIcons.Augmentations)
        {
            Assert.Equal(Url(icon), BuildCatalog.IconUrl(LogIds.AugmentationOffset + id, noName));
        }
    }
}
