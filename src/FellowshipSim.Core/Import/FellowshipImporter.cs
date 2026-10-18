using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Blessings;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Customization.Legendaries;
using FellowshipSim.Core.Customization.Sets;
using FellowshipSim.Core.Customization.Traits;
using FellowshipSim.Core.Customization.Weapons;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Heroes.Rime;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Import;

public static class FellowshipImporter
{
    public const int StrandOfEternityEffectId = 3087;
    public const int MoteOfCreationEffectId = 3085;

    public static Rime ImportRime(CharacterSnapshotDto snapshot, ICodexDataService codex, GameData? data = null)
    {
        var rime = new Rime(data) { Name = snapshot.Name, GameDate = snapshot.GameDate };

        // Base stats: ratings from gear, and the character sheet's percentages when the snapshot comes from a log
        // (they already include passive bonuses such as gem nodes and set bonuses).
        rime.Stats.BaseIntellect = snapshot.Intellect;
        rime.Stats.Stamina = snapshot.Stamina;
        rime.Stats.Armor = snapshot.Armor;
        rime.Stats.CritRating = snapshot.CritRating;
        rime.Stats.HasteRating = snapshot.HasteRating;
        rime.Stats.ExpertiseRating = snapshot.ExpertiseRating;
        rime.Stats.SpiritRating = snapshot.Spirit;
        if (snapshot.SheetPercents.Count > 0)
        {
            rime.Stats.SheetCritChance = snapshot.SheetPercents.GetValueOrDefault("crit");
            rime.Stats.SheetHaste = snapshot.SheetPercents.GetValueOrDefault("haste");
            rime.Stats.SheetExpertise = snapshot.SheetPercents.GetValueOrDefault("expertise");
            rime.Stats.SheetSpirit = snapshot.SheetPercents.GetValueOrDefault("spirit");
        }

        rime.Initialize();

        foreach (var talentKey in snapshot.Talents)
        {
            rime.TalentNames.Add(talentKey);
            Talent? talent = Normalize(talentKey) switch
            {
                "glacialassault" => new GlacialAssault(),
                "bitingcold" => new BitingCold(),
                "navirskeeper" => new NavirsKeeper(),
                "cascadingblitz" => new CascadingBlitz(),
                "burstbolter" => new Burstbolter(),
                "burstingswallows" => new BurstingSwallows(),
                "icytalons" => new IcyTalons(),
                "chillingfinesse" => new ChillingFinesse(),
                "wisdomofthenorth" => new WisdomOfTheNorth(),
                "greaterglacialblast" => new GreaterGlacialBlast(),
                "harrowingice" => new HarrowingIce(),
                "supremetorrent" => new SupremeTorrent(),
                "coalescingfrost" => new CoalescingFrost(),
                "coldshower" => new ColdShower(),
                "avalanche" => new Avalanche(),
                "icyflow" => new IcyFlow(),
                "frostweaverswrath" => new FrostweaversWrath(),
                "soulfrosttorrent" => new SoulfrostTorrent(),
                _ => null
            };
            if (talent == null)
            {
                rime.UnmodeledFeatures.Add($"Talent: {talentKey}");
                continue;
            }
            rime.Talents.Add(talent);
            talent.Bind(rime);
        }

        foreach (var (gemKey, power) in snapshot.Gems)
        {
            var key = gemKey.ToLowerInvariant();
            Gem? gem = key switch
            {
                var k when k.Contains("emerald") => new Emerald { Power = power },
                var k when k.Contains("ruby") => new Ruby { Power = power },
                var k when k.Contains("amethyst") => new Amethyst { Power = power },
                var k when k.Contains("topaz") => new Topaz { Power = power },
                var k when k.Contains("sapphire") => new Sapphire { Power = power },
                var k when k.Contains("diamond") => new Diamond { Power = power },
                _ => null
            };
            if (gem == null)
            {
                rime.UnmodeledFeatures.Add($"Gem: {gemKey}");
                continue;
            }
            gem.Initialize(codex);
            rime.Gems.Add(gem);
            gem.Bind(rime);
            rime.UnmodeledFeatures.AddRange(gem.UnmodeledNodes.Select(n => $"Gem node: {n}"));
        }

        foreach (var (blessingKey, rank) in snapshot.Blessings)
        {
            Blessing? blessing = Normalize(blessingKey).Replace("the", "") switch
            {
                "intrepid" => new TheIntrepid { Rank = rank },
                "trickster" => new TheTrickster { Rank = rank },
                "executioner" => new TheExecutioner { Rank = rank },
                "celestial" => new TheCelestial { Rank = rank },
                "sinister" => new TheSinister { Rank = rank },
                "heretic" => new TheHeretic { Rank = rank },
                "philosopher" => new ThePhilosopher { Rank = rank },
                "vainglorious" => new TheVainglorious { Rank = rank },
                "wayfarer" => new TheWayfarer { Rank = rank },
                "mystic" => new TheMystic { Rank = rank },
                "monarch" => new TheMonarch { Rank = rank },
                "usurper" => new TheUsurper { Rank = rank },
                "herald" => new TheHerald { Rank = rank },
                "vehement" => new TheVehement { Rank = rank },
                _ => null
            };
            if (blessing == null || !blessing.IsModeled)
            {
                rime.UnmodeledFeatures.Add($"Blessing: {blessingKey} (rank {rank})");
                if (blessing == null) continue;
            }
            blessing.Initialize(codex);
            rime.Blessings.Add(blessing);
            blessing.Bind(rime);
        }

        // Weapon before traits: some traits key off the weapon ability.
        if (!string.IsNullOrWhiteSpace(snapshot.Weapon))
        {
            Weapon? weapon = snapshot.Weapon.ToLowerInvariant() switch
            {
                var w when w.Contains("chronoshift") || w.Contains("asha") => new AshasChronoshiftSpire(),
                _ => null
            };
            if (weapon == null) rime.UnmodeledFeatures.Add($"Weapon: {snapshot.Weapon}");
            weapon?.Equip(rime);
        }

        foreach (var (traitKey, rank) in snapshot.WeaponTraits)
        {
            Trait? trait = Normalize(traitKey) switch
            {
                "kindling" => new Kindling { Rank = rank },
                "willfulmomentum" => new WillfulMomentum { Rank = rank },
                "vengefulsoul" => new VengefulSoul { Rank = rank },
                "seizedopportunity" => new SeizedOpportunity { Rank = rank },
                "martialinitiative" => new MartialInitiative { Rank = rank },
                "inspiredallegiance" => new InspiredAllegiance { Rank = rank },
                "hiddenpower" => new HiddenPower { Rank = rank },
                "heroicbrand" => new HeroicBrand { Rank = rank },
                "heartofstone" => new HeartOfStone { Rank = rank },
                "huntersfocus" => new HuntersFocus { Rank = rank },
                "ironspikes" => new IronSpikes { Rank = rank },
                "kingofthehill" => new KingOfTheHill { Rank = rank },
                "latentresurgence" => new LatentResurgence { Rank = rank },
                "navigatorsintuition" => new NavigatorsIntuition { Rank = rank },
                "patientsoul" => new PatientSoul { Rank = rank },
                "rubystorm" => new RubyStorm { Rank = rank },
                "sapphireaurastone" => new SapphireAurastone { Rank = rank },
                "stalwartreadiness" => new StalwartReadiness { Rank = rank },
                "treasurehuntersdelight" => new TreasureHuntersDelight { Rank = rank },
                "visionsofgrandeur" => new VisionsOfGrandeur { Rank = rank },
                "amethystsplinters" => new AmethystSplinters { Rank = rank },
                "bravemachinations" => new BraveMachinations { Rank = rank },
                "diamondstrike" => new DiamondStrike { Rank = rank },
                "divinemediation" => new DivineMediation { Rank = rank },
                "emeraldjudgement" => new EmeraldJudgement { Rank = rank },
                "firstmanstanding" => new FirstManStanding { Rank = rank },
                "groundedspirit" => new GroundedSpirit { Rank = rank },
                _ => null
            };
            if (trait == null || !trait.IsModeled)
            {
                rime.UnmodeledFeatures.Add($"Trait: {traitKey} (rank {rank})");
                if (trait == null) continue;
            }
            trait.Initialize(codex);
            rime.Traits.Add(trait);
            trait.Bind(rime);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Legendary))
        {
            Legendary? legendary = Normalize(snapshot.Legendary) switch
            {
                var l when l.Contains("undulating") => new UndulatingSpirit(),
                var l when l.Contains("frozenlight") => new RepositoryOfFrozenLight(),
                var l when l.Contains("voidbringer") => new VoidbringersTouch(),
                var l when l.Contains("earthbreaker") => new Earthbreaker(),
                _ => null
            };
            if (legendary == null || !legendary.IsModeled) rime.UnmodeledFeatures.Add($"Legendary: {snapshot.Legendary}");
            if (legendary != null)
            {
                legendary.Initialize(codex);
                rime.Legendaries.Add(legendary);
                legendary.Bind(rime);
            }
            // Every legendary item also carries Strand of Eternity (effect 3087, +10% cooldown recovery) and Mote of
            // Creation (effect 3085, +10% primary stat; already in the character sheet's primary stat when imported from a log).
            rime.PassiveAuras.Add(AuraDef.FromEffect(rime.Data, StrandOfEternityEffectId));
            if (!rime.Stats.UsesSheetValues) rime.PassiveAuras.Add(AuraDef.FromEffect(rime.Data, MoteOfCreationEffectId));
        }

        foreach (var setKey in snapshot.Sets)
        {
            SetBonus? set = Normalize(setKey) switch
            {
                var s when s.Contains("baelaurum") => new TormentOfBaelAurum(),
                var s when s.Contains("drakheim") => new DrakheimsAbsolution(),
                var s when s.Contains("heskyr") => new SealOfTheHeskyr(),
                var s when s.Contains("grasp") => new DeathsGrasp(),
                var s when s.Contains("draconic") => new DraconicMight(),
                var s when s.Contains("prophecy") => new DarkProphecy(),
                _ => null
            };
            if (set == null)
            {
                rime.UnmodeledFeatures.Add($"Set: {setKey}");
                continue;
            }
            set.Bind(rime);
            rime.UnmodeledFeatures.AddRange(set.UnmodeledParts.Select(p => $"Set {set.Name}: {p}"));
        }

        return rime;
    }

    private static string Normalize(string key) =>
        key.ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("'", "");
}
