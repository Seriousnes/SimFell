using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 49: Ice Blitz, Flight of the Navir and Winter's Blessing cooldowns -0.3s per Winter Orb spent or overflowed.</summary>
public class WisdomOfTheNorth : Talent
{
    public const int TalentId = 49;
    public const string TalentName = "Wisdom of the North";
    public const double ReductionPerOrbSeconds = 0.3;

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        // "...for each Winters Orb you spend or that overflows your maximum."
        hero.OnResourceSpent += (resource, amount, now) =>
        {
            if (resource == Rime.WinterOrbsResource) ReduceCooldowns(hero, (int)amount, now);
        };
        hero.OnResourceOverflow += (resource, amount, now) =>
        {
            if (resource == Rime.WinterOrbsResource) ReduceCooldowns(hero, (int)amount, now);
        };
    }

    public static void ReduceCooldowns(Hero hero, int orbs, SimTime now)
    {
        var amount = SimTime.FromSeconds(ReductionPerOrbSeconds * orbs);
        hero.ReduceCooldown<IceBlitz>(amount, now);
        hero.ReduceCooldown<FlightOfTheNavir>(amount, now);
        hero.ReduceCooldown<WintersBlessing>(amount, now);
    }
}
