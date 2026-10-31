using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FellowshipSim.Core.Generators;

/// <summary>
/// Writes the constructors <c>Catalog</c> uses: for each hero, talent, augmentation, glyph, weapon, relic passive and legendary class
/// with a public constructor without parameters, a switch from its ID to <c>new</c> of the class. The IDs are read from
/// the class's overrides of <c>Id</c>, <c>WeaponAbilityId</c>, <c>ItemIds</c> and <c>HeroName</c>,
/// which must be constants or lists of constants. An ID of 0 means unknown, and the class has no entry.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CatalogGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NotConstant = new(
        "SIMFELL001", "Catalog ID is not a constant",
        "{0}.{1} must be a constant or a list of constants for the catalog", "Catalog", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor SharedId = new(
        "SIMFELL002", "Two classes share a catalog ID",
        "{0} and {1} share the {2} ID {3}", "Catalog", DiagnosticSeverity.Error, true);

    private const string HintName = "Catalog.g.cs";
    private const char IdSeparator = ',';
    private const int HashMultiplier = 397;
    private const string IdProperty = "Id";
    private const string WeaponAbilityIdProperty = "WeaponAbilityId";
    private const string ItemIdsProperty = "ItemIds";
    private const string NameProperty = "Name";
    private const string HeroNameProperty = "HeroName";

    private enum Kind { Hero, Talent, Augmentation, Glyph, Weapon, RelicPassive, Legendary }

    private static readonly BaseClassKind[] Bases =
    [
        new("FellowshipSim.Core.Domain.Hero", Kind.Hero),
        new("FellowshipSim.Core.Domain.Talent", Kind.Talent),
        new("FellowshipSim.Core.Customization.Augmentations.Augmentation", Kind.Augmentation),
        new("FellowshipSim.Core.Customization.Glyphs.Glyph", Kind.Glyph),
        new("FellowshipSim.Core.Customization.Weapons.Weapon", Kind.Weapon),
        new("FellowshipSim.Core.Customization.RelicPassives.RelicPassive", Kind.RelicPassive),
        new("FellowshipSim.Core.Customization.Legendaries.Legendary", Kind.Legendary),
    ];

    /// <summary>One catalog class: its kind, full name, IDs, and a name (a hero's class name, a glyph's or legendary hero's name).</summary>
    /// <remarks>A class with value equality, so the generator's cache compares entries by value.</remarks>
    private sealed class Entry(Kind kind, string typeName, string shortName, string ids, string name, string? error, Location? errorLocation)
        : System.IEquatable<Entry>
    {
        public Kind Kind { get; } = kind;
        public string TypeName { get; } = typeName;
        public string ShortName { get; } = shortName;
        public string Ids { get; } = ids;
        public string Name { get; } = name;
        public string? Error { get; } = error;
        public Location? ErrorLocation { get; } = errorLocation;

        public int[] IdValues => Ids.Length == 0 ? [] : Ids.Split(IdSeparator).Select(int.Parse).ToArray();

        public bool Equals(Entry? other) =>
            other != null && Kind == other.Kind && TypeName == other.TypeName && ShortName == other.ShortName && Ids == other.Ids
            && Name == other.Name && Error == other.Error && Equals(ErrorLocation, other.ErrorLocation);

        public override bool Equals(object? obj) => Equals(obj as Entry);

        public override int GetHashCode() => TypeName.GetHashCode() ^ Ids.GetHashCode();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entries = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null } classDeclaration
                                    && !classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) && classDeclaration.TypeParameterList == null,
                static (ctx, ct) => EntryOf(ctx, ct))
            .Where(static e => e != null)
            .Collect();
        context.RegisterSourceOutput(entries, static (spc, list) => Emit(spc, list!));
    }

    private static Entry? EntryOf(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol type || type.IsAbstract) return null;
        if (!type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)) return null;
        Kind? kind = null;
        for (var ancestor = type.BaseType; ancestor != null && kind == null; ancestor = ancestor.BaseType)
        {
            string name = ancestor.ToDisplayString();
            foreach (var baseClass in Bases)
            {
                if (name == baseClass.Base) kind = baseClass.Kind;
            }
        }
        if (kind == null) return null;

        string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string ids = "", name2 = "";
        string? error = null;
        Location? location = null;
        bool Read(string property, out object[] values)
        {
            values = ConstantsOf(ctx.SemanticModel.Compilation, type, property, ct, out var at);
            if (values.Length > 0 || at == null) return true;
            error = property;
            location = at;
            return false;
        }

        switch (kind.Value)
        {
            case Kind.Hero:
                name2 = type.Name;
                break;
            case Kind.Talent:
            case Kind.Augmentation:
            case Kind.Glyph:
            case Kind.Weapon:
            case Kind.RelicPassive:
            case Kind.Legendary:
                string idProperty = kind switch
                {
                    Kind.Weapon => WeaponAbilityIdProperty,
                    Kind.Legendary => ItemIdsProperty,
                    _ => IdProperty,
                };
                if (Read(idProperty, out var idValues)) ids = string.Join(IdSeparator.ToString(), idValues.Select(v => (int)v));
                if (kind == Kind.Glyph && error == null && Read(NameProperty, out var names)) name2 = (string)names[0];
                if (kind == Kind.Legendary && error == null && Read(HeroNameProperty, out var heroes)) name2 = ((string)heroes[0]).ToLowerInvariant();
                break;
        }
        return new Entry(kind.Value, typeName, type.Name, ids, name2, error, location);
    }

    /// <summary>
    /// The constant values of <paramref name="property"/> on <paramref name="type"/> (its most derived override): one value,
    /// or each element of a collection or array. Empty with <paramref name="at"/> set when a value is not a constant.
    /// </summary>
    private static object[] ConstantsOf(Compilation compilation, INamedTypeSymbol type, string property, System.Threading.CancellationToken ct, out Location? at)
    {
        at = type.Locations.FirstOrDefault();
        for (var current = type; current != null; current = current.BaseType)
        {
            var symbol = current.GetMembers(property).OfType<IPropertySymbol>().FirstOrDefault(p => !p.IsAbstract);
            if (symbol == null) continue;
            if (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(ct) is not PropertyDeclarationSyntax syntax) return [];
            at = syntax.GetLocation();
            var body = syntax.ExpressionBody?.Expression
                       ?? syntax.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration))?.ExpressionBody?.Expression;
            if (body == null) return [];
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            IEnumerable<ExpressionSyntax> elements = body switch
            {
                CollectionExpressionSyntax collection => collection.Elements.Select(e => e is ExpressionElementSyntax expressionElement ? expressionElement.Expression : null!),
                ArrayCreationExpressionSyntax { Initializer: { } arrayInitializer } => arrayInitializer.Expressions,
                ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
                _ => [body],
            };
            var values = new List<object>();
            foreach (var element in elements)
            {
                if (element == null) return [];
                var constant = model.GetConstantValue(element, ct);
                if (!constant.HasValue || constant.Value is not (int or string)) return [];
                values.Add(constant.Value);
            }
            at = null;
            return values.ToArray();
        }
        return [];
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<Entry> found)
    {
        var entries = new List<Entry>();
        var seen = new HashSet<string>();
        foreach (var entry in found)
        {
            if (!seen.Add(entry.TypeName)) continue;
            if (entry.Error != null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(NotConstant, entry.ErrorLocation, entry.ShortName, entry.Error));
                continue;
            }
            entries.Add(entry);
        }

        // Classes by ID, in declaration order; an ID of 0 is unknown.
        var byKind = new Dictionary<Kind, List<IdentifiedEntry>>();
        foreach (var kind in new[] { Kind.Talent, Kind.Augmentation, Kind.Glyph, Kind.Weapon, Kind.RelicPassive })
        {
            var list = new List<IdentifiedEntry>();
            var owners = new Dictionary<int, Entry>();
            foreach (var entry in entries.Where(e => e.Kind == kind))
            {
                foreach (int id in entry.IdValues)
                {
                    if (id == 0) continue;
                    if (owners.TryGetValue(id, out var other))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(SharedId, null, entry.ShortName, other.ShortName, kind.ToString().ToLowerInvariant(), id));
                        continue;
                    }
                    owners[id] = entry;
                    list.Add(new IdentifiedEntry(id, entry));
                }
            }
            byKind[kind] = list;
        }
        var legendaries = new List<LegendaryEntry>();
        var legendaryKeys = new Dictionary<LegendaryKey, Entry>();
        foreach (var entry in entries.Where(e => e.Kind == Kind.Legendary))
        {
            foreach (int id in entry.IdValues)
            {
                if (legendaryKeys.TryGetValue(new LegendaryKey(entry.Name, id), out var other))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(SharedId, null, entry.ShortName, other.ShortName, "legendary item", id));
                    continue;
                }
                legendaryKeys[new LegendaryKey(entry.Name, id)] = entry;
                legendaries.Add(new LegendaryEntry(entry.Name, id, entry));
            }
        }

        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/> by FellowshipSim.Core.Generators.CatalogGenerator");
        source.AppendLine("#nullable enable");
        source.AppendLine("namespace FellowshipSim.Core;");
        source.AppendLine();
        source.AppendLine("partial class Catalog");
        source.AppendLine("{");

        var heroes = entries.Where(e => e.Kind == Kind.Hero).ToList();
        source.AppendLine("    private static HeroClass[] HeroClasses =>");
        source.AppendLine("    [");
        foreach (var heroEntry in heroes) source.AppendLine($"        new({Literal(heroEntry.Name)}, typeof({heroEntry.TypeName})),");
        source.AppendLine("    ];");
        source.AppendLine();
        source.AppendLine("    private static global::FellowshipSim.Core.Domain.Hero? NewHero(string name)");
        source.AppendLine("    {");
        foreach (var heroEntry in heroes)
        {
            source.AppendLine($"        if (string.Equals(name, {Literal(heroEntry.Name)}, global::System.StringComparison.OrdinalIgnoreCase)) return new {heroEntry.TypeName}();");
        }
        source.AppendLine("        return null;");
        source.AppendLine("    }");
        source.AppendLine();

        Switch(source, "global::FellowshipSim.Core.Domain.Talent", "NewTalent", "int id", byKind[Kind.Talent], "");
        Switch(source, "global::FellowshipSim.Core.Customization.Augmentations.Augmentation", "NewAugmentation", "int id, int rank", byKind[Kind.Augmentation], " { Rank = rank }");
        Switch(source, "global::FellowshipSim.Core.Customization.Glyphs.Glyph", "NewGlyph", "int id, int rank", byKind[Kind.Glyph], " { Rank = rank }");
        Switch(source, "global::FellowshipSim.Core.Customization.Weapons.Weapon", "NewWeapon", "int id", byKind[Kind.Weapon], "");
        Switch(source, "global::FellowshipSim.Core.Customization.RelicPassives.RelicPassive", "NewRelicPassive", "int id", byKind[Kind.RelicPassive], "");

        source.AppendLine("    private static global::FellowshipSim.Core.Customization.Legendaries.Legendary? NewLegendary(string hero, int itemId) => hero switch");
        source.AppendLine("    {");
        foreach (string hero in legendaries.Select(l => l.Hero).Distinct())
        {
            source.AppendLine($"        {Literal(hero)} => itemId switch");
            source.AppendLine("        {");
            foreach (var legendary in legendaries.Where(l => l.Hero == hero)) source.AppendLine($"            {legendary.ItemId} => new {legendary.Entry.TypeName}(),");
            source.AppendLine("            _ => null,");
            source.AppendLine("        },");
        }
        source.AppendLine("        _ => null,");
        source.AppendLine("    };");
        source.AppendLine();

        source.AppendLine("    private static int GlyphIdOf(string name)");
        source.AppendLine("    {");
        foreach (var glyph in byKind[Kind.Glyph])
        {
            source.AppendLine($"        if (string.Equals(name, {Literal(glyph.Entry.Name)}, global::System.StringComparison.OrdinalIgnoreCase)) return {glyph.Id};");
        }
        source.AppendLine("        return 0;");
        source.AppendLine("    }");
        source.AppendLine();

        Types(source, "AugmentationClasses", Distinct(byKind[Kind.Augmentation]));
        Types(source, "GlyphClasses", Distinct(byKind[Kind.Glyph]));
        Types(source, "WeaponClasses", Distinct(byKind[Kind.Weapon]));
        Types(source, "RelicPassiveClasses", Distinct(byKind[Kind.RelicPassive]));
        Instances(source, "NewAugmentations", "global::FellowshipSim.Core.Customization.Augmentations.Augmentation", Distinct(byKind[Kind.Augmentation]));
        Instances(source, "NewGlyphs", "global::FellowshipSim.Core.Customization.Glyphs.Glyph", Distinct(byKind[Kind.Glyph]));
        Instances(source, "NewWeapons", "global::FellowshipSim.Core.Customization.Weapons.Weapon", Distinct(byKind[Kind.Weapon]));
        Instances(source, "NewRelicPassives", "global::FellowshipSim.Core.Customization.RelicPassives.RelicPassive", Distinct(byKind[Kind.RelicPassive]));
        source.AppendLine("    private static LegendaryClass[] LegendaryClasses =>");
        source.AppendLine("    [");
        foreach (var legendary in legendaries) source.AppendLine($"        new({Literal(legendary.Hero)}, typeof({legendary.Entry.TypeName})),");
        source.AppendLine("    ];");
        source.AppendLine("}");

        spc.AddSource(HintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void Switch(StringBuilder source, string returnType, string method, string parameters, List<IdentifiedEntry> classes, string initializer)
    {
        source.AppendLine($"    private static {returnType}? {method}({parameters}) => id switch");
        source.AppendLine("    {");
        foreach (var identified in classes) source.AppendLine($"        {identified.Id} => new {identified.Entry.TypeName}(){initializer},");
        source.AppendLine("        _ => null,");
        source.AppendLine("    };");
        source.AppendLine();
    }

    private static void Types(StringBuilder source, string field, IEnumerable<Entry> classes)
    {
        source.AppendLine($"    private static global::System.Type[] {field} =>");
        source.AppendLine("    [");
        foreach (var entry in classes) source.AppendLine($"        typeof({entry.TypeName}),");
        source.AppendLine("    ];");
        source.AppendLine();
    }

    private static void Instances(StringBuilder source, string method, string baseType, IEnumerable<Entry> classes)
    {
        source.AppendLine($"    private static {baseType}[] {method}() =>");
        source.AppendLine("    [");
        foreach (var entry in classes) source.AppendLine($"        new {entry.TypeName}(),");
        source.AppendLine("    ];");
        source.AppendLine();
    }

    private static List<Entry> Distinct(List<IdentifiedEntry> classes)
    {
        var seen = new HashSet<string>();
        return classes.Where(c => seen.Add(c.Entry.TypeName)).Select(c => c.Entry).ToList();
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    /// <summary>A catalog base class's full name and the kind of class derived from it.</summary>
    private readonly struct BaseClassKind
    {
        public BaseClassKind(string @base, Kind kind)
        {
            Base = @base;
            Kind = kind;
        }

        public string Base { get; }
        public Kind Kind { get; }
    }

    /// <summary>A catalog class and one of its IDs.</summary>
    private readonly struct IdentifiedEntry
    {
        public IdentifiedEntry(int id, Entry entry)
        {
            Id = id;
            Entry = entry;
        }

        public int Id { get; }
        public Entry Entry { get; }
    }

    /// <summary>A legendary class, the hero it belongs to and one of its item IDs.</summary>
    private readonly struct LegendaryEntry
    {
        public LegendaryEntry(string hero, int itemId, Entry entry)
        {
            Hero = hero;
            ItemId = itemId;
            Entry = entry;
        }

        public string Hero { get; }
        public int ItemId { get; }
        public Entry Entry { get; }
    }

    /// <summary>A hero and an item ID: the key a legendary class is found by.</summary>
    private readonly struct LegendaryKey : System.IEquatable<LegendaryKey>
    {
        public LegendaryKey(string hero, int itemId)
        {
            Hero = hero;
            ItemId = itemId;
        }

        public string Hero { get; }
        public int ItemId { get; }

        public bool Equals(LegendaryKey other) => Hero == other.Hero && ItemId == other.ItemId;

        public override bool Equals(object? obj) => obj is LegendaryKey other && Equals(other);

        public override int GetHashCode() => unchecked(Hero.GetHashCode() * HashMultiplier + ItemId);
    }
}
