using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Customization.Sets;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Import;
using CoreCatalog = FellowshipSim.Core.Catalog;

namespace FellowshipSim.UI.Build;

/// <summary>
/// The changes the build editor makes to a <see cref="CharacterBuild"/>. A rating set by hand replaces the log's
/// character-sheet percentage of its stat (<see cref="CharacterBuild.SheetPercents"/>).
/// </summary>
public sealed class BuildEditor(CharacterBuild build)
{
    public CharacterBuild Build { get; } = build;

    public BuildCatalog Catalog => BuildCatalog.For(Build.Hero);

    /// <summary>A new build of <paramref name="hero"/> with nothing picked.</summary>
    public static CharacterBuild NewBuild(string hero) => new() { Hero = hero, Name = "New build", PrimaryStat = 100 };

    public int PointsLeft => Catalog.PointsLeft(Build.Talents);

    public bool HasTalent(int id) => Build.Talents.Contains(id);

    /// <summary>Picks the talent, or drops it when picked. A pick that costs more than the points left is refused: false.</summary>
    public bool ToggleTalent(TalentOption talent)
    {
        if (Build.Talents.Remove(talent.Slot.TalentId)) return true;
        if (talent.Slot.Cost > PointsLeft) return false;
        Build.Talents.Add(talent.Slot.TalentId);
        return true;
    }

    public void ClearTalents() => Build.Talents.Clear();

    public static string Key(RatedStat stat) => stat.ToString().ToLowerInvariant();

    /// <summary>The percentage (a fraction) a stat has with no rating: 5% crit.</summary>
    public static double BasePercent(RatedStat stat) => stat switch
    {
        RatedStat.Crit => StatBlock.BaseCritChance,
        RatedStat.Spirit => StatBlock.BaseSpirit,
        _ => 0.0,
    };

    public double Rating(RatedStat stat) => stat switch
    {
        RatedStat.Crit => Build.CritRating,
        RatedStat.Haste => Build.HasteRating,
        RatedStat.Expertise => Build.ExpertiseRating,
        _ => Build.SpiritRating,
    };

    public void SetRating(RatedStat stat, double rating)
    {
        rating = Math.Max(0, rating);
        switch (stat)
        {
            case RatedStat.Crit: Build.CritRating = rating; break;
            case RatedStat.Haste: Build.HasteRating = rating; break;
            case RatedStat.Expertise: Build.ExpertiseRating = rating; break;
            default: Build.SpiritRating = rating; break;
        }
        Build.SheetPercents.Remove(Key(stat));
    }

    /// <summary>The stat's character-sheet percentage (a fraction): the log's when the build came from one, otherwise its rating's.</summary>
    public double Percent(RatedStat stat) =>
        Build.SheetPercents.TryGetValue(Key(stat), out double sheet) ? sheet : BasePercent(stat) + StatDiminishingReturns.FromRating(Rating(stat));

    /// <summary>True when <see cref="Percent"/> is the log's character sheet, which counts gem nodes and set bonuses too.</summary>
    public bool PercentFromLog(RatedStat stat) => Build.SheetPercents.ContainsKey(Key(stat));

    /// <summary>The weapon's ability ID, or null for no weapon.</summary>
    public int? Weapon
    {
        get => Build.WeaponAbilityId == 0 ? null : Build.WeaponAbilityId;
        set => Build.WeaponAbilityId = value ?? 0;
    }

    /// <summary>The item ID of the legendary worn, or null. A build wears one legendary.</summary>
    public int? Legendary
    {
        get => Build.LegendaryItemIds.Count == 0 ? null : Build.LegendaryItemIds[0];
        set
        {
            Build.LegendaryItemIds.Clear();
            if (value is { } id) Build.LegendaryItemIds.Add(id);
        }
    }

    /// <summary>True when the build has the bonus of the set whose first set-bonus ID is <paramref name="setId"/>.</summary>
    public bool HasSet(int setId) =>
        CoreCatalog.SetOf(setId) is { } type && Build.SetBonusIds.Count(id => CoreCatalog.SetOf(id) == type) >= SetBonus.PiecesForBonus;

    /// <summary>Wears two pieces of the set (its bonus), or none.</summary>
    public void SetSet(int setId, bool on)
    {
        if (CoreCatalog.SetOf(setId) is not { } type) return;
        Build.SetBonusIds.RemoveAll(id => CoreCatalog.SetOf(id) == type);
        if (on) Build.SetBonusIds.AddRange(Enumerable.Repeat(setId, SetBonus.PiecesForBonus));
    }

    public double Gem(GemColor color) => Build.GemPower.GetValueOrDefault(color);

    public void SetGem(GemColor color, double power)
    {
        if (power > 0) Build.GemPower[color] = power;
        else Build.GemPower.Remove(color);
    }

    public int TraitRank(int id) => Build.Traits.FirstOrDefault(t => t.Id == id)?.Rank ?? 0;

    public void SetTraitRank(BuildOption trait, int rank) => SetRank(Build.Traits, trait, rank);

    public int BlessingRank(int id) => Build.Blessings.FirstOrDefault(b => b.Id == id)?.Rank ?? 0;

    public void SetBlessingRank(BuildOption blessing, int rank) => SetRank(Build.Blessings, blessing, rank);

    private static void SetRank(List<BuildEntry> entries, BuildOption option, int rank)
    {
        rank = Math.Clamp(rank, 0, option.MaxRank);
        int at = entries.FindIndex(e => e.Id == option.Id);
        if (rank == 0)
        {
            if (at >= 0) entries.RemoveAt(at);
        }
        else if (at >= 0) entries[at] = entries[at] with { Rank = rank };
        else entries.Add(new BuildEntry(option.Id, rank, option.Name));
    }

    /// <summary>
    /// Removes the parts of an imported build the catalog has no class for, so the build writes as a profile, and returns
    /// their names.
    /// </summary>
    public static IReadOnlyList<string> RemoveUnknown(CharacterBuild build)
    {
        var removed = new List<string>();
        var tree = CoreCatalog.CreateHero(build.Hero).TalentTreeSlots.Select(s => s.TalentId).ToHashSet();
        foreach (int id in build.Talents.Where(id => !tree.Contains(id) || CoreCatalog.CreateTalent(id) == null).ToList())
        {
            build.Talents.Remove(id);
            removed.Add($"talent {id}");
        }
        foreach (var t in build.Traits.Where(t => CoreCatalog.CreateTrait(t.Id, t.Rank) == null).ToList())
        {
            build.Traits.Remove(t);
            removed.Add($"trait {t.Name ?? t.Id.ToString()}");
        }
        foreach (var b in build.Blessings.Where(b => CoreCatalog.CreateBlessing(b.Id, b.Rank) == null).ToList())
        {
            build.Blessings.Remove(b);
            removed.Add($"blessing {b.Name ?? b.Id.ToString()}");
        }
        foreach (int id in build.LegendaryItemIds.Where(id => CoreCatalog.CreateLegendary(build.Hero, id) == null).ToList())
        {
            build.LegendaryItemIds.Remove(id);
            removed.Add($"legendary item {id}");
        }
        if (build.WeaponAbilityId != 0 && CoreCatalog.CreateWeapon(build.WeaponAbilityId) == null)
        {
            removed.Add($"weapon with ability {build.WeaponAbilityId}");
            build.WeaponAbilityId = 0;
        }
        return removed;
    }
}
