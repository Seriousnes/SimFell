using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Customization.Legendaries;
using FellowshipSim.Core.Customization.Sets;

namespace FellowshipSim.Core.Import;

/// <summary>
/// Turns a <see cref="CharacterBuild"/> into a hero ready to simulate: its stats, then its talents, gems, blessings,
/// weapon, traits, legendary and sets, each found in the <see cref="Catalog"/> by game ID. Anything the engine does not
/// model is listed in <see cref="Hero.UnmodeledFeatures"/>.
/// </summary>
public static class HeroImporter
{
    public static Hero Import(CharacterBuild build)
    {
        var hero = Catalog.CreateHero(build.Hero);
        hero.Name = build.Name;

        // Base stats: ratings from gear, and the character sheet's percentages when the build comes from a log
        // (they already include passive bonuses such as gem nodes and set bonuses).
        hero.Stats.PrimaryStat = build.PrimaryStat;
        hero.Stats.Stamina = build.Stamina;
        hero.Stats.Armor = build.Armor;
        hero.Stats.CritRating = build.CritRating;
        hero.Stats.HasteRating = build.HasteRating;
        hero.Stats.ExpertiseRating = build.ExpertiseRating;
        hero.Stats.SpiritRating = build.SpiritRating;
        if (build.SheetPercents.Count > 0)
        {
            hero.Stats.SheetCritChance = build.SheetPercents.GetValueOrDefault("crit");
            hero.Stats.SheetHaste = build.SheetPercents.GetValueOrDefault("haste");
            hero.Stats.SheetExpertise = build.SheetPercents.GetValueOrDefault("expertise");
            hero.Stats.SheetSpirit = build.SheetPercents.GetValueOrDefault("spirit");
        }

        hero.Initialize();

        foreach (int talentId in build.Talents)
        {
            var talent = Catalog.CreateTalent(talentId);
            if (talent == null)
            {
                hero.UnmodeledFeatures.Add($"Talent {talentId}");
                continue;
            }
            hero.Talents.Add(talent);
            talent.Bind(hero);
        }

        foreach (var (color, power) in build.GemPower)
        {
            Gem gem = color switch
            {
                GemColor.Emerald => new Emerald(),
                GemColor.Ruby => new Ruby(),
                GemColor.Amethyst => new Amethyst(),
                GemColor.Topaz => new Topaz(),
                GemColor.Sapphire => new Sapphire(),
                _ => new Diamond(),
            };
            gem.Power = power;
            hero.Gems.Add(gem);
            gem.Bind(hero);
            hero.UnmodeledFeatures.AddRange(gem.UnmodeledNodes.Select(n => $"Gem node: {n}"));
        }

        foreach (var entry in build.Blessings)
        {
            var blessing = Catalog.CreateBlessing(entry.Id, entry.Rank);
            if (blessing == null || !blessing.IsModeled)
            {
                hero.UnmodeledFeatures.Add($"Blessing: {blessing?.Name ?? entry.Name ?? entry.Id.ToString()} (rank {entry.Rank})");
                if (blessing == null) continue;
            }
            hero.Blessings.Add(blessing);
            blessing.Bind(hero);
        }

        // Weapon before traits: some traits key off the weapon ability.
        if (build.WeaponAbilityId != 0)
        {
            var weapon = Catalog.CreateWeapon(build.WeaponAbilityId);
            if (weapon == null) hero.UnmodeledFeatures.Add($"Weapon with ability {build.WeaponAbilityId}");
            weapon?.Equip(hero);
        }

        foreach (var entry in build.Traits)
        {
            var trait = Catalog.CreateTrait(entry.Id, entry.Rank);
            if (trait == null || !trait.IsModeled)
            {
                hero.UnmodeledFeatures.Add($"Trait: {trait?.Name ?? entry.Name ?? entry.Id.ToString()} (rank {entry.Rank})");
                if (trait == null) continue;
            }
            hero.Traits.Add(trait);
            trait.Bind(hero);
        }

        if (build.LegendaryItemIds.Count > 0)
        {
            foreach (int itemId in build.LegendaryItemIds)
            {
                var legendary = Catalog.CreateLegendary(build.Hero, itemId);
                if (legendary == null || !legendary.IsModeled) hero.UnmodeledFeatures.Add($"Legendary item {itemId}");
                if (legendary == null) continue;
                hero.Legendaries.Add(legendary);
                legendary.Bind(hero);
            }
            hero.PassiveAuras.Add(LegendaryStats.StrandOfEternity);
            if (!hero.Stats.UsesSheetValues) hero.PassiveAuras.Add(LegendaryStats.MoteOfCreation);
        }

        // Two pieces of a set give its bonus. Sets bind in name order.
        var sets = build.SetBonusIds
            .Select(Catalog.SetOf)
            .GroupBy(t => t)
            .Where(g => g.Count() >= SetBonus.PiecesForBonus)
            .Select(g => g.Key)
            .ToList();
        foreach (var setType in sets.Where(t => t != null).Select(t => Catalog.CreateSet(t!)).OrderBy(s => SortKey(s.Name), StringComparer.Ordinal))
        {
            setType.Bind(hero);
            hero.Sets.Add(setType);
            hero.UnmodeledFeatures.AddRange(setType.UnmodeledParts.Select(p => $"Set {setType.Name}: {p}"));
        }
        int unknownSetPieces = build.SetBonusIds.Count(id => Catalog.SetOf(id) == null);
        if (unknownSetPieces > 0)
        {
            hero.UnmodeledFeatures.Add($"Set pieces of unknown sets: {string.Join(", ", build.SetBonusIds.Where(id => Catalog.SetOf(id) == null).Distinct())}");
        }

        return hero;
    }

    private static string SortKey(string name) => name.ToLowerInvariant().Replace("'", "").Replace(' ', '_');
}
