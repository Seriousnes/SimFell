using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 628: Frost Swallow hits have a 12% chance to trigger a Bursting Ice pulse on the target.</summary>
public class BurstingSwallows : Talent
{
    public const int TalentId = 628;
    public const string TalentName = "Bursting Swallows";
    public const double ProcChance = 0.12;

    public override string Name => TalentName;
    public override int Id => TalentId;

    public const string ProcKey = "bursting_swallows";

    /// <summary>"Your Frost Swallow hits have a 12% chance to trigger a pulse of Bursting Ice on the target."</summary>
    public override void Bind(Hero hero)
    {
        var rime = (Rime)hero;
        var burstingIce = hero.GetAbility<BurstingIce>();
        if (burstingIce == null) return;
        rime.FrostSwallowHit += (ctx, target) =>
        {
            if (ctx.Outcomes.RollProc(ProcKey, ProcChance, ctx, LogIds.Effect(BurstingIce.PulseEffectId))) burstingIce.DealPulse(ctx, target);
        };
    }
}
