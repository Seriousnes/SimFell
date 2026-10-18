using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1029. Generates 3 Anima.</summary>
public class FrostBolt : RimeAbility
{
    public const int AbilityId = 1029;
    public const string AbilityName = "Frost Bolt";
    public const double Coefficient = 2.017;
    public const double CastTimeSeconds = 1.5;
    public const double AnimaGain = 3.0;

    public FrostBolt(Rime owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Basic)
    {
        BaseCoefficient = Coefficient;
        CastTime = SimTime.FromSeconds(CastTimeSeconds);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.AddAnima(AnimaGain, ctx, target);
        // Talent 193 Burstbolter: "When your Frost Bolt deals damage to an enemy, it triggers a single pulse of Bursting Ice."
        // (on the bolt's hit, after its travel: yMAZVJL9hTpFjqbC fight 7 @ 57.569 cast, 57.906 hit and pulse)
        var pulse = Rime.HasTalent<Burstbolter>() ? Rime.GetAbility<BurstingIce>() : null;
        DealDamage(ctx, target, onLanded: pulse == null ? null : (c, dealt) => { if (dealt > 0.0) pulse.DealPulse(c, target); });
    }
}
