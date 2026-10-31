namespace FellowshipSim.Core.APL.Expressions;

public enum BinaryOperator
{
    And,
    Or,
    Add,
    Subtract,
    Multiply,
    /// <summary>Written '%', as in SimulationCraft. Division by zero gives 0.</summary>
    Divide,
    /// <summary>Written '%%', as in SimulationCraft. A zero divisor gives 0.</summary>
    Modulo,
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
}

public enum UnaryOperator
{
    /// <summary>Written '!': the negation, 1 where its operand is 0 and 0 elsewhere.</summary>
    Not,
    /// <summary>Written '-' before one operand: its value with the opposite sign.</summary>
    Minus,
}

public static class AplOperatorSymbols
{
    public const string AndSymbol = "&";
    public const string OrSymbol = "|";
    public const string AddSymbol = "+";
    public const string SubtractSymbol = "-";
    public const string MultiplySymbol = "*";
    public const string DivideSymbol = "%";
    public const string ModuloSymbol = "%%";
    public const string EqualSymbol = "=";
    public const string DoubleEqualSymbol = "==";
    public const string NotEqualSymbol = "!=";
    public const string LessSymbol = "<";
    public const string LessEqualSymbol = "<=";
    public const string GreaterSymbol = ">";
    public const string GreaterEqualSymbol = ">=";
    public const string PlusEqualSymbol = "+=";
    public const string NotSymbol = "!";
    public const string MinusSymbol = "-";
    public const string AndWord = "and";
    public const string OrWord = "or";
    public const string NotWord = "not";

    public static string Symbol(this BinaryOperator binaryOperator) => binaryOperator switch
    {
        BinaryOperator.And => AndSymbol,
        BinaryOperator.Or => OrSymbol,
        BinaryOperator.Add => AddSymbol,
        BinaryOperator.Subtract => SubtractSymbol,
        BinaryOperator.Multiply => MultiplySymbol,
        BinaryOperator.Divide => DivideSymbol,
        BinaryOperator.Modulo => ModuloSymbol,
        BinaryOperator.Equal => EqualSymbol,
        BinaryOperator.NotEqual => NotEqualSymbol,
        BinaryOperator.Less => LessSymbol,
        BinaryOperator.LessEqual => LessEqualSymbol,
        BinaryOperator.Greater => GreaterSymbol,
        BinaryOperator.GreaterEqual => GreaterEqualSymbol,
        _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null)
    };

    public static string Symbol(this UnaryOperator unaryOperator) => unaryOperator switch
    {
        UnaryOperator.Not => NotSymbol,
        UnaryOperator.Minus => MinusSymbol,
        _ => throw new ArgumentOutOfRangeException(nameof(unaryOperator), unaryOperator, null)
    };

    /// <summary>The operator's name and meaning in the condition reference.</summary>
    public static AplDoc Doc(this BinaryOperator binaryOperator) => binaryOperator switch
    {
        BinaryOperator.And => new("and", "1 if both sides are true. Otherwise 0. The word and has the same meaning."),
        BinaryOperator.Or => new("or", "1 if the left side, the right side or both sides are true. Otherwise 0. The word or has the same meaning."),
        BinaryOperator.Add => new("Add", "The left side plus the right side."),
        BinaryOperator.Subtract => new("Subtract", "The left side minus the right side."),
        BinaryOperator.Multiply => new("Multiply", "The left side multiplied by the right side."),
        BinaryOperator.Divide => new("Divide", "The left side divided by the right side. If the right side is 0, the result is 0."),
        BinaryOperator.Modulo => new("Remainder", "The remainder of the left side divided by the right side. If the right side is 0, the result is 0."),
        BinaryOperator.Equal => new("Equal", "1 if the difference between the two sides is less than 0.0001. Otherwise 0. == has the same meaning."),
        BinaryOperator.NotEqual => new("Not equal", "1 if the difference between the two sides is 0.0001 or more. Otherwise 0."),
        BinaryOperator.Less => new("Less than", "1 if the left side is less than the right side. Otherwise 0."),
        BinaryOperator.LessEqual => new("At most", "1 if the left side is less than or equal to the right side. Otherwise 0."),
        BinaryOperator.Greater => new("Greater than", "1 if the left side is greater than the right side. Otherwise 0."),
        BinaryOperator.GreaterEqual => new("At least", "1 if the left side is greater than or equal to the right side. Otherwise 0."),
        _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null)
    };

    /// <summary>The prefix operator's name and meaning in the condition reference.</summary>
    public static AplDoc Doc(this UnaryOperator unaryOperator) => unaryOperator switch
    {
        UnaryOperator.Not => new("not", "1 if the value is 0. Otherwise 0. The word not has the same meaning."),
        UnaryOperator.Minus => new("Negate", "The value with the opposite sign."),
        _ => throw new ArgumentOutOfRangeException(nameof(unaryOperator), unaryOperator, null)
    };
}
