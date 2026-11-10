namespace FellowshipSim.Core.Domain;

/// <summary>
/// The game data constants FellowshipSim.Codex.Generators leaves out of this class's generated half, each with no warning:
/// a name the class or a base class already has (CODEX009 and the like), or a name with several tuning values (CODEX008
/// and the like). A name the generator writes after all is CODEX038.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CodexAcceptedAttribute(params string[] names) : Attribute
{
    /// <summary>The constants' names.</summary>
    public IReadOnlyList<string> Names { get; } = names;
}
