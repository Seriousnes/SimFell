using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Tests;

/// <summary>A talent smoke run: the simulation's result, the hero it ran and how often each aura was gained.</summary>
public readonly record struct TalentSmokeRun(MonteCarloResult Result, Hero Hero, Dictionary<string, int> Gained);
