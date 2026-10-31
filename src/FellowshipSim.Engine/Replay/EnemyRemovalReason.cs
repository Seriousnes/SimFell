using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>Why an enemy leaves a replay.</summary>
public enum EnemyRemovalReason
{
    [JsonStringEnumMemberName("death")] Death,
    [JsonStringEnumMemberName("last_seen")] LastSeen,
}
