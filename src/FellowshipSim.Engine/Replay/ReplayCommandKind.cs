using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>What a replay command or step does. Each member's JSON text is its <see cref="ReplayCommandKindText.LogText"/>.</summary>
public enum ReplayCommandKind
{
    [JsonStringEnumMemberName(ReplayCommandKindText.EnemyArise)] EnemyArise,
    [JsonStringEnumMemberName(ReplayCommandKindText.EnemyDemise)] EnemyDemise,
    [JsonStringEnumMemberName(ReplayCommandKindText.EnemyHealth)] EnemyHealth,
    [JsonStringEnumMemberName(ReplayCommandKindText.ExternalAura)] ExternalAura,
    [JsonStringEnumMemberName(ReplayCommandKindText.Cast)] Cast,
    [JsonStringEnumMemberName(ReplayCommandKindText.BeginCast)] BeginCast,
    [JsonStringEnumMemberName(ReplayCommandKindText.CompleteCast)] CompleteCast,
    [JsonStringEnumMemberName(ReplayCommandKindText.CancelCast)] CancelCast,
    [JsonStringEnumMemberName(ReplayCommandKindText.StopChannel)] StopChannel,
    [JsonStringEnumMemberName(ReplayCommandKindText.PlayerDeath)] PlayerDeath,
    [JsonStringEnumMemberName(ReplayCommandKindText.PlayerRevive)] PlayerRevive,
    [JsonStringEnumMemberName(ReplayCommandKindText.BeginChannel)] BeginChannel,
    [JsonStringEnumMemberName(ReplayCommandKindText.EndChannel)] EndChannel,
}
