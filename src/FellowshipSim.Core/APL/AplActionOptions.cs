using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.Core.APL;

/// <summary>The parts of an action line, each as written and with its <see cref="AplDoc"/>, for the condition reference.</summary>
public static class AplActionOptions
{
    public static IReadOnlyList<AplSyntaxDoc> All { get; } =
    [
        new AplSyntaxDoc($"{AplActionLine.ListPrefix}+=/<ability>", new("Action", $"Casts the ability. Each decision starts at the top of the {AplActionLine.DefaultList} list.")),
        new AplSyntaxDoc($"{AplActionLine.ListPrefix}.<list>+=/<ability>", new("Action in a list", "An action in the named list.")),
        new AplSyntaxDoc($"{AplActionLine.ConditionKey}=<expression>", new("Condition", "The action runs only if the expression is true (not 0).")),
        .. Enum.GetValues<AplTargetSelection>().Select(s =>
            new AplSyntaxDoc($"{AplActionLine.TargetIfKey}={AplTargetIf.Prefix(s)}:<expression>", new AplDoc(AplTargetIf.Label(s), AplTargetIf.Description(s)))),
        .. Enum.GetValues<AplListAction>().Select(a =>
            new AplSyntaxDoc($"{a.Name()},{AplActionLine.ListNameKey}=<list>", new AplDoc(a == AplListAction.Run ? "Run action list" : "Call action list", a.Description()))),
        new AplSyntaxDoc($"{AplVariable.ActionName},{AplVariable.NameKey}=<name>", new("Variable", $"Changes the variable {PropertyExpression.VariableScope}.<name>. It does not cast an ability.")),
        new AplSyntaxDoc($"{AplVariable.ValueKey}=<expression>", new("Variable value", "The value that the variable action uses.")),
        .. Enum.GetValues<AplVariableOp>().Select(op =>
            new AplSyntaxDoc($"{AplVariable.OpKey}={op.ToString().ToLowerInvariant()}", new AplDoc($"Variable op: {op.ToString().ToLowerInvariant()}", AplVariable.Description(op)))),
        new AplSyntaxDoc($"{AplVariable.DefaultKey}=<number>", new("Variable default", "The value of the variable at the start of each fight. 0 if the action has no default.")),
    ];
}
