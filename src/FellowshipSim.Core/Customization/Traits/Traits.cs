using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

// Rank values come from the datamine "trait" records (rankRuns); effects from the named effect records.

/// <summary>Trait 51 / effect 2174: the weapon ability grants Main Stat for a fraction of its default cooldown.</summary>
public class MartialInitiative : Trait
{
    public const int EffectId = 2174;
    public override string Name => "Martial Initiative";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        double fraction = RankValue(hero);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability != hero.WeaponAbility) return;
            hero.Auras.Apply(aura.WithDuration(fraction * ability.Cooldown.TotalSeconds), now);
        };
    }
}

/// <summary>
/// Trait 31: "When you deal Critical Damage to an enemy, you deal an additional X% of that damage to them over 8 seconds"
/// (effect 101, cannot crit). One Amethyst Splinter per target. Like a hasted damage-over-time effect, each tick is
/// worth a quarter of its damage (8 seconds of 2-second ticks) and haste adds ticks within the 8 seconds, so it deals
/// X% x (1 + haste) in total. A crit adds its share to what the ticks left would still deal and restarts the 8 seconds;
/// ticks keep their rhythm. (Combat logs, 10 Rime logs: splinter damage is ~1.5x X% of crit damage at every rank; the
/// datamine gives no tick period.)
/// </summary>
public class AmethystSplinters : Trait
{
    public const int DotEffectId = 101;
    public const double DurationSeconds = 8.0;
    public const double TickPeriodSeconds = 2.0;
    public const double BaseTicks = DurationSeconds / TickPeriodSeconds;
    public override string Name => "Amethyst Splinters";

    private sealed class Splinter
    {
        public double TickValue;
        public SimTime EndsAt;
        public HastedTimer? NextTick;
    }

    public override void Bind(Hero hero)
    {
        double share = RankValue(hero);
        int logId = GameData.EffectLogId(DotEffectId);
        string name = hero.Data.Effect(DotEffectId).Name;
        var splinters = new Dictionary<EnemyTarget, Splinter>();

        void Tick(SimulationContext ctx, EnemyTarget target, Splinter s)
        {
            s.NextTick = null;
            if (target.IsSleeping || ctx.CurrentTime >= s.EndsAt) return;
            double value = s.TickValue;
            // The crit's damage already includes the hero's damage buffs, so the ticks are not scaled again.
            ctx.ApplyDamage(hero, target, logId, name, canCrit: false, _ => new SimulationContext.HitNumbers(value, 0.0, 1.0));
            // Keep the rhythm: a crit landing before the next tick restarts the 8 seconds.
            s.NextTick = HastedTimer.Start(ctx, hero, TickPeriodSeconds, c => Tick(c, target, s), name);
        }

        hero.OnDamageDealt += dmg =>
        {
            if (!dmg.IsCrit || dmg.Target == null || dmg.Target.IsSleeping || dmg.SimContext == null || dmg.AbilityId == logId) return;
            var ctx = dmg.SimContext;
            var now = ctx.CurrentTime;
            if (!splinters.TryGetValue(dmg.Target, out var s)) splinters[dmg.Target] = s = new Splinter();
            // The damage not yet dealt (the time left in unhasted ticks) carries over into the new 8 seconds.
            double left = s.EndsAt > now ? s.TickValue * (s.EndsAt - now).TotalSeconds / TickPeriodSeconds : 0.0;
            s.TickValue = (left + dmg.Amount * share) / BaseTicks;
            s.EndsAt = now + SimTime.FromSeconds(DurationSeconds);
            if (s.NextTick == null)
            {
                var target = dmg.Target;
                s.NextTick = HastedTimer.Start(ctx, hero, TickPeriodSeconds, c => Tick(c, target, s), name);
            }
        };
    }
}

/// <summary>Trait 47: activating the Spirit ability reduces the weapon ability's cooldown by a fraction of its default cooldown.</summary>
public class VisionsOfGrandeur : Trait
{
    public override string Name => "Visions of Grandeur";

    public override void Bind(Hero hero)
    {
        double fraction = RankValue(hero);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category != "Spirit" || hero.WeaponAbility == null) return;
            hero.ReduceCooldown(hero.WeaponAbility.Name, SimTime.FromSeconds(fraction * hero.WeaponAbility.Cooldown.TotalSeconds), now);
        };
    }
}

/// <summary>
/// Trait 34 / trait-power 376: damage has a chance (rank PPM) to conjure an Emerald Spike for 800% Main Stat damage
/// (effect 93), which also triggers the Emerald gem's First Strike.
/// </summary>
public class EmeraldJudgement : Trait
{
    public const int TraitPowerId = 376;
    public const int DamageEffectId = 93;
    public const string ProcKey = "emerald_judgement";
    /// <summary>The spike erupts from the ground a moment after the proc (logs: about 0.5s).</summary>
    public const double SpikeDelaySeconds = 1.0;
    public override string Name => "Emerald Judgement";

    public override void Bind(Hero hero)
    {
        var rppm = new RealPpm(ProcKey, RankValue(hero));
        int logId = GameData.EffectLogId(DamageEffectId);
        string name = hero.Data.Effect(DamageEffectId).Name;
        var match = Regex.Match(hero.Data.TraitPower(TraitPowerId).Description, @"(\d+)% Main Stat damage");
        double coefficient = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;

        hero.OnDamageDealt += dmg =>
        {
            if (dmg.Target == null || dmg.Target.IsSleeping || dmg.SimContext == null || dmg.AbilityId == logId) return;
            var ctx = dmg.SimContext;
            // The spike erupts under the enemy whose hit triggered it.
            var target = dmg.Target;
            if (!rppm.Roll(hero, ctx, logId, SpikeDelaySeconds, target)) return;

            // The spike also triggers First Strike, when it lands (combat logs: First Strike refreshes with the spike's hit).
            var firstStrike = hero.Gems.OfType<Emerald>().FirstOrDefault()?.FirstStrike;
            ctx.ApplyDamage(hero, target, logId, name, canCrit: true, c => new SimulationContext.HitNumbers(
                coefficient * hero.PrimaryAttribute(c.CurrentTime) * (1.0 + hero.Expertise(c.CurrentTime))
                    * hero.DamageScale(c.CurrentTime) * hero.DamageMultiplierAgainst(target, c.CurrentTime),
                hero.CritChanceAgainst(target, c.CurrentTime),
                hero.CritMultiplier(c.CurrentTime)),
                onLanded: firstStrike == null ? null : (c, dealt) => { if (dealt > 0.0) hero.Auras.Apply(firstStrike, c.CurrentTime); });
        };
    }
}

/// <summary>
/// Trait 42 / effect 2141 ("Trait : Focused Haste"): targeted offensive abilities grant Haste rating for 8s, stacking to 5;
/// lost when a targeted offensive ability is used on another enemy.
/// </summary>
public class HuntersFocus : Trait
{
    public const int EffectId = 2141;
    public const double DurationSeconds = 8.0;
    public override string Name => "Hunter's Focus";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.HasteRating, RankValue(hero)));
        EnemyTarget? focus = null;
        hero.OnCastCompleted += (ability, target, now) =>
        {
            if (target == null || ability.BaseCoefficient <= 0) return;
            if (focus != null && focus != target) hero.Auras.Remove(aura, now);
            focus = target;
            hero.Auras.Apply(aura, now);
        };
    }
}

/// <summary>
/// Trait 4: every 20 critical strikes (counted by effect 2111, not while the buff is active) grant Critical Strike
/// rating for 12s (effect 2110).
/// </summary>
public class SeizedOpportunity : Trait
{
    public const int CounterEffectId = 2111;
    public const int BuffEffectId = 2110;
    public const double DurationSeconds = 12.0;
    public override string Name => "Seized Opportunity";

    public override void Bind(Hero hero)
    {
        var counter = hero.DefineAura(CounterEffectId);
        var buff = hero.DefineAura(BuffEffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.CritRating, RankValue(hero)));
        hero.OnDamageDealt += dmg =>
        {
            if (!dmg.IsCrit || hero.Auras.HasAura(buff, dmg.Timestamp)) return;
            var stacks = hero.Auras.Apply(counter, dmg.Timestamp).Stacks;
            if (stacks >= counter.MaxStacks)
            {
                hero.Auras.Remove(counter, dmg.Timestamp);
                hero.Auras.Apply(buff, dmg.Timestamp);
            }
        };
    }
}

// Traits below have no engine model yet (defensive, healing, or not implemented); they are reported, not guessed.
public class Kindling : UnmodeledTrait { public override string Name => "Kindling"; }
public class WillfulMomentum : UnmodeledTrait { public override string Name => "Willful Momentum"; }
public class VengefulSoul : UnmodeledTrait { public override string Name => "Vengeful Soul"; }
public class InspiredAllegiance : UnmodeledTrait { public override string Name => "Inspired Allegiance"; }
public class HiddenPower : UnmodeledTrait { public override string Name => "Hidden Power"; }
public class HeroicBrand : UnmodeledTrait { public override string Name => "Heroic Brand"; }
public class HeartOfStone : UnmodeledTrait { public override string Name => "Heart of Stone"; }
public class IronSpikes : UnmodeledTrait { public override string Name => "Iron Spikes"; }
public class KingOfTheHill : UnmodeledTrait { public override string Name => "King of the Hill"; }
public class LatentResurgence : UnmodeledTrait { public override string Name => "Latent Resurgence"; }
public class NavigatorsIntuition : UnmodeledTrait { public override string Name => "Navigator's Intuition"; }
public class PatientSoul : UnmodeledTrait { public override string Name => "Patient Soul"; }
public class RubyStorm : UnmodeledTrait { public override string Name => "Ruby Storm"; }
public class SapphireAurastone : UnmodeledTrait { public override string Name => "Sapphire Aurastone"; }
public class StalwartReadiness : UnmodeledTrait { public override string Name => "Stalwart Readiness"; }
public class TreasureHuntersDelight : UnmodeledTrait { public override string Name => "Treasure Hunter's Delight"; }
public class BraveMachinations : UnmodeledTrait { public override string Name => "Brave Machinations"; }
public class DiamondStrike : UnmodeledTrait { public override string Name => "Diamond Strike"; }
public class DivineMediation : UnmodeledTrait { public override string Name => "Divine Mediation"; }
public class FirstManStanding : UnmodeledTrait { public override string Name => "First Man Standing"; }
public class GroundedSpirit : UnmodeledTrait { public override string Name => "Grounded Spirit"; }
