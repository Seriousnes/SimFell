using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1018. Costs 2 Winter Orbs.</summary>
public class IceComet : RimeAbility
{
    public const int AbilityId = 1018;
    public const string AbilityName = "Ice Comet";
    public const double Coefficient = 5.285;
    public const int DefaultWinterOrbCost = 2;

    /// <summary>hero_data Ice Comet TargetCountThreshold 12: damage falls off as sqrt(12/N) beyond 12 targets.</summary>
    public const double TargetThreshold = 12;

    public IceComet(Rime owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Power)
    {
        BaseCoefficient = Coefficient;
        AoeTargetThreshold = TargetThreshold;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} is replaced by Rising Talons (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        Strike(this, ctx, target, Rime.ConsumeSpenderCritBuffs(ctx));
        Rime.TriggerAvalanche(ctx, target);
    }

    /// <summary>One comet impact on <paramref name="target"/> and all enemies around it.</summary>
    internal static void Strike(RimeAbility comet, SimulationContext ctx, EnemyTarget target, double extraCritChance)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target is { IsSleeping: false }) enemies.Add(target);
        comet.DealAoeDamage(ctx, enemies, extraCritChance: extraCritChance);
    }
}
