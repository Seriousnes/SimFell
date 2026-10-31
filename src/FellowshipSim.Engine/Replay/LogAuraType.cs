using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>The combat log event types that change an aura.</summary>
public enum LogAuraType
{
    [JsonStringEnumMemberName(LogAuraTypeText.ApplyBuff)] ApplyBuff,
    [JsonStringEnumMemberName(LogAuraTypeText.ApplyBuffStack)] ApplyBuffStack,
    [JsonStringEnumMemberName(LogAuraTypeText.ApplyDebuff)] ApplyDebuff,
    [JsonStringEnumMemberName(LogAuraTypeText.ApplyDebuffStack)] ApplyDebuffStack,
    [JsonStringEnumMemberName(LogAuraTypeText.RefreshBuff)] RefreshBuff,
    [JsonStringEnumMemberName(LogAuraTypeText.RefreshDebuff)] RefreshDebuff,
    [JsonStringEnumMemberName(LogAuraTypeText.RemoveBuff)] RemoveBuff,
    [JsonStringEnumMemberName(LogAuraTypeText.RemoveBuffStack)] RemoveBuffStack,
    [JsonStringEnumMemberName(LogAuraTypeText.RemoveDebuff)] RemoveDebuff,
    [JsonStringEnumMemberName(LogAuraTypeText.RemoveDebuffStack)] RemoveDebuffStack,
}
