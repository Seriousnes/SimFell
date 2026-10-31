using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 48: "Your Ice Comet ability has a 18% chance to cause 1 additional Ice Comet to strike the target, and a 9%
/// chance to cause 2" (hero_data Ice Comet MultiStrike 0.18 / 0.09). Rolled as a 27% chance of at least one extra comet,
/// then a 1-in-3 chance of a second: the same odds, one roll per extra comet. Rolls when a comet strikes (a cast comet,
/// Cold Shower's, or Rising Talons' first wave); the extra comets cannot trigger it again (per the user).
/// Combat logs (n7zxK8harZT2wjDB fight 9: 29 extra impacts over 90 casts): each extra comet strikes 5 server frames after
/// the impact before it.
/// </summary>
public class Avalanche : Talent
{
    public const int TalentId = 48;
    public const string TalentName = "Avalanche";
    public const double OneExtraChance = 0.18;
    public const double TwoExtraChance = 0.09;
    /// <summary>The chance of at least one extra comet, and of a second one once there is one.</summary>
    public const double AnyExtraChance = OneExtraChance + TwoExtraChance;
    public const double SecondExtraChance = TwoExtraChance / AnyExtraChance;
    public const double CometSpacingSeconds = 5.0 / 30.0;
    /// <summary>"a 9% chance to cause 2" additional Ice Comets (talent 48 text; hero_data Ice Comet MultiStrike 0.09).</summary>
    public const int MaxExtraComets = 2;
    public const string ProcKey = "avalanche";
    public const string SecondProcKey = "avalanche_second";

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        var comet = hero.GetAbility<IceComet>();
        if (comet == null) return;
        // Every comet that strikes: Rising Talons is built on Ice Comet, so its first wave counts too.
        foreach (var source in hero.Abilities.OfType<IceComet>()) source.Struck += (ctx, target) =>
        {
            if (!ctx.Outcomes.RollProc(ProcKey, AnyExtraChance, ctx)) return;
            var extra = ctx.Outcomes.RollProc(SecondProcKey, SecondExtraChance, ctx) ? MaxExtraComets : 1;
            for (var i = 1; i <= extra; i++)
            {
                comet.FreeCast(ctx, target, source: this, countsAsCast: false, impactDelaySeconds: CometSpacingSeconds * i);
            }
        };
    }
}
