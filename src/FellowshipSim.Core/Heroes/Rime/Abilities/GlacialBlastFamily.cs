using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Shared rules of Glacial Blast and Talon Strike (Talon Strike benefits from talents that affect Glacial Blast).</summary>
public abstract class GlacialBlastFamily : RimeAbility
{
    /// <summary>Talent 38 Glacial Assault: "deals 40% more damage" at 4 stacks.</summary>
    public const double GlacialAssaultMultiplier = 1.40;
    /// <summary>Talent 38: "dealing 30% of the initial damage to nearby enemies" (ability 2023's text says 20%).</summary>
    public const double GlacialAssaultExplosionFraction = 0.30;
    public const int GlacialAssaultExplosionId = 2023;
    /// <summary>
    /// Combat logs: the explosion is 30% of the hit actually dealt (crit included), never crits itself, and loses damage
    /// as sqrt(12/N) beyond 12 targets (yMAZVJL9hTpFjqbC fight 7 @ 31.8s: explosion/spike = 0.2598 on 16, 0.2520 on 17).
    /// </summary>
    public const double GlacialAssaultExplosionTargetThreshold = 12;

    protected GlacialBlastFamily(Rime owner, int id, string name) : base(owner, id, name, AbilityCategory.Power) { }

    protected bool GlacialAssaultReady => Rime.GlacialAssaultStacks >= 4;

    /// <summary>Called when a Glacial Assault hit on <paramref name="target"/> lands, with the damage it dealt.</summary>
    protected void Explode(SimulationContext ctx, EnemyTarget target, double initialDamage)
    {
        if (initialDamage <= 0.0) return;
        var nearby = ctx.Outcomes.SelectTargets(GlacialAssaultExplosionId, ctx.ActiveEnemies.Where(e => e != target).ToList(), ctx);
        if (nearby.Count == 0) return;
        double falloff = nearby.Count > GlacialAssaultExplosionTargetThreshold ? Math.Sqrt(GlacialAssaultExplosionTargetThreshold / nearby.Count) : 1.0;
        double damage = initialDamage * GlacialAssaultExplosionFraction * falloff;
        foreach (var enemy in nearby)
        {
            ctx.ApplyDamage(Owner, enemy, damage, GlacialAssaultExplosionId, "Glacial Assault", canCrit: false, 0.0, 1.0);
        }
    }
}
