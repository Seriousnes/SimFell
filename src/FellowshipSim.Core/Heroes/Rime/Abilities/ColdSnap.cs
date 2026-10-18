using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1020. 2 charges, cooldown accelerated by haste, generates 1 Winter Orb.</summary>
public class ColdSnap : RimeAbility
{
    public const int AbilityId = 1020;
    public const int WinterOrbsGained = 1;

    public ColdSnap(Rime owner) : base(owner, AbilityId)
    {
        // "Cold Snap gains Cooldown Acceleration equal to your Haste": its charges recharge with haste added to the rate.
        ChargesRechargeWithHaste = true;
        CooldownScalesWithHaste = false;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        // Navir's Keeper: "causes your next Cold Snap to not require nor cost any charge"
        if (Rime.Auras.HasAura(Rime.Aura.NavirsKeeper, ctx.CurrentTime))
        {
            reason = null;
            return true;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        if (hero.Auras.HasAura(hero.Aura.NavirsKeeper, ctx.CurrentTime))
        {
            hero.Auras.ConsumeStack(hero.Aura.NavirsKeeper, ctx.CurrentTime);
        }
        else
        {
            TriggerCooldown(ctx);
        }

        hero.GenerateWinterOrbs(WinterOrbsGained, ctx, target);
        if (hero.HasTalent<GlacialAssault>())
        {
            hero.Auras.Apply(hero.Aura.GlacialAssault, ctx.CurrentTime);
        }
        if (hero.HasTalent<IcyFlow>())
        {
            hero.Auras.Apply(hero.Aura.IcyFlow, ctx.CurrentTime);
        }
        if (hero.HasTalent<ChillingFinesse>())
        {
            hero.ReduceCooldown<FreezingTorrent>(SimTime.FromSeconds(ChillingFinesse.FreezingTorrentReductionSeconds), ctx.CurrentTime);
        }

        DealDamage(ctx, target);
        hero.TriggerFrostSwallows(ctx, target);
    }
}
