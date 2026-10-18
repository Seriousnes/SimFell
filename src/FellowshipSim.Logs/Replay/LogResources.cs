using FellowshipSim.Core.Heroes.Rime;

namespace FellowshipSim.Logs.Replay;

/// <summary>
/// How combat logs report each hero's resources, so a replay can compare them with the engine before every cast. A
/// resource not listed here is not compared: add a hero's resources when adding the hero.
/// </summary>
public static class LogResources
{
    /// <summary>
    /// <paramref name="LogResourceType"/> is the log's resource type number, <paramref name="Scale"/> converts log values to
    /// engine units and <paramref name="Tolerance"/> is how far the engine may differ before the replay reports it.
    /// </summary>
    public sealed record LogResource(ResourceDef Resource, int LogResourceType, double Scale = 1.0, double Tolerance = 0.0);

    public static readonly LogResource[] All =
    [
        // Rime: Anima as resource type 2 and Winter Orbs as type 5, both in hundredths. Anima is compared within 0.99
        // (the log rounds it).
        new(Rime.Anima, 2, Scale: 0.01, Tolerance: 0.99),
        new(Rime.WinterOrbsResource, 5, Scale: 0.01),
    ];
}
