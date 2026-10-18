using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.WeaponAbilities;

/// <summary>
/// Weapon ability of Asha's Chronoshift Spire (ability 1558): 1s cast, then a 3s channel pulsing every 1.5s (hasted)
/// at all nearby enemies. While channeling, Asha's Arcane Embrace (effect 2279) gives 800% increased cooldown recovery.
/// </summary>
public class Chronoshift : Ability
{
    public const int AbilityId = 1558;
    public const int BarrierEffectId = 2279;
    /// <summary>Combat logs: pulses lose damage as sqrt(5/N) beyond 5 targets.</summary>
    public const double TargetThreshold = 5;

    private readonly AuraDef _barrier;

    public Chronoshift(Hero owner) : base(owner, AbilityId)
    {
        // Logs: a pulse lands as the channel starts, then every hasted period, plus a partial final pulse.
        ChannelTicksOnStart = true;
        ChannelPartialFinalTick = true;
        AoeTargetThreshold = TargetThreshold;
        _barrier = owner.DefineAura(BarrierEffectId);
        // Chronoshift's own cooldown recovers at 8x while channeling too (per the user).
        OwnRecoveryAuraLogIds.Add(_barrier.LogId);
        // The barrier's CooldownRecovery 8 joins the recovery pool while channeling (+7: cooldowns tick at 8x),
        // and leaves it when the channel ends or is stopped.
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        TriggerCooldown(ctx);
        Owner.Auras.Apply(_barrier, ctx.CurrentTime);
    }

    public override void OnChannelTick(SimulationContext ctx, EnemyTarget target, double fraction)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target != null) enemies.Add(target);
        DealAoeDamage(ctx, enemies, damageMultiplier: fraction);
    }

    public override void OnChannelEnd(SimulationContext ctx, EnemyTarget target, bool clipped)
    {
        Owner.Auras.Remove(_barrier, ctx.CurrentTime);
    }
}
