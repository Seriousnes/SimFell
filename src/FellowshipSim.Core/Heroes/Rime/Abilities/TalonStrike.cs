using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 2223 (Icy Talons). One ice spike per Winter Orb spent; costs all Winter Orbs.</summary>
public class TalonStrike : GlacialBlastFamily
{
    public const int AbilityId = 2223;
    public const string AbilityName = "Talon Strike";
    /// <summary>Per ice spike (40% of Glacial Blast).</summary>
    public const double Coefficient = 4.096;

    public TalonStrike(Rime owner) : base(owner, AbilityId, AbilityName)
    {
        BaseCoefficient = Coefficient;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (!Rime.HasIcyTalons)
        {
            reason = $"{Name} needs Icy Talons";
            return false;
        }
        if (Rime.WinterOrbs < 1 && !GlacialAssaultReady)
        {
            reason = $"{Name} needs a Winter Orb";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        bool assault = GlacialAssaultReady;
        // With Glacial Assault it is free and fires as if cast with full Winter Orbs (per the user; Rising Talons never
        // consumes Glacial Assault in the logs).
        int spikes = assault ? Rime.MaxWinterOrbs : Math.Max(1, hero.WinterOrbs);
        double multiplier = 1.0;
        if (assault)
        {
            // Glacial Assault: no cost and 40% more damage.
            hero.Auras.Remove(GlacialAssault.Buff, ctx.CurrentTime);
            multiplier = GlacialAssaultMultiplier;
            // INFERRED from logs (not the tooltip): cast "as if" with full Winter Orbs, it counts them as spent for Wisdom of
            // the North. Across 10 logs this removes 46 of 62 Ice Blitz cooldown misses while some casts still land right
            // as Ice Blitz comes back (so it is not too fast).
            hero.NotifyResourceSpent(Rime.WinterOrbsResource, spikes, ctx.CurrentTime);
        }
        else
        {
            hero.SpendWinterOrbs(spikes, ctx);
        }

        // Talon Strike benefits from talents that affect Glacial Blast: every spike of the cast gets the consumed crit buffs.
        double crit = hero.ConsumeSpenderCritBuffs(ctx);
        for (int i = 0; i < spikes; i++)
        {
            DealDamage(ctx, target, damageMultiplier: multiplier, onLanded: assault ? (c, dealt) => Explode(c, target, dealt) : null, extraCritChance: crit);
        }
    }
}
