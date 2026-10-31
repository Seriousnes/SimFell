using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Tests;

/// <summary>A hero in a fight, with the fight's simulation context.</summary>
public readonly record struct HeroInFight(Hero Hero, SimulationContext Ctx);
