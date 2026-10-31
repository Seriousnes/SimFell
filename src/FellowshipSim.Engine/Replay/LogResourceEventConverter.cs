using System.Text.Json;
using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// Reads a resource snapshot's <c>event</c> string as its <see cref="LogResourceEvent"/>, and a type without a member as
/// <see cref="LogResourceEvent.Other"/>.
/// </summary>
public sealed class LogResourceEventConverter : JsonConverter<LogResourceEvent>
{
    public override LogResourceEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            LogEventTypes.BeginCast => LogResourceEvent.BeginCast,
            LogEventTypes.BeginChannel => LogResourceEvent.BeginChannel,
            LogEventTypes.Cast => LogResourceEvent.Cast,
            LogEventTypes.Damage => LogResourceEvent.Damage,
            LogEventTypes.EndChannel => LogResourceEvent.EndChannel,
            LogEventTypes.Heal => LogResourceEvent.Heal,
            _ => LogResourceEvent.Other,
        };

    public override void Write(Utf8JsonWriter writer, LogResourceEvent value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            LogResourceEvent.BeginCast => LogEventTypes.BeginCast,
            LogResourceEvent.BeginChannel => LogEventTypes.BeginChannel,
            LogResourceEvent.Cast => LogEventTypes.Cast,
            LogResourceEvent.Damage => LogEventTypes.Damage,
            LogResourceEvent.EndChannel => LogEventTypes.EndChannel,
            LogResourceEvent.Heal => LogEventTypes.Heal,
            _ => throw new JsonException($"The resource event {value} has no combat log string."),
        });
}
