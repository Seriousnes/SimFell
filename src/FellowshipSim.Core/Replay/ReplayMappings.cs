using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Replay;

/// <summary>
/// Per-hero replay data that the engine itself does not carry (Replay/Mappings/{hero}.json): how to read the
/// log's resource values. Damage and aura IDs need no translation (the engine uses the combat log's IDs), and
/// auras from other actors take their effect from the datamine.
/// </summary>
public class ReplayMappings
{
    /// <summary>Log resource type -> engine resource and the factor from log units to engine units.</summary>
    [JsonPropertyName("resources")] public Dictionary<string, ResourceMapping> Resources { get; set; } = new();

    public static ReplayMappings ForHero(string hero)
    {
        string resource = $"FellowshipSim.Core.Replay.Mappings.{hero.ToLowerInvariant()}.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"No replay mappings for hero '{hero}' (expected embedded resource {resource}).");
        return JsonSerializer.Deserialize<ReplayMappings>(stream, new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() }
        })
            ?? throw new InvalidOperationException($"Could not read replay mappings for hero '{hero}'.");
    }
}

public class ResourceMapping
{
    [JsonPropertyName("resource")] public ResourceType Resource { get; set; }
    [JsonPropertyName("scale")] public double Scale { get; set; } = 1.0;
    [JsonPropertyName("tolerance")] public double Tolerance { get; set; } = 0.0;
}
