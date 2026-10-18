using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 657: each Freezing Torrent tick has a 7% chance to cast a free Ice Comet on the target (a plain Ice Comet even
/// with Icy Talons, and it can trigger Avalanche, per the user). hero_data: OnTargetPulsatingAoe.FreeCast.ProcChance 0.07.
/// UNVERIFIED: no combat log has this talent.
/// </summary>
public class ColdShower : Talent
{
    public const int TalentId = 657;
    public const string TalentName = "Cold Shower";
    public const double ProcChance = 0.07;
    public const string ProcKey = "cold_shower";

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        var rime = (Rime)hero;
        var comet = hero.GetAbility<IceComet>();
        if (comet == null) return;
        hero.OnChannelTick += (ability, target, _, ctx) =>
        {
            if (ability is FreezingTorrent && target != null && ctx.Outcomes.RollProc(ProcKey, ProcChance, ctx))
            {
                comet.FreeCast(ctx, target);
            }
        };
    }
}
