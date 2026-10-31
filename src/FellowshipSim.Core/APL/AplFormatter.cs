using System.Globalization;
using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.Core.APL;

/// <summary>Spelling of the logical operators: <c>&amp;</c>, <c>|</c> and <c>!</c> in action-list text, <c>and</c>, <c>or</c> and <c>not</c> in the editor.</summary>
public enum AplOperatorStyle
{
    Symbols,
    Words,
}

/// <summary>
/// Writes an expression as action-list text: <c>buff.x.up &amp; (a | b)</c>. Parentheses written in the text
/// (<see cref="GroupExpression"/>) are kept, and others are added only where <see cref="AplParser"/> needs them to read the
/// expression back the same way. Binary operators have a space on each side.
/// </summary>
public static class AplFormatter
{
    // Binding strength, as in AplParser: '|' < '&' < comparison < sum < product < prefix and primary.
    private const int OrPrecedence = 1;
    private const int AndPrecedence = 2;
    private const int ComparisonPrecedence = 3;
    private const int SumPrecedence = 4;
    private const int ProductPrecedence = 5;
    private const int PrefixPrecedence = 6;

    private static int Precedence(BinaryOperator binaryOperator) => binaryOperator switch
    {
        BinaryOperator.Or => OrPrecedence,
        BinaryOperator.And => AndPrecedence,
        BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.Less or BinaryOperator.LessEqual
            or BinaryOperator.Greater or BinaryOperator.GreaterEqual => ComparisonPrecedence,
        BinaryOperator.Add or BinaryOperator.Subtract => SumPrecedence,
        BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo => ProductPrecedence,
        _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null)
    };

    private static int Precedence(IAplExpression expression) =>
        expression is BinaryOpExpression binary ? Precedence(binary.Operator) : PrefixPrecedence;

    private static string Symbol(BinaryOperator binaryOperator, AplOperatorStyle style) => style == AplOperatorStyle.Words
        ? binaryOperator switch
        {
            BinaryOperator.And => AplOperatorSymbols.AndWord,
            BinaryOperator.Or => AplOperatorSymbols.OrWord,
            _ => binaryOperator.Symbol()
        }
        : binaryOperator.Symbol();

    /// <summary>A prefix operator as written before its operand: "not " for <c>!</c> in words, else its symbol.</summary>
    public static string Prefix(UnaryOperator unaryOperator, AplOperatorStyle style) =>
        unaryOperator == UnaryOperator.Not && style == AplOperatorStyle.Words ? $"{AplOperatorSymbols.NotWord} " : unaryOperator.Symbol();

    public static string Format(IAplExpression expression, AplOperatorStyle style = AplOperatorStyle.Symbols) => expression switch
    {
        ConstantExpression constant => constant.Value.ToString(CultureInfo.InvariantCulture),
        PropertyExpression property => property.PropertyPath,
        GroupExpression group => $"({Format(group.Inner, style)})",
        // A prefix operator binds to one primary, so any operator expression under it needs parentheses.
        UnaryOpExpression unary => Prefix(unary.Operator, style) + Wrap(unary.Operand, Precedence(unary.Operand) < PrefixPrecedence, style),
        // Every level is left-associative: the left operand needs parentheses when it binds looser, the right one
        // when it binds looser or equally ("a - (b - c)").
        BinaryOpExpression binary =>
            $"{Wrap(binary.Left, Precedence(binary.Left) < Precedence(binary.Operator), style)} {Symbol(binary.Operator, style)} " +
            Wrap(binary.Right, Precedence(binary.Right) <= Precedence(binary.Operator), style),
        _ => throw new ArgumentOutOfRangeException(nameof(expression), expression.GetType().Name, null)
    };

    private static string Wrap(IAplExpression expression, bool parenthesize, AplOperatorStyle style) =>
        parenthesize ? $"({Format(expression, style)})" : Format(expression, style);
}
