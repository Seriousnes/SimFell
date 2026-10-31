using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>The combat log event types that change an aura.</summary>
public enum LogAuraType
{
    [JsonStringEnumMemberName("applybuff")] ApplyBuff,
    [JsonStringEnumMemberName("applybuffstack")] ApplyBuffStack,
    [JsonStringEnumMemberName("applydebuff")] ApplyDebuff,
    [JsonStringEnumMemberName("applydebuffstack")] ApplyDebuffStack,
    [JsonStringEnumMemberName("refreshbuff")] RefreshBuff,
    [JsonStringEnumMemberName("refreshdebuff")] RefreshDebuff,
    [JsonStringEnumMemberName("removebuff")] RemoveBuff,
    [JsonStringEnumMemberName("removebuffstack")] RemoveBuffStack,
    [JsonStringEnumMemberName("removedebuff")] RemoveDebuff,
    [JsonStringEnumMemberName("removedebuffstack")] RemoveDebuffStack,
}
