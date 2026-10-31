using System.Text.Json.Serialization;

namespace FellowshipSim.Logs;

/// <summary>Variables of the queries for one fight of a report.</summary>
public sealed record FightVariables(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("fightIDs")] int[] FightIds);
