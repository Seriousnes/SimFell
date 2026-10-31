using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>What a replay command or step does. Each member's JSON text is its <see cref="ReplayCommandKindText.LogText"/>.</summary>
public enum ReplayCommandKind
{
    [JsonStringEnumMemberName("enemy_arise")] EnemyArise,
    [JsonStringEnumMemberName("enemy_demise")] EnemyDemise,
    [JsonStringEnumMemberName("enemy_health")] EnemyHealth,
    [JsonStringEnumMemberName("external_aura")] ExternalAura,
    [JsonStringEnumMemberName("cast")] Cast,
    [JsonStringEnumMemberName("begin_cast")] BeginCast,
    [JsonStringEnumMemberName("complete_cast")] CompleteCast,
    [JsonStringEnumMemberName("cancel_cast")] CancelCast,
    [JsonStringEnumMemberName("stop_channel")] StopChannel,
    [JsonStringEnumMemberName("player_death")] PlayerDeath,
    [JsonStringEnumMemberName("player_revive")] PlayerRevive,
    [JsonStringEnumMemberName("begin_channel")] BeginChannel,
    [JsonStringEnumMemberName("end_channel")] EndChannel,
}
