using System.Text.Json.Serialization;

namespace FellowshipSim.Logs;

/// <summary>Body of a FellowshipLogs GraphQL request.</summary>
public sealed record GraphQlRequest(
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("variables")] object? Variables);
