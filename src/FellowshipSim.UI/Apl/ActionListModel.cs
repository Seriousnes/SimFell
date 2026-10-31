using System.Text.RegularExpressions;
using FellowshipSim.Core.APL;

namespace FellowshipSim.UI.Apl;

/// <summary>
/// Names, titles, warnings and checks of the editor's action lists. The engine parses and writes the action-list text, so
/// the editor and the engine read the same grammar.
/// </summary>
public static partial class ActionListModel
{
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*\z")]
    private static partial Regex ListNamePattern();

    [GeneratedRegex(@"^(and|or|not)\z", RegexOptions.IgnoreCase)]
    private static partial Regex OperatorPattern();

    /// <summary>The distinct problems of the tokens of <paramref name="check"/>, in token order.</summary>
    public static IReadOnlyList<string> ProblemsOf(AplExpressionCheck? check) =>
        (check?.Tokens ?? []).Select(t => t.Problem).OfType<string>().Where(p => p.Length > 0).Distinct().ToList();

    public static string AbilityName(AplVocabulary vocabulary, string slug) => vocabulary.Abilities.FirstOrDefault(a => a.Slug == slug)?.Name ?? slug;

    public static AplTargetSelectionName? SelectionName(AplVocabulary vocabulary, AplTargetSelection selection) =>
        vocabulary.TargetSelections.FirstOrDefault(s => s.Selection == selection);

    /// <summary>run_action_list and call_action_list run another list.</summary>
    public static bool IsListAction(AplVocabulary vocabulary, EditorAction action) => vocabulary.ListActions.Contains(action.Ability);

    /// <summary>"run_action_list" -> "Run action list".</summary>
    public static string ListActionName(string name)
    {
        var words = name.Replace('_', ' ');
        return words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
    }

    public static string ActionTitle(AplVocabulary vocabulary, EditorAction action)
    {
        if (IsListAction(vocabulary, action)) return $"{ListActionName(action.Ability)}: {(string.IsNullOrEmpty(action.ActionList) ? "pick a list" : action.ActionList)}";
        var name = AbilityName(vocabulary, action.Ability);
        return name.Length > 0 ? name : "Pick an ability";
    }

    public static bool IsDefaultList(AplVocabulary vocabulary, ActionList list) => list.Name == vocabulary.DefaultList;

    public static string ListTitle(AplVocabulary vocabulary, ActionList list) => IsDefaultList(vocabulary, list) ? "Default list" : $"Action list {list.Name}";

    public static bool IsRunAction(AplVocabulary vocabulary, EditorAction action) => action.Ability == vocabulary.ListActions[0];

    /// <summary>
    /// True when the list casts on every decision: it has a repeatable ability with no condition or target selection, or a
    /// run_action_list with no condition naming a list that casts on every decision.
    /// </summary>
    private static bool AlwaysCasts(AplVocabulary vocabulary, IReadOnlyList<ActionList> lists, string? name, HashSet<string> seen)
    {
        var list = lists.FirstOrDefault(l => l.Name == name);
        if (list == null || name == null || !seen.Add(name)) return false;
        return list.Actions.Any(action =>
        {
            if (!string.IsNullOrWhiteSpace(action.Condition)) return false;
            if (IsRunAction(vocabulary, action)) return AlwaysCasts(vocabulary, lists, action.ActionList, seen);
            return vocabulary.RepeatableAbilities.Contains(action.Ability) && action.TargetSelection == null;
        });
    }

    /// <summary>The warning for a list a run_action_list names, when the list can cast nothing.</summary>
    public static string? RunWarning(AplVocabulary vocabulary, IReadOnlyList<ActionList> lists, string? name)
    {
        if (string.IsNullOrEmpty(name) || AlwaysCasts(vocabulary, lists, name, [])) return null;
        return $"When nothing in {name} is ready, the hero casts nothing and the actions after this one do not run. " +
               $"End {name} with an action that is always ready{Example(vocabulary)}.";
    }

    /// <summary>The warning for a list named by a run_action_list that can cast nothing.</summary>
    public static string? ListWarning(AplVocabulary vocabulary, IReadOnlyList<ActionList> lists, ActionList list)
    {
        var runBy = lists.Any(l => l.Actions.Any(a => IsRunAction(vocabulary, a) && a.ActionList == list.Name));
        if (!runBy || AlwaysCasts(vocabulary, lists, list.Name, [])) return null;
        return "A Run action list action names this list. When nothing here is ready, the hero casts nothing. " +
               $"End it with an action that is always ready{Example(vocabulary)}.";
    }

    private static string Example(AplVocabulary vocabulary) =>
        vocabulary.RepeatableAbilities.Count > 0 ? $", such as {AbilityName(vocabulary, vocabulary.RepeatableAbilities[0])} with no condition" : "";

    /// <summary>A list name is one identifier: letters, digits and '_', and none of the words 'and', 'or' and 'not'.</summary>
    public static string? ListNameProblem(IReadOnlyList<ActionList> lists, string name, string? current = null)
    {
        if (!ListNamePattern().IsMatch(name)) return "Name the list with letters, digits and '_', starting with a letter.";
        if (OperatorPattern().IsMatch(name)) return $"`{name}` is an operator.";
        if (name != current && lists.Any(l => l.Name == name)) return $"There is already a list named {name}.";
        return null;
    }

    /// <summary>Every action in list order, with its list's name.</summary>
    public static IReadOnlyList<AplDocumentAction> DocumentActions(IReadOnlyList<ActionList> lists) =>
        lists.SelectMany(list => list.Actions.Select(a => new AplDocumentAction(
            a.Ability, a.Condition, a.TargetSelection, a.TargetExpression, a.Comment, list.Name, a.Arguments, a.ActionList))).ToList();

    public static string ErrorText(AplVocabulary vocabulary, IReadOnlyList<ActionList> lists, IReadOnlyList<AplDocumentError> errors)
    {
        var labels = lists.SelectMany(list => list.Actions.Select((_, i) => $"{ListTitle(vocabulary, list)} action {i + 1}")).ToList();
        return string.Join(" ", errors.Select(e => $"{(e.Action >= 0 && e.Action < labels.Count ? labels[e.Action] : "undefined")}: {e.Message}"));
    }

    /// <summary>The default list first, then the other lists in order of their first action.</summary>
    public static IReadOnlyList<ActionList> ListsOf(AplVocabulary vocabulary, IReadOnlyList<AplDocumentAction> actions)
    {
        var names = new List<string> { vocabulary.DefaultList };
        var byName = new Dictionary<string, List<EditorAction>> { [vocabulary.DefaultList] = [] };
        foreach (var action in actions)
        {
            if (!byName.TryGetValue(action.List, out var list))
            {
                byName[action.List] = list = [];
                names.Add(action.List);
            }
            list.Add(EditorAction.From(action));
        }
        return names.Select(n => new ActionList(n, byName[n])).ToList();
    }
}
