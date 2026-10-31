using System.Text.Json.Serialization;

namespace FellowshipSim.Logs;

/// <summary>Variables of the combatant info query for one player in one fight.</summary>
public sealed record CombatantInfoVariables(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("fightIDs")] int[] FightIds,
    [property: JsonPropertyName("sourceID")] int SourceId,
    [property: JsonPropertyName("startTime")] double StartTime,
    [property: JsonPropertyName("endTime")] double EndTime);
