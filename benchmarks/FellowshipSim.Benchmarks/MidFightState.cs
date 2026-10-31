using FellowshipSim.Core.APL;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Benchmarks;

/// <summary>The hero, fight and action list <see cref="Fight.AtMidFight"/> returns.</summary>
public readonly record struct MidFightState(Hero Hero, SimulationContext Context, AplActionLists Actions);
