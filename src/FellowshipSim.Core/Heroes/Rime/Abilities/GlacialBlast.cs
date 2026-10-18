using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1028. Costs 2 Winter Orbs.</summary>
public class GlacialBlast : GlacialBlastFamily
{
    public const int AbilityId = 1028;
    public const int DefaultWinterOrbCost = 2;

    public GlacialBlast(Rime owner) : base(owner, AbilityId)
    {
        WinterOrbCost = DefaultWinterOrbCost;
    }

    public override SimTime GetAdjustedCastTime(SimTime now = default)
    {
        // Instant with 4 Glacial Assault stacks or during Wrath of Winter.
        if (GlacialAssaultReady || Rime.Auras.HasAura(Rime.Aura.WrathOfWinter, now)) return SimTime.Zero;
        double seconds = CastTime.TotalSeconds + (Rime.HasTalent<GreaterGlacialBlast>() ? GreaterGlacialBlast.CastTimeIncreaseSeconds : 0.0);
        // Icy Flow: "+50% Haste to you while casting" the Glacial Blast that consumes it.
        double icyFlowHaste = Rime.Auras.HasAura(Rime.Aura.IcyFlow, now) ? IcyFlow.CastHaste : 0.0;
        return SimTime.FromSeconds(seconds / (1.0 + Owner.GetHaste(now) + icyFlowHaste));
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} is replaced by Talon Strike (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost && !GlacialAssaultReady)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        bool assault = GlacialAssaultReady;
        double multiplier = 1.0;
        if (assault)
        {
            hero.Auras.Remove(hero.Aura.GlacialAssault, ctx.CurrentTime);
            multiplier = GlacialAssaultMultiplier;
        }
        else
        {
            hero.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        }

        double crit = hero.ConsumeSpenderCritBuffs(ctx);
        DealDamage(ctx, target, damageMultiplier: multiplier, onLanded: assault ? (c, dealt) => Explode(c, target, dealt) : null, extraCritChance: crit);
    }
}
