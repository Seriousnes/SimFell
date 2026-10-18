using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 48: an Ice Comet cast (also Rising Talons with Icy Talons, and Cold Shower's comet) has an 18% chance to bring 1
/// more Ice Comet, else a 9% chance to bring 2; the extra comets cannot trigger it again (per the user).
/// Values: talent text; hero_data Ice Comet "MultiStrike" (0.18 / 0.09). The roll order and the 0.3s spacing follow the
/// old C++ sim. UNVERIFIED: no combat log has this talent.
/// </summary>
public class Avalanche : Talent
{
    public const int TalentId = 48;
    public const string TalentName = "Avalanche";
    public const double OneExtraChance = 0.18;
    public const double TwoExtraChance = 0.09;
    public const double CometSpacingSeconds = 0.3;
    public const string ProcKey = "avalanche";

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        var comet = hero.GetAbility<IceComet>();
        if (comet == null) return;
        // Ice Comet's event: Rising Talons and Cold Shower's comet raise it too (Rising Talons is built on Ice Comet).
        foreach (var cast in hero.Abilities.OfType<IceComet>()) cast.CometCast += (ctx, target) =>
        {
            int extra = ctx.Outcomes.RollProc(ProcKey, OneExtraChance, ctx) ? 1
                : ctx.Outcomes.RollProc(ProcKey, TwoExtraChance, ctx) ? 2 : 0;
            for (int i = 1; i <= extra; i++)
            {
                ctx.Schedule(SimTime.FromSeconds(CometSpacingSeconds * i), c =>
                {
                    comet.FreeCast(c, target, source: this, countsAsCast: false);
                }, "Avalanche comet");
            }
        };
    }
}
