using System.Runtime.CompilerServices;
using FellowshipSim.Core.APL;
using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.UI.Apl;

/// <summary>The completion list of the code fields: the parts a condition can read after the word at the caret.</summary>
public static class Completions
{
    /// <summary>What separates the parts of a path (scope, name, property).</summary>
    public const string PathSeparator = ".";

    private static readonly ConditionalWeakTable<AplVocabulary, CompletionNode> Trees = new();

    private static readonly string[] Operators = ["and", "or", "not"];

    /// <summary>
    /// Every path a condition can read, as a tree of its '.'-separated parts: scope, name, property; a resource, with or
    /// without its prefix; a named value; and the words 'and', 'or' and 'not'. Each node has the kind its text is coloured by.
    /// </summary>
    public static CompletionNode TreeOf(AplVocabulary vocabulary) => Trees.GetValue(vocabulary, Build);

    private static CompletionNode Build(AplVocabulary vocabulary)
    {
        var root = new CompletionNode(null);
        void Add(IEnumerable<string> parts, AplTokenKind kind, string detail = "")
        {
            var node = root;
            var list = parts.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (!node.Children.TryGetValue(list[i], out var child)) node.Children.Add(list[i], child = new CompletionNode(kind));
                node = child;
                if (i == list.Count - 1)
                {
                    node.Leaf = true;
                    node.Detail = detail;
                }
            }
        }

        var scopeNames = vocabulary.ScopeNames;
        foreach (var (scope, properties) in vocabulary.Scopes)
        {
            var kind = scope switch
            {
                PropertyExpression.BuffScope => AplTokenKind.Buff,
                PropertyExpression.DebuffScope => AplTokenKind.Debuff,
                PropertyExpression.CooldownScope => AplTokenKind.Cooldown,
                _ => AplTokenKind.Named,
            };
            foreach (var name in scopeNames[scope])
            {
                foreach (var property in properties) Add([scope, name.Slug, property], kind);
                if (root.Children.TryGetValue(scope, out var scopeNode) && scopeNode.Children.TryGetValue(name.Slug, out var nameNode))
                {
                    nameNode.Detail = name.Name == name.Slug ? "" : name.Name;
                }
            }
        }
        var prefix = vocabulary.ResourcePrefix.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var resource in vocabulary.Resources)
        {
            Add([resource.Slug], AplTokenKind.Resource, resource.Name);
            Add([.. prefix, resource.Slug], AplTokenKind.Resource, resource.Name);
        }
        foreach (var named in vocabulary.Named) Add(named.Split(PathSeparator), AplTokenKind.Named);
        foreach (var word in Operators) Add([word], AplTokenKind.Operator);
        return root;
    }

    /// <summary>
    /// The parts after <paramref name="word"/>'s last '.', starting with its text after that '.': those starting with it
    /// first, then those containing it.
    /// </summary>
    public static IReadOnlyList<Completion> For(AplVocabulary vocabulary, string word)
    {
        var parts = word.ToLowerInvariant().Split(PathSeparator);
        var partial = parts[^1];
        var node = TreeOf(vocabulary);
        foreach (var part in parts[..^1])
        {
            if (!node.Children.TryGetValue(part, out var child)) return [];
            node = child;
        }
        var entries = node.Children.Select(c => new Completion(c.Key, c.Value)).ToList();
        var found = entries.Where(e => e.Text.StartsWith(partial, StringComparison.Ordinal))
            .Concat(partial.Length > 0
                ? entries.Where(e => !e.Text.StartsWith(partial, StringComparison.Ordinal) && e.Text.Contains(partial, StringComparison.Ordinal))
                : [])
            .ToList();
        // A finished name has nothing to complete.
        if (found.Count == 1 && found[0].Text == partial && found[0].Node.Children.Count == 0) return [];
        return found;
    }
}
