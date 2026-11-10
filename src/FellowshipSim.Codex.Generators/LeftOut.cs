using Microsoft.CodeAnalysis;

namespace FellowshipSim.Codex.Generators;

/// <summary>
/// The constants a generator leaves out of one partial class. A name the class accepts with
/// <c>[CodexAccepted("&lt;name&gt;")]</c> has no warning; any other name has its generator's warning. An accepted name
/// the generator writes after all is CODEX038, so each acceptance stays current with the game data.
/// </summary>
internal sealed class LeftOut(SourceProductionContext context, string className, EquatableArray<string> accepted, Location? location)
{
    public const string AttributeName = "FellowshipSim.Core.Domain.CodexAcceptedAttribute";

    private static readonly DiagnosticDescriptor AcceptedNotLeftOut = new(
        "CODEX038", "A constant the class accepts as left out is in the game data's half",
        "The class {0} accepts {1} as left out, and the game data has {1} as a constant of the class; remove {1} from its [CodexAccepted]",
        "Codex", DiagnosticSeverity.Warning, true);

    private readonly HashSet<string> _accepted = new(accepted, StringComparer.Ordinal);
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);

    /// <summary>The names in each <c>[CodexAccepted]</c> on the class, in ordinal order.</summary>
    public static EquatableArray<string> AcceptedOf(INamedTypeSymbol type) => new([
        .. type.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == AttributeName)
            .SelectMany(a => a.ConstructorArguments)
            .SelectMany(arg => arg.Kind == TypedConstantKind.Array ? arg.Values : [arg])
            .Select(v => v.Value as string)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)]);

    /// <summary>The constant <paramref name="name"/> is left out: the warning, unless the class accepts it.</summary>
    public void Report(DiagnosticDescriptor descriptor, string name, params object?[] args)
    {
        if (_accepted.Contains(name)) _used.Add(name);
        else context.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }

    /// <summary>CODEX038 for each accepted name the generator did not leave out.</summary>
    public void Finish()
    {
        foreach (var name in _accepted.Where(n => !_used.Contains(n)).OrderBy(n => n, StringComparer.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(AcceptedNotLeftOut, location, className, name));
        }
    }
}
