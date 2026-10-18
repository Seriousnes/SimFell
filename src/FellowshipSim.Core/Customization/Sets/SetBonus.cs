using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

public abstract class SetBonus
{
    public abstract string Name { get; }

    /// <summary>Parts of the bonus the engine does not model yet (reported).</summary>
    public virtual IReadOnlyList<string> UnmodeledParts => Array.Empty<string>();

    public abstract void Bind(Hero hero);
}

/// <summary>
/// Set bonus 686 / effect 1988: +5% Intellect (in the character sheet's Intellect when imported from a log;
/// applied otherwise). The low-health heal is defensive.
/// </summary>
public class TormentOfBaelAurum : SetBonus
{
    public const int EffectId = 1988;
    public override string Name => "Torment of Bael'Aurum";
    public override IReadOnlyList<string> UnmodeledParts => ["low-health heal"];

    public override void Bind(Hero hero)
    {
        if (!hero.Stats.UsesSheetValues) hero.PassiveAuras.Add(AuraDef.FromEffect(hero.Data, EffectId));
    }
}

/// <summary>
/// Set bonus 681 / set-bonus-power 246 / effect 1994: "Your abilities have a chance (0.8 PPM) to increase your Haste
/// by 25% for 20 seconds." Rolled on every ability used.
/// </summary>
public class DarkProphecy : SetBonus
{
    public const int EffectId = 1994;
    public const int PowerId = 246;
    public const double DurationSeconds = 20.0;
    public const string ProcKey = "dark_prophecy";
    public override string Name => "Dark Prophecy";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        if (aura.Duration == SimTime.Infinite) aura = aura.WithDuration(DurationSeconds);
        var ppm = System.Text.RegularExpressions.Regex.Match(hero.Data.Raw("set-bonus-power", PowerId).GetProperty("description").GetString()!, @"\(([\d.]+) PPM\)");
        var rppm = new RealPpm(ProcKey, double.Parse(ppm.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (hero.SimContext is { } ctx && rppm.Roll(hero, ctx, aura.LogId)) hero.Auras.Apply(aura, now);
        };
    }
}

/// <summary>Set bonus 691 / effect 1993: the Spirit ability grants +20% Intellect for 20 seconds.</summary>
public class DrakheimsAbsolution : SetBonus
{
    public const int EffectId = 1993;
    public override string Name => "Drakheim's Absolution";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category == "Spirit") hero.Auras.Apply(aura, now);
        };
    }
}

/// <summary>Set bonus 5638: +25% gem power (in the gem powers reported by the log).</summary>
public class SealOfTheHeskyr : SetBonus
{
    public override string Name => "Seal of the Heskyr";
    public override void Bind(Hero hero) { }
}

/// <summary>
/// Set bonus 1771: +4% Spirit (in the character sheet's Spirit when imported from a log) and +10% damage
/// (effect 1984, DamageScale x1.1) to "Low Health" enemies: at or below 30% health (not in the datamine; confirmed by logs).
/// </summary>
public class DeathsGrasp : SetBonus
{
    public const double SpiritBonus = 0.04;
    public const int EffectId = 1984;
    public const double LowHealthThreshold = 0.30;
    public override string Name => "Death's Grasp";

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusSpirit += SpiritBonus;
        double multiplier = AuraDef.FromEffect(hero.Data, EffectId).Modifiers
            .Where(m => m.Type == StatType.DamageScale).Select(m => m.Value).DefaultIfEmpty(1.0).First();
        hero.TargetDamageMultipliers.Add((target, _) => target.HealthFraction <= LowHealthThreshold ? multiplier : 1.0);
    }
}

public class DraconicMight : SetBonus
{
    public override string Name => "Draconic Might";
    public override IReadOnlyList<string> UnmodeledParts => ["entire bonus"];
    public override void Bind(Hero hero) { }
}
