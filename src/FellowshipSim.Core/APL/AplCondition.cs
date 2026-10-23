using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.Core.APL;

/// <summary>
/// One row of a condition in the editor's builder: its join to the row before it (<c>and</c> or <c>or</c>, null on the first
/// row of a group), its <c>not</c>, and either a value (<see cref="Path"/>, compared to <see cref="Value"/> when
/// <see cref="Comparison"/> is set) or a parenthesised group of rows (<see cref="Items"/>).
/// </summary>
public sealed record AplConditionItem(
    string? Join,
    bool Not,
    string? Path = null,
    string? Comparison = null,
    double? Value = null,
    IReadOnlyList<AplConditionItem>? Items = null);

/// <summary>
/// The rows of a condition, as the builder shows them. Rows joined by <c>and</c> and <c>or</c> without parentheses stay in
/// one group, so writing them back with their joins reads the same as the text.
/// </summary>
public static class AplCondition
{
    /// <summary>
    /// The rows of <paramref name="expression"/>, or null when it has a part the builder has no row for: arithmetic, or a
    /// comparison that is not a value against a number.
    /// </summary>
    public static IReadOnlyList<AplConditionItem>? Rows(IAplExpression expression)
    {
        var rows = new List<AplConditionItem>();
        return AddRows(expression, null, rows) ? rows : null;
    }

    private static bool AddRows(IAplExpression expression, string? join, List<AplConditionItem> rows)
    {
        if (expression is BinaryOpExpression { Operator: BinaryOperator.And or BinaryOperator.Or } logical)
        {
            return AddRows(logical.Left, join, rows)
                   && AddRows(logical.Right, logical.Operator == BinaryOperator.And ? "and" : "or", rows);
        }
        var row = Row(expression, join);
        if (row == null) return false;
        rows.Add(row);
        return true;
    }

    private static AplConditionItem? Row(IAplExpression expression, string? join)
    {
        switch (expression)
        {
            case PropertyExpression property:
                return new AplConditionItem(join, false, property.PropertyPath);
            case BinaryOpExpression { Left: PropertyExpression property } comparison when Number(comparison.Right) is { } value
                                                                                       && IsComparison(comparison.Operator):
                return new AplConditionItem(join, false, property.PropertyPath, comparison.Operator.Symbol(), value);
            // 'not' binds to one value, so a negated comparison is written in parentheses: not (worb >= 5).
            case UnaryOpExpression { Operator: UnaryOperator.Not, Operand: GroupExpression { Inner: BinaryOpExpression inner } }
                when IsComparison(inner.Operator):
                return Row(inner, join) is { } negated ? negated with { Not = true } : null;
            case UnaryOpExpression { Operator: UnaryOperator.Not } not:
                return Row(not.Operand, join) is { } operand ? operand with { Not = true } : null;
            case GroupExpression group:
                var items = new List<AplConditionItem>();
                return AddRows(group.Inner, null, items) ? new AplConditionItem(join, false, Items: items) : null;
            default:
                return null;
        }
    }

    private static bool IsComparison(BinaryOperator op) => op is BinaryOperator.Equal or BinaryOperator.NotEqual
        or BinaryOperator.Less or BinaryOperator.LessEqual or BinaryOperator.Greater or BinaryOperator.GreaterEqual;

    private static double? Number(IAplExpression expression) => expression switch
    {
        ConstantExpression constant => constant.Value,
        UnaryOpExpression { Operator: UnaryOperator.Negate, Operand: ConstantExpression constant } => -constant.Value,
        _ => null
    };
}
