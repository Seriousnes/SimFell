namespace FellowshipSim.UI.Apl;

/// <summary>The text with a completion written into it, and the caret after the completion.</summary>
public readonly record struct AcceptedCompletion(string Text, int Caret);
