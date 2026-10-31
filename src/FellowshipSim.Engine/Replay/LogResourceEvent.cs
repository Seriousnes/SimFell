using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// The combat log event types that carry the player's resources. <see cref="Other"/> is every type without a member of its
/// own.
/// </summary>
[JsonConverter(typeof(LogResourceEventConverter))]
public enum LogResourceEvent
{
    Other,
    BeginCast,
    BeginChannel,
    Cast,
    Damage,
    EndChannel,
    Heal,
}
