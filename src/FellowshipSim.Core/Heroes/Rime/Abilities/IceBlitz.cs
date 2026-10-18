using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1032 / effect 1397: +20% damage for 20s, extended 0.1s per Frost Swallow hit. Usable while casting.</summary>
public class IceBlitz : RimeAbility
{
    public const int AbilityId = 1032;
    public const double ExtensionPerSwallowSeconds = 0.1;

    public IceBlitz(Rime owner) : base(owner, AbilityId)
    {
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.Auras.Apply(Rime.Aura.IceBlitz, ctx.CurrentTime);
        TriggerCooldown(ctx);
    }
}
