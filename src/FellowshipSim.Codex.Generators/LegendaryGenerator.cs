using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Fellowship.SDK;
using Fellowship.SDK.Documents;
using Fellowship.SDK.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FellowshipSim.Codex.Generators;

/// <summary>
/// Writes the game data's half of each legendary class: for each <c>partial</c> class in
/// <c>FellowshipSim.Core.Customization.Legendaries</c> named after a legendary power (the power's name in Pascal case,
/// letters and digits only), a <c>partial</c> of the same class with the power's values. These are <c>PowerId</c>, an item
/// ID constant per item granting the power (<c>HeadItemId</c>, <c>ChestItemId</c>, <c>LegsItemId</c>: the item's slot), the
/// power's tuning values in a nested <c>Constants</c> under their <see cref="TuningNames"/> names, with the items' hero as
/// the record's hero, and the overrides of <c>Legendary</c>'s <c>LegendaryPowerId</c>, <c>Name</c>, <c>HeroName</c> and
/// <c>ItemIds</c>. The class's own part derives
/// from <c>Legendary</c> and has the power's behaviour. The game data is the AdditionalFiles <c>entities.jsonl</c> and
/// <c>settings.json</c>, read through FellowshipAnalyzer.Codex.SDK; <see cref="CreatureGenerator"/> reports missing game data.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class LegendaryGenerator : IIncrementalGenerator
{
    private const string EntitiesName = "entities.jsonl";
    private const string SettingsName = "settings.json";
    private const string Namespace = "FellowshipSim.Core.Customization.Legendaries";

    private static readonly DiagnosticDescriptor NoPower = new(
        "CODEX003", "A legendary class names no legendary power",
        "The partial class {0} names no legendary power of the game data", "Codex", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor SeveralHeroes = new(
        "CODEX005", "A legendary power's items name several heroes",
        "The items granting legendary power {0} name the heroes {1}", "Codex", DiagnosticSeverity.Error, true);

    /// <summary>An item granting a power: its ID, name, slot and hero.</summary>
    private sealed record Item(int Id, string Name, string Slot, string Hero);

    /// <summary>A legendary power: its ID, name, class name, the items granting it and its tuning keys.</summary>
    private sealed record Power(int Id, string Name, string ClassName, EquatableArray<Item> Items, EquatableArray<TuningEntry> Tunings);

    /// <summary>The game data's powers granted by an item, the tuning keys' first segments naming a hero, and its build date.</summary>
    private sealed record Export(EquatableArray<Power> Powers, EquatableArray<HeroSegment> Segments, string? BuildDate);

    /// <summary>
    /// A partial class in the legendaries namespace: its name, the class's constants and static readonly fields, and where
    /// it is declared.
    /// </summary>
    private sealed record Declared(string Name, EquatableArray<string> Fields, Location? Location)
    {
        public bool Equals(Declared? other) => other != null && Name == other.Name && Fields.Equals(other.Fields);

        public override int GetHashCode() => Name.GetHashCode();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var export = context.AdditionalTextsProvider
            .Where(f => Path.GetFileName(f.Path) is EntitiesName or SettingsName)
            .Collect()
            .Select((all, ct) => Read(all, ct));
        var declared = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword) && NamespaceOf(c) == Namespace,
                static (ctx, ct) => new Declared(((ClassDeclarationSyntax)ctx.Node).Identifier.ValueText,
                    new([.. ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) is INamedTypeSymbol type ? TuningNames.FieldsOf(type) : []]),
                    ctx.Node.GetLocation()))
            .Collect();
        context.RegisterSourceOutput(export.Combine(declared), (spc, pair) => Write(spc, pair.Left, pair.Right));
    }

    private static string? NamespaceOf(SyntaxNode node)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is BaseNamespaceDeclarationSyntax ns) return ns.Name.ToString();
        }
        return null;
    }

    private static Export Read(ImmutableArray<AdditionalText> files, CancellationToken ct)
    {
        var entities = files.FirstOrDefault(f => Path.GetFileName(f.Path) == EntitiesName)?.GetText(ct);
        var settings = files.FirstOrDefault(f => Path.GetFileName(f.Path) == SettingsName)?.GetText(ct);

        string? buildDate = null;
        if (settings != null)
        {
            var file = JsonSerializer.Deserialize<SettingsFile>(settings.ToString(), EntityDocuments.Options);
            buildDate = file?.BuildDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var powers = new List<LegendaryPowerDocument>();
        var items = new List<ItemDocument>();
        if (entities != null)
        {
            foreach (var line in entities.Lines)
            {
                ct.ThrowIfCancellationRequested();
                string text = line.ToString();
                // Only legendary power lines and the item lines granting a power are read in full.
                bool power = text.Contains("\"$type\":\"legendary-power\"");
                if (!power && !(text.Contains("\"$type\":\"item\"") && text.Contains("\"grants\":{\"type\":\"legendary-power\""))) continue;
                try
                {
                    switch (JsonSerializer.Deserialize<EntityDocument>(text, EntityDocuments.Options))
                    {
                        case LegendaryPowerDocument p:
                            powers.Add(p);
                            break;
                        case ItemDocument i when i.Grants is { Type: EntityType.LegendaryPower }:
                            items.Add(i);
                            break;
                    }
                }
                catch (JsonException)
                {
                }
            }
        }

        var result = new List<Power>();
        foreach (var p in powers.OrderBy(p => p.Id))
        {
            string name = (p.Name ?? "").Trim();
            var granting = items.Where(i => i.Grants!.Id == p.Id).OrderBy(i => i.Id)
                .Select(i => new Item(i.Id, (i.Name ?? "").Trim(), i.Slot ?? "", i.Hero?.ToString() ?? ""))
                .ToList();
            if (name.Length == 0 || granting.Count == 0) continue;
            result.Add(new Power(p.Id, name, PascalCase(name), new([.. granting]), new([.. TuningsOf(p)])));
        }
        return new Export(new([.. result]), HeroSegments.Of(entities, ct), buildDate);
    }

    /// <summary>The power's tuning keys, in the order of the game data.</summary>
    private static List<TuningEntry> TuningsOf(LegendaryPowerDocument power) =>
        TuningNames.Entries(power.Tuning, v => ValueOf(v) is { } value ? (value.Type, value.Literal, false) : null, SourceOf);

    /// <summary>The C# type and literal of a tuning value; null for a value of no constant type.</summary>
    private static (string Type, string Literal)? ValueOf(object? value)
    {
        if (value is not JsonElement element) return null;
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                string raw = element.GetRawText();
                if (element.TryGetInt32(out int whole) && raw.IndexOfAny(['.', 'e', 'E']) < 0) return ("int", whole.ToString(CultureInfo.InvariantCulture));
                return ("double", Number(element.GetDouble()));
            case JsonValueKind.True:
            case JsonValueKind.False:
                return ("bool", element.ValueKind == JsonValueKind.True ? "true" : "false");
            case JsonValueKind.String:
                return ("string", Literal(element.GetString() ?? ""));
            default:
                return null;
        }
    }

    /// <summary>The tuning value's owner, and its attribute and operation where it has them.</summary>
    private static string SourceOf(TuningReference tuning)
    {
        var parts = new List<string>();
        if (tuning.Owner is { } owner)
        {
            string type = owner.Type switch
            {
                EntityType.LegendaryPower => "legendary power",
                _ => owner.Type.ToString().ToLowerInvariant(),
            };
            parts.Add(owner.Name is { Length: > 0 } ownerName ? $"{ownerName} ({type} {owner.Id})" : $"{type} {owner.Id}");
        }
        if (tuning.Attribute is { Length: > 0 } attribute) parts.Add(tuning.Op is { Length: > 0 } op ? $"{attribute} {op}" : attribute);
        return string.Join(", ", parts);
    }

    private static void Write(SourceProductionContext context, Export export, ImmutableArray<Declared> declared)
    {
        var classes = declared.GroupBy(d => d.Name).ToDictionary(g => g.Key, g => g.First());
        var powers = export.Powers.ToDictionary(p => p.ClassName);
        foreach (var d in classes.Values.Where(d => !powers.ContainsKey(d.Name)).OrderBy(d => d.Name, StringComparer.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(NoPower, d.Location, d.Name));
        }

        foreach (var power in export.Powers.Where(p => classes.ContainsKey(p.ClassName)))
        {
            var heroes = power.Items.Select(i => i.Hero).Distinct().ToList();
            if (heroes.Count > 1) context.ReportDiagnostic(Diagnostic.Create(SeveralHeroes, classes[power.ClassName].Location, power.Id, string.Join(", ", heroes)));

            var source = new StringBuilder()
                .Append("// <auto-generated> by FellowshipSim.Codex.Generators from the game data")
                .Append(export.BuildDate != null ? $" of {export.BuildDate}" : "").Append(". </auto-generated>\n")
                .Append("#nullable enable\n")
                .Append("namespace ").Append(Namespace).Append(";\n\n")
                .Append("partial class ").Append(power.ClassName).Append("\n{\n")
                .Append("    /// <summary>").Append(Escape(power.Name)).Append(", legendary power ").Append(power.Id).Append(" of the game data.</summary>\n")
                .Append("    public const int PowerId = ").Append(power.Id).Append(";\n");
            foreach (var item in power.Items)
            {
                source.Append("\n    /// <summary>").Append(Escape(item.Name)).Append(", item ").Append(item.Id).Append(" of the game data.</summary>\n")
                    .Append("    public const int ").Append(PascalCase(item.Slot)).Append("ItemId = ").Append(item.Id).Append(";\n");
            }
            var shared = new List<(string Name, string First, string Second)>();
            var tunings = TuningNames.Of(power.Tunings, export.Segments, heroes, shared);
            var part = classes[power.ClassName];
            TuningNames.Report(context, part.Location, power.ClassName, $"legendary power {power.Id}", shared, part.Fields, tunings);
            if (tunings.Count > 0)
            {
                TuningNames.Write(source, $"Game data: the tuning values of legendary power {power.Id}.",
                    tunings.Select(t => (t, $"Game data: tuning <c>{Escape(t.Keys)}</c> of legendary power {power.Id}{(t.Source.Length > 0 ? "; " + Escape(t.Source) : "")}.")),
                    false, true);
            }
            source.Append("\n    public override int LegendaryPowerId => PowerId;\n")
                .Append("    public override string Name => ").Append(Literal(power.Name)).Append(";\n")
                .Append("    public override string HeroName => ").Append(Literal(heroes[0])).Append(";\n")
                .Append("    public override global::System.Collections.Generic.IReadOnlyList<int> ItemIds => [")
                .Append(string.Join(", ", power.Items.Select(i => $"{PascalCase(i.Slot)}ItemId"))).Append("];\n")
                .Append("}\n");
            context.AddSource($"{power.ClassName}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    /// <summary>The name's words, each starting upper case, with letters and digits only.</summary>
    private static string PascalCase(string name)
    {
        var result = new StringBuilder();
        bool start = true;
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(start ? char.ToUpperInvariant(c) : c);
                start = false;
            }
            // An apostrophe stays inside its word: Skandi's is Skandis.
            else if (c != '\'') start = true;
        }
        return result.ToString();
    }

    private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
