namespace LithographERP.Application.Modules.Calculator;

public enum FormulaTokenKind
{
    Number,
    Identifier,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    LeftParen,
    RightParen,
    Comma,
    End,
}

public readonly record struct FormulaToken(FormulaTokenKind Kind, string Text, int Position, decimal? Number = null);

public abstract record FormulaExpression;

public sealed record NumberLiteral(decimal Value) : FormulaExpression;

public sealed record BooleanLiteral(bool Value) : FormulaExpression;

public sealed record FieldReference(string Key) : FormulaExpression;

public sealed record UnaryExpression(FormulaExpression Operand) : FormulaExpression;

public sealed record BinaryExpression(string Operator, FormulaExpression Left, FormulaExpression Right) : FormulaExpression;

public sealed record FunctionCall(string Name, IReadOnlyList<FormulaExpression> Arguments) : FormulaExpression;

public sealed class FormulaParseException(string code, string message, int position) : Exception(message)
{
    public string Code { get; } = code;

    public int Position { get; } = position;
}

public readonly struct CalculatorValue : IEquatable<CalculatorValue>
{
    private CalculatorValue(decimal? number, bool? boolean)
    {
        Number = number;
        Boolean = boolean;
    }

    public decimal? Number { get; }

    public bool? Boolean { get; }

    public bool IsNumber => Number is not null;

    public bool IsBoolean => Boolean is not null;

    public static CalculatorValue FromNumber(decimal value) => new(value, null);

    public static CalculatorValue FromBoolean(bool value) => new(null, value);

    public bool Equals(CalculatorValue other) => Number == other.Number && Boolean == other.Boolean;

    public override bool Equals(object? obj) => obj is CalculatorValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Number, Boolean);

    public static bool operator ==(CalculatorValue left, CalculatorValue right) => left.Equals(right);

    public static bool operator !=(CalculatorValue left, CalculatorValue right) => !left.Equals(right);
}

public sealed class FormulaEvaluationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public enum FormulaValueKind
{
    Number,
    Boolean,
}
