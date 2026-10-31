using System.Text.Json.Serialization;

namespace FellowshipSim.EngineServer;

/// <summary>Body of a failed engine server response.</summary>
public sealed record EngineError(
    [property: JsonPropertyName("error")] string Error);
