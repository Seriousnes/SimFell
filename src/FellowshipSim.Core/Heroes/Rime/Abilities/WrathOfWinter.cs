using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>
/// Ability 1023 (Spirit ability) / effect 1387: +20% damage for 20s and 1 Winter Orb every 4s; grants
/// Spirit of Heroism (effect 2253, +30% Haste).
/// </summary>
public class WrathOfWinter : RimeAbility
{
    public const int AbilityId = 1023;
    public const double OrbIntervalSeconds = 4.0;

    public WrathOfWinter(Rime owner) : base(owner, AbilityId)
    {
        // Combat logs: cast 0.265s after a Cold Snap, inside its global cooldown (yMAZVJL9hTpFjqbC fight 7 @ 27.284).
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        hero.ActivateSpiritOfHeroism(ctx);
        hero.Auras.Apply(hero.Aura.WrathOfWinter, ctx.CurrentTime);

        double duration = hero.Aura.WrathOfWinter.Duration.TotalSeconds;
        // Not in the description, but every log shows it: the first orb comes on activation (yMAZVJL9hTpFjqbC fight 7 @ 709.809: 4 -> 5 orbs).
        // The datamine marks the ability scalesWithHaste: the 4 seconds between orbs are hasted (logs: orbs every ~2.4s at ~69% haste),
        // by the haste when the previous orb came (yMAZVJL9hTpFjqbC fight 7: orbs at ~40.2 and ~42.5 despite The Philosopher
        // lapsing 38.44-39.17 in between).
        PeriodicEffect.BeginHasted(ctx, hero, Name, duration, OrbIntervalSeconds, tickOnStart: true, partialFinalTick: false,
            onTick: (c, _) => hero.GenerateWinterOrbs(1, c), retimeOnHasteChange: false);
        TriggerCooldown(ctx);
    }
}
