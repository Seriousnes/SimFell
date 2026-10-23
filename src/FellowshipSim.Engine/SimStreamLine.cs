using System.Text.Json;
using System.Text.Json.Serialization;

namespace FellowshipSim.Engine;

/// <summary>
/// One line of the engine server's <c>POST sims</c> response (NDJSON, <see cref="ContentType"/>). Each line has one
/// member: <c>{"progress":{"completedFights":120,"fights":10000}}</c> any number of times, then one
/// <c>{"result":{...}}</c> or <c>{"error":"..."}</c> as the last line.
/// </summary>
public sealed record SimStreamLine(SimProgress? Progress = null, SimResult? Result = null, string? Error = null)
{
    public const string ContentType = "application/x-ndjson";

    /// <summary>Web defaults without null members.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
