using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>
/// Ability 2227 ("Ice Comet", GA_Rime_Util_OnTargetPulsatingAoe): the free comets of Cold Shower and Avalanche. A plain Ice
/// Comet even with Icy Talons (per the user). UNVERIFIED: the damage ID these log under is not in any log we have.
/// </summary>
public class ProcIceComet : RimeAbility
{
    public const int AbilityId = 2227;
    public const string AbilityName = "Ice Comet";
    public const double Coefficient = 5.285;

    public ProcIceComet(Rime owner) : base(owner, AbilityId, AbilityName)
    {
        BaseCoefficient = Coefficient;
        AoeTargetThreshold = IceComet.TargetThreshold;
    }

    public void Strike(SimulationContext ctx, EnemyTarget target, double extraCritChance) => IceComet.Strike(this, ctx, target, extraCritChance);

    public override void Execute(SimulationContext ctx, EnemyTarget target) => Strike(ctx, target, 0.0);
}
