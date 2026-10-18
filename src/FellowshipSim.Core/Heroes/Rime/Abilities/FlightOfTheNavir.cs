using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1009 / effect 2446: 5 Frost Swallows circle Rime for 20s.</summary>
public class FlightOfTheNavir : RimeAbility
{
    public const int AbilityId = 1009;
    public const int SwallowCount = 5;

    public FlightOfTheNavir(Rime owner) : base(owner, AbilityId) { }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        hero.Auras.Apply(hero.Aura.FlightOfTheNavir, ctx.CurrentTime);
        if (hero.HasTalent<NavirsKeeper>())
        {
            hero.Auras.Apply(hero.Aura.NavirsKeeper, ctx.CurrentTime, setStacks: NavirsKeeper.StacksGranted);
        }
        TriggerCooldown(ctx);
    }
}
