using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Shared Frost Swallow hit: scales with Spirit; each hit that lands extends Ice Blitz and may trigger Bursting Swallows.</summary>
public abstract class FrostSwallowAttack : RimeAbility
{
    /// <summary>"For every 1% Spirit you have, your Frost Swallows deal 1.5% more damage."</summary>
    public const double DamagePerSpiritPercent = 1.5;
    public const string BurstingSwallowsProc = "bursting_swallows";

    protected FrostSwallowAttack(Rime owner, int id, string name, double coefficient) : base(owner, id, name)
    {
        BaseCoefficient = coefficient;
        owner.OnDamageDealt += OnSwallowHit;
    }

    public override double BaseDamage(SimulationContext ctx, EnemyTarget? target, double damageMultiplier = 1.0) =>
        base.BaseDamage(ctx, target, damageMultiplier) * (1.0 + DamagePerSpiritPercent * Owner.SpiritPercent(ctx.CurrentTime));

    public void Attack(SimulationContext ctx, EnemyTarget target, int count)
    {
        if (target == null) return;
        for (int i = 0; i < count; i++) DealDamage(ctx, target);
    }

    private void OnSwallowHit(DamageContext dmg)
    {
        if (dmg.AbilityId != DamageLogId || dmg.SimContext == null || dmg.Target == null) return;
        var ctx = dmg.SimContext;
        var hero = Rime;

        // "The duration of Ice Blitz is extended by 0.1 seconds each time a Frost Swallow hits an enemy."
        hero.Auras.Extend(IceBlitz.Buff, SimTime.FromSeconds(IceBlitz.ExtensionPerSwallowSeconds));

        // Talent 628: "Your Frost Swallow hits have a 12% chance to trigger a pulse of Bursting Ice on the target."
        if (hero.HasTalent<BurstingSwallows>() &&
            ctx.Outcomes.RollProc(BurstingSwallowsProc, BurstingSwallows.ProcChance, ctx, LogIds.Effect(BurstingIce.PulseEffectId)))
        {
            hero.GetAbility<BurstingIce>()?.DealPulse(ctx, dmg.Target);
        }
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target) => Attack(ctx, target, 1);
}
