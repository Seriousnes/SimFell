using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 49: Ice Blitz, Flight of the Navir and Winter's Blessing cooldowns -0.3s per Winter Orb spent or overflowed.</summary>
public class WisdomOfTheNorth : Talent
{
    public const double ReductionPerOrbSeconds = 0.3;
    public override string Name => "Wisdom of the North";
    public override int Id => 49;
    public override void Bind(Hero hero) { }

    public static void ReduceCooldowns(Hero hero, int orbs, SimTime now)
    {
        var amount = SimTime.FromSeconds(ReductionPerOrbSeconds * orbs);
        hero.ReduceCooldown<IceBlitz>(amount, now);
        hero.ReduceCooldown<FlightOfTheNavir>(amount, now);
        hero.ReduceCooldown<WintersBlessing>(amount, now);
    }
}
