using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

// Values come from the datamine blessing records (descriptions and placeholder constants) and the named effects.

/// <summary>Blessing 700 / effect 3181: [POWER] abilities grant Critical Strike chance for 4 seconds.</summary>
public class TheTrickster : Blessing
{
    public const int EffectId = 3181;
    public const double DurationSeconds = 4.0;
    public override string Name => "The Trickster";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.CritChance, DescriptionValue(hero)));
        hero.OnCastStarted += (ability, _, now) =>
        {
            if (ability.Category == "Power") hero.Auras.Apply(aura, now);
        };
    }
}

/// <summary>
/// Blessing 709 / effect 3189: [MAJOR] abilities grant +0.2% Haste, Expertise and Critical Strike per X% Spirit
/// (X by rank), up to 50% Spirit, for 8 seconds.
/// </summary>
public class ThePhilosopher : Blessing
{
    public const int EffectId = 3189;
    public const double DurationSeconds = 8.0;
    /// <summary>
    /// Not in the datamine; measured from logs: a refresh keeps up to 30% (2.4s) of the time left, so a refreshed buff
    /// lasts up to 10.4s (yMAZVJL9hTpFjqbC fight 7: every refresh with over 2.4s left is removed 10.42s later).
    /// </summary>
    public const double RefreshCarryoverFraction = 0.3;
    public override string Name => "The Philosopher";

    public override void Bind(Hero hero)
    {
        var baseAura = hero.DefineAura(EffectId).WithDuration(DurationSeconds) with { RefreshCarryoverFraction = RefreshCarryoverFraction };
        double increase = RankData.EffectConstant(hero.Data, EffectId, "IncreasePerSpirit", Rank);
        double threshold = RankData.EffectConstant(hero.Data, EffectId, "SpiritThreshold", Rank);
        double spiritPerIncrease = RankData.EffectConstant(hero.Data, EffectId, "CurrentSpiritPerIncrease", Rank);
        // Applied once the MAJOR ability resolves, so its own effects (e.g. Winter's Blessing's Spirit) count.
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category != "Major") return;
            double spiritPercent = Math.Min(hero.SpiritPercent(now), threshold) * 100.0;
            double bonus = increase * spiritPercent / spiritPerIncrease;
            hero.Auras.Apply(baseAura.WithModifiers(
                StatModifier.Add(StatType.Haste, bonus),
                StatModifier.Add(StatType.Expertise, bonus),
                StatModifier.Add(StatType.CritChance, bonus)), now);
        };
    }
}

/// <summary>Blessing 706: POWER abilities gain X% power per 3% Spirit, up to 50% Spirit.</summary>
public class TheMystic : Blessing
{
    public override string Name => "The Mystic";

    public override void Bind(Hero hero)
    {
        double perStep = DescriptionValue(hero);
        double step = RankData.BlessingConstant(hero.Data, Name, "SpiritAmountPerPowerInPercentage", Rank);
        double threshold = RankData.BlessingConstant(hero.Data, Name, "SpiritThreshold", Rank);
        hero.AbilityDamageMultipliers.Add((ability, now) =>
            ability.Category == "Power"
                ? 1.0 + perStep * (Math.Min(hero.SpiritPercent(now), threshold) * 100.0 / step)
                : 1.0);
    }
}

/// <summary>Blessing 705: MAJOR abilities gain Cooldown Acceleration, +0.1% per 1% Haste up to +5%.</summary>
public class TheMonarch : Blessing
{
    public const double HasteBonusPerPercent = 0.001;
    public const double MaxHasteBonus = 0.05;
    public override string Name => "The Monarch";

    public override void Bind(Hero hero)
    {
        double acceleration = DescriptionValue(hero);
        hero.CooldownAccelerations.Add((ability, now) =>
            ability.Category == "Major"
                ? acceleration + Math.Min(MaxHasteBonus, HasteBonusPerPercent * hero.GetHaste(now) * 100.0)
                : 0.0);
    }
}

/// <summary>Blessing 699: start dungeons with Spirit Points (no effect on damage).</summary>
public class TheHerald : Blessing
{
    public override string Name => "The Herald";
    public override void Bind(Hero hero) { }
}

// Blessings below have no engine model yet; they are reported, not guessed.
public class TheIntrepid : UnmodeledBlessing { public override string Name => "The Intrepid"; }
public class TheExecutioner : UnmodeledBlessing { public override string Name => "The Executioner"; }
public class TheCelestial : UnmodeledBlessing { public override string Name => "The Celestial"; }
public class TheSinister : UnmodeledBlessing { public override string Name => "The Sinister"; }
public class TheHeretic : UnmodeledBlessing { public override string Name => "The Heretic"; }
public class TheVainglorious : UnmodeledBlessing { public override string Name => "The Vainglorious"; }
public class TheWayfarer : UnmodeledBlessing { public override string Name => "The Wayfarer"; }
public class TheUsurper : UnmodeledBlessing { public override string Name => "The Usurper"; }
/// <summary>
/// Blessing 712: every 6 / 5 / 4 / 3 BASIC abilities, the target erupts, dealing that hit's damage again (effect 3193,
/// cannot crit) to all enemies around it, increased by your Critical Strike chance up to 50%. Counter: effect 3194.
/// Beyond 5 targets the eruption loses damage as sqrt(5/N) (TargetCountScaling 5, fs_tc_uploads s3/gear_data.json;
/// combat logs: e.g. a 7,818 Cold Snap crit -> 7,818 x 1.5 x sqrt(5/20) = 5,864 on 20 enemies, exactly as logged).
/// </summary>
public class TheVehement : Blessing
{
    public const int DamageEffectId = 3193;
    public const int CounterEffectId = 3194;
    public const double TargetCountScaling = 5.0;
    public override string Name => "The Vehement";

    public override void Bind(Hero hero)
    {
        int threshold = (int)RankData.EffectConstant(hero.Data, CounterEffectId, "Finesse.BasicToAoe.CounterThreshold", Rank);
        double critCap = Constant(hero, "CritChanceThreshold");
        var counter = hero.DefineAura(CounterEffectId).WithMaxStacks(threshold - 1) with { Duration = SimTime.Infinite };
        int damageId = GameData.EffectLogId(DamageEffectId);
        string damageName = hero.Data.Effect(DamageEffectId).Name;
        // The counter advances when a BASIC ability's hit lands (combat logs: Frost Bolt counts when the bolt hits, not when
        // its cast completes), and the hit that reaches the threshold erupts.
        var basicIds = hero.Abilities.Where(a => a.Category == "Basic").Select(a => a.DamageLogId).ToHashSet();
        int hits = 0;
        hero.OnDamageDealt += dmg =>
        {
            if (dmg.SimContext == null || dmg.Target == null || !basicIds.Contains(dmg.AbilityId)) return;
            hits++;
            if (hits < threshold)
            {
                hero.Auras.Apply(counter, dmg.Timestamp, setStacks: hits);
                return;
            }
            hits = 0;
            hero.Auras.Remove(counter, dmg.Timestamp);
            var ctx = dmg.SimContext;
            var enemies = ctx.Outcomes.SelectTargets(damageId, ctx.ActiveEnemies.ToList(), ctx);
            if (enemies.Count == 0) return;
            double falloff = enemies.Count > TargetCountScaling ? Math.Sqrt(TargetCountScaling / enemies.Count) : 1.0;
            double damage = dmg.Amount * (1.0 + Math.Min(hero.CritChance(dmg.Timestamp), critCap)) * falloff;
            foreach (var enemy in enemies)
            {
                ctx.ApplyDamage(hero, enemy, damage, damageId, damageName, canCrit: false, 0.0, 1.0);
            }
        };
    }
}
