using FellowshipSim.Core.APL;
using FellowshipSim.UI.Apl;

namespace FellowshipSim.Tests.Apl.Editor;

/// <summary>Titles, names, warnings and text of the editor's action lists.</summary>
public class ActionListModelTests
{
    private readonly AplVocabulary _vocabulary = EditorVocabulary.Create();

    private string Run => _vocabulary.ListActions[0];

    private string Call => _vocabulary.ListActions[1];

    [Fact]
    public void List_actions_have_their_words_as_names()
    {
        Assert.Equal("Run action list", ActionListModel.ListActionName(Run));
        Assert.Equal("Call action list", ActionListModel.ListActionName(Call));
    }

    [Fact]
    public void An_action_title_is_its_ability_name_or_its_list()
    {
        Assert.Equal("Frost Bolt", ActionListModel.ActionTitle(_vocabulary, EditorAction.New() with { Ability = "frost_bolt" }));
        Assert.Equal("not_here", ActionListModel.ActionTitle(_vocabulary, EditorAction.New() with { Ability = "not_here" }));
        Assert.Equal("Pick an ability", ActionListModel.ActionTitle(_vocabulary, EditorAction.New()));
        Assert.Equal("Run action list: aoe", ActionListModel.ActionTitle(_vocabulary, EditorAction.New() with { Ability = Run, ActionList = "aoe" }));
        Assert.Equal("Call action list: pick a list", ActionListModel.ActionTitle(_vocabulary, EditorAction.New() with { Ability = Call }));
    }

    [Theory]
    [InlineData("aoe", null)]
    [InlineData("_st2", null)]
    [InlineData("2aoe", "Name the list with letters, digits and '_', starting with a letter.")]
    [InlineData("aoe 2", "Name the list with letters, digits and '_', starting with a letter.")]
    [InlineData("aoe\n", "Name the list with letters, digits and '_', starting with a letter.")]
    [InlineData("OR", "`OR` is an operator.")]
    [InlineData("cleave", "There is already a list named cleave.")]
    public void List_names_are_one_identifier_and_not_taken(string name, string? problem)
    {
        IReadOnlyList<ActionList> lists = [new(_vocabulary.DefaultList, []), new("cleave", [])];

        Assert.Equal(problem, ActionListModel.ListNameProblem(lists, name));
        Assert.Null(ActionListModel.ListNameProblem(lists, "cleave", "cleave"));
    }

    [Fact]
    public void Lists_are_the_default_list_then_the_others_in_order_of_their_first_action()
    {
        var lists = ActionListModel.ListsOf(_vocabulary,
        [
            new AplDocumentAction("frost_bolt", List: "aoe"),
            new AplDocumentAction("ice_comet"),
            new AplDocumentAction("ice_comet", List: "aoe"),
        ]);

        Assert.Equal([_vocabulary.DefaultList, "aoe"], lists.Select(l => l.Name));
        Assert.Equal(["frost_bolt", "ice_comet"], lists[1].Actions.Select(a => a.Ability));
        Assert.Equal(lists.SelectMany(l => l.Actions).Count(), lists.SelectMany(l => l.Actions).Select(a => a.Key).Distinct().Count());

        var actions = ActionListModel.DocumentActions(lists);
        Assert.Equal([_vocabulary.DefaultList, "aoe", "aoe"], actions.Select(a => a.List));
        Assert.Equal(["ice_comet", "frost_bolt", "ice_comet"], actions.Select(a => a.Ability));
    }

    [Fact]
    public void Errors_are_labelled_by_their_list_and_place()
    {
        IReadOnlyList<ActionList> lists = [new(_vocabulary.DefaultList, [EditorAction.New()]), new("aoe", [EditorAction.New(), EditorAction.New()])];

        Assert.Equal("Default list action 1: Pick an ability. Action list aoe action 2: bad",
            ActionListModel.ErrorText(_vocabulary, lists, [new AplDocumentError(0, "ability", "Pick an ability."), new AplDocumentError(2, "condition", "bad")]));
    }

    [Fact]
    public void A_run_list_that_can_cast_nothing_is_warned_about_on_both_ends()
    {
        var run = EditorAction.New() with { Ability = Run, ActionList = "aoe" };
        IReadOnlyList<ActionList> lists = [new(_vocabulary.DefaultList, [run]), new("aoe", [EditorAction.New() with { Ability = "ice_comet" }])];

        Assert.StartsWith("When nothing in aoe is ready", ActionListModel.RunWarning(_vocabulary, lists, "aoe"));
        Assert.EndsWith("such as Frost Bolt with no condition.", ActionListModel.ListWarning(_vocabulary, lists, lists[1]));

        IReadOnlyList<ActionList> ready = [lists[0], new("aoe", [EditorAction.New() with { Ability = "frost_bolt" }])];
        Assert.Null(ActionListModel.RunWarning(_vocabulary, ready, "aoe"));
        Assert.Null(ActionListModel.ListWarning(_vocabulary, ready, ready[1]));

        IReadOnlyList<ActionList> conditioned = [lists[0], new("aoe", [EditorAction.New() with { Ability = "frost_bolt", Condition = "worb > 1" }])];
        Assert.NotNull(ActionListModel.RunWarning(_vocabulary, conditioned, "aoe"));
    }

    [Fact]
    public void Problems_are_each_token_problem_once()
    {
        var check = new AplExpressionCheck(
            [new AplTokenSpan(0, 1, AplTokenKind.Unknown, "x"), new AplTokenSpan(2, 1, AplTokenKind.Named), new AplTokenSpan(4, 1, AplTokenKind.Unknown, "x")],
            null, null);

        Assert.Equal(["x"], ActionListModel.ProblemsOf(check));
        Assert.Empty(ActionListModel.ProblemsOf(null));
    }
}
