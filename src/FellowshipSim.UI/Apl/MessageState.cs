using FellowshipSim.UI.Shared;

namespace FellowshipSim.UI.Apl;

/// <summary>Text and colour of a <see cref="Message"/> line.</summary>
public sealed record MessageState(string Text, MessageKind Kind)
{
    public static readonly MessageState None = new("", MessageKind.Plain);

    public static MessageState Error(string text) => new(text, MessageKind.Error);

    public static MessageState Ok(string text) => new(text, MessageKind.Ok);
}
