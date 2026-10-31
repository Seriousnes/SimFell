using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Domain;

/// <summary>A tick of a damage over time and its damage ID, as <see cref="Hero.QueueFlushingTick"/> holds it.</summary>
public readonly record struct FlushingTick(int DamageLogId, HastedTimer Timer);
