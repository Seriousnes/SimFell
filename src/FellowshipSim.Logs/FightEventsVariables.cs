using System.Text.Json.Serialization;

namespace FellowshipSim.Logs;

/// <summary>Variables of the events query for one page of one fight.</summary>
public sealed record FightEventsVariables(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("fightIDs")] int[] FightIds,
    [property: JsonPropertyName("startTime")] double StartTime,
    [property: JsonPropertyName("endTime")] double EndTime);
