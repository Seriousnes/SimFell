namespace FellowshipSim.Engine;

/// <summary>A request the engine refused, with the reason (an unknown player, an action list outside the grammar).</summary>
public sealed class SimEngineException(string message, Exception? inner = null) : Exception(message, inner);
