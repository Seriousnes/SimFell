using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1026 / effects 1389 + 2299: +20% Spirit for 20s. Usable during the global cooldown.</summary>
public class WintersBlessing : RimeAbility
{
    public const int AbilityId = 1026;

    public WintersBlessing(Rime owner) : base(owner, AbilityId)
    {
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.Auras.Apply(Rime.Aura.WintersBlessing, ctx.CurrentTime);
        TriggerCooldown(ctx);
    }
}
