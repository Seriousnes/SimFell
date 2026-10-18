using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 192: each Anima generated during Ice Blitz conjures 1 Frost Swallow.</summary>
public class CascadingBlitz : Talent
{
    public const int TalentId = 192;
    public const string TalentName = "Cascading Blitz";
    public override string Name => TalentName;
    public override int Id => TalentId;
    /// <summary>"...you conjure 1 Frost Swallow to attack your target" (a replay follows the log if the player briefly had another target).</summary>
    public override void Bind(Hero hero)
    {
        var swallows = hero.GetAbility<FrostSwallows>();
        if (swallows == null) return;
        ((Rime)hero).AnimaGenerated += (ctx, amount, target) =>
        {
            if (!hero.Auras.HasAura(IceBlitz.Buff, ctx.CurrentTime)) return;
            for (int i = 0; i < (int)amount; i++)
            {
                var enemy = ctx.Outcomes.ChooseTarget(swallows.DamageLogId, ctx.ActiveEnemies.ToList(), ctx, preferred: target);
                if (enemy != null) swallows.Attack(ctx, enemy, 1);
            }
        };
    }
}
