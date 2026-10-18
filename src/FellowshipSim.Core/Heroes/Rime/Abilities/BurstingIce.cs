using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>
/// Ability 1031. Applies debuff effect 1395 (3s, pulsing every 0.5s hasted) whose pulses deal damage effect 1396
/// to the target and nearby enemies and generate 1 Anima. While it is active Rime has Winter's Embrace (effect 2303).
/// </summary>
public class BurstingIce : RimeAbility
{
    public const int AbilityId = 1031;
    public const int PulseEffectId = 1396;
    public const double AnimaPerPulse = 1.0;

    private int _activeDebuffs;

    /// <summary>Combat logs: pulses lose damage as sqrt(5/N) beyond 5 targets.</summary>
    public const double TargetThreshold = 5;

    public BurstingIce(Rime owner) : base(owner, AbilityId)
    {
        AoeTargetThreshold = TargetThreshold;
        DamageLogId = GameData.EffectLogId(PulseEffectId);
        var debuff = owner.Data.Effect(1395);
        DurationSeconds = debuff.Duration ?? 3.0;
        PulsePeriodSeconds = debuff.Period ?? 0.5;
    }

    public double DurationSeconds { get; }
    public double PulsePeriodSeconds { get; }

    /// <summary>One pulse around <paramref name="target"/>; <paramref name="fraction"/> scales a partial final pulse.</summary>
    public void DealPulse(SimulationContext ctx, EnemyTarget target, double fraction = 1.0)
    {
        var hero = Rime;
        hero.AddAnima(AnimaPerPulse, ctx, target);
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target != null) enemies.Add(target);

        // Harrowing Ice: the pulse deals damage with the stacks held before it, then adds its own stack; the stack after 30
        // consumes them all and shortens Flight of the Navir.
        double harrowing = 1.0;
        bool harrowingIce = hero.HasTalent<HarrowingIce>();
        if (harrowingIce) harrowing += HarrowingIce.DamagePerStack * hero.Auras.GetStacks(hero.Aura.HarrowingIce, ctx.CurrentTime);
        DealAoeDamage(ctx, enemies, damageMultiplier: fraction * harrowing);
        if (harrowingIce)
        {
            var now = ctx.CurrentTime;
            hero.Auras.Apply(hero.Aura.HarrowingIce, now);
            if (hero.Auras.GetStacks(hero.Aura.HarrowingIce, now) >= hero.Aura.HarrowingIce.MaxStacks)
            {
                hero.Auras.Remove(hero.Aura.HarrowingIce, now);
                hero.ReduceCooldown<FlightOfTheNavir>(SimTime.FromSeconds(HarrowingIce.FlightOfTheNavirReductionSeconds), now);
            }
        }
    }

    /// <summary>"Winter's Embrace does not affect Bursting Ice."</summary>
    protected override double DamageScaleFor(SimulationContext ctx)
    {
        double scale = Owner.DamageScale(ctx.CurrentTime);
        if (Rime.Auras.HasAura(Rime.Aura.WintersEmbrace, ctx.CurrentTime))
        {
            foreach (var mod in Rime.Aura.WintersEmbrace.Modifiers)
            {
                if (mod.Type == StatType.DamageScale && mod.Op == ModifierOp.MultiplyCompound) scale /= mod.Value;
            }
        }
        return scale;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        TriggerCooldown(ctx);

        _activeDebuffs++;
        hero.Auras.Apply(hero.Aura.WintersEmbrace, ctx.CurrentTime);

        PeriodicEffect.BeginHasted(ctx, hero, Name, DurationSeconds, PulsePeriodSeconds, tickOnStart: false, partialFinalTick: true,
            // Combat logs (10 Rime logs, 526 debuffs): the final pulse deals the fraction of a pulse period left when the
            // debuff runs out (leftover 0.4 -> 0.43x, 0.5 -> 0.52x, 0.8 -> 0.78x, 0.9 -> 0.88x of a full pulse).
            onTick: (c, fraction) => DealPulse(c, target, fraction),
            onEnd: (c, _) =>
            {
                if (--_activeDebuffs == 0) hero.Auras.Remove(hero.Aura.WintersEmbrace, c.CurrentTime);
            });
    }

    public void ResetState() => _activeDebuffs = 0;
}
