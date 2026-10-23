using FellowshipSim.Core.Import;

namespace FellowshipSim.Engine;

/// <summary>The body of the engine server's <c>POST sims</c>: the profile to simulate and the sim's fights.</summary>
public sealed record SimRequest(SimcProfile Profile, SimSettings Settings);
