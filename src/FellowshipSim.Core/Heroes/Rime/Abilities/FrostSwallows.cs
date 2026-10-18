using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1033: "Rime commands Frost Swallows to attack her target." (Cascading Blitz swallows.)</summary>
public class FrostSwallows : FrostSwallowAttack
{
    public const int AbilityId = 1033;
    public const string AbilityName = "Frost Swallows";
    public const double Coefficient = 0.469;

    public FrostSwallows(Rime owner) : base(owner, AbilityId, AbilityName, Coefficient) { }

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
