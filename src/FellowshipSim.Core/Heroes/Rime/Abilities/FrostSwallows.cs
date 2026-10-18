using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1033: "Rime commands Frost Swallows to attack her target." (Cascading Blitz swallows.)</summary>
public class FrostSwallows : FrostSwallowAttack
{
    public const int AbilityId = 1033;

    public FrostSwallows(Rime owner) : base(owner, AbilityId) { }

    /// <summary>Swallows that pick their own targets among the enemies in combat.</summary>
    public void AttackEnemies(SimulationContext ctx, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var target = ctx.Outcomes.ChooseTarget(DamageLogId, ctx.ActiveEnemies.ToList(), ctx);
            if (target != null) Attack(ctx, target, 1);
        }
    }
}
