namespace FellowshipSim.Core.APL;

/// <summary>A form an action list is written in and its <see cref="AplDoc"/>, as a row of the condition reference.</summary>
public readonly record struct AplSyntaxDoc(string Syntax, AplDoc Doc);
