using System.Text.Json;
using System.Text.Json.Serialization;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// Reads a resource snapshot's <c>event</c> string as its <see cref="LogResourceEvent"/>, and a type without a member as
/// <see cref="LogResourceEvent.Other"/>.
/// </summary>
public sealed class LogResourceEventConverter : JsonConverter<LogResourceEvent>
{
    public const string BeginCast = "begincast";
    public const string BeginChannel = "beginchannel";
    public const string Cast = "cast";
    public const string Damage = "damage";
    public const string EndChannel = "endchannel";
    public const string Heal = "heal";

    public override LogResourceEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            BeginCast => LogResourceEvent.BeginCast,
            BeginChannel => LogResourceEvent.BeginChannel,
            Cast => LogResourceEvent.Cast,
            Damage => LogResourceEvent.Damage,
            EndChannel => LogResourceEvent.EndChannel,
            Heal => LogResourceEvent.Heal,
            _ => LogResourceEvent.Other,
        };

    public override void Write(Utf8JsonWriter writer, LogResourceEvent value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            LogResourceEvent.BeginCast => BeginCast,
            LogResourceEvent.BeginChannel => BeginChannel,
            LogResourceEvent.Cast => Cast,
            LogResourceEvent.Damage => Damage,
            LogResourceEvent.EndChannel => EndChannel,
            LogResourceEvent.Heal => Heal,
            _ => throw new JsonException($"The resource event {value} has no combat log string."),
        });
}
