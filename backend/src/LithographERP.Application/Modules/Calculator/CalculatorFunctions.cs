namespace LithographERP.Application.Modules.Calculator;

public sealed class CalculatorFunction
{
    public required string Name { get; init; }

    public required int MinArguments { get; init; }

    public required int MaxArguments { get; init; }

    public required Func<IReadOnlyList<CalculatorValue>, CalculatorValue> Invoke { get; init; }
}

public static class CalculatorFunctionCatalog
{
    public static readonly IReadOnlyList<CalculatorFunction> All =
    [
        Numeric("SUM", 1, int.MaxValue, args => CalculatorValue.FromNumber(args.Aggregate(0m, (sum, value) => CalculatorMath.Add(sum, Number(value))))),
        Numeric("AVERAGE", 1, int.MaxValue, args =>
        {
            decimal sum = 0m;
            foreach (var value in args)
            {
                sum = CalculatorMath.Add(sum, Number(value));
            }

            return CalculatorValue.FromNumber(CalculatorMath.Divide(sum, args.Count));
        }),
        Numeric("MIN", 1, int.MaxValue, args => CalculatorValue.FromNumber(args.Min(Number))),
        Numeric("MAX", 1, int.MaxValue, args => CalculatorValue.FromNumber(args.Max(Number))),
        Numeric("COUNT", 1, int.MaxValue, args =>
        {
            foreach (var value in args)
            {
                _ = Number(value);
            }

            return CalculatorValue.FromNumber(args.Count);
        }),
        Numeric("ROUND", 2, 2, args => CalculatorValue.FromNumber(CalculatorMath.Round(Number(args[0]), Digits(args[1])))),
        Numeric("ROUNDUP", 2, 2, args => CalculatorValue.FromNumber(CalculatorMath.RoundUp(Number(args[0]), Digits(args[1])))),
        Numeric("ROUNDDOWN", 2, 2, args => CalculatorValue.FromNumber(CalculatorMath.RoundDown(Number(args[0]), Digits(args[1])))),
        Numeric("CEILING", 1, 2, args => CalculatorValue.FromNumber(CalculatorMath.Ceiling(Number(args[0]), args.Count == 2 ? Number(args[1]) : 1m))),
        Numeric("FLOOR", 1, 2, args => CalculatorValue.FromNumber(CalculatorMath.Floor(Number(args[0]), args.Count == 2 ? Number(args[1]) : 1m))),
        Numeric("ABS", 1, 1, args => CalculatorValue.FromNumber(CalculatorMath.Abs(Number(args[0])))),
        Numeric("SQRT", 1, 1, args => CalculatorValue.FromNumber(CalculatorMath.Sqrt(Number(args[0])))),
        Numeric("POWER", 2, 2, args => CalculatorValue.FromNumber(CalculatorMath.Pow(Number(args[0]), Number(args[1])))),
        Numeric("MOD", 2, 2, args => CalculatorValue.FromNumber(CalculatorMath.Remainder(Number(args[0]), Number(args[1])))),
        Logical("IF", 3, 3),
        Logical("AND", 2, int.MaxValue),
        Logical("OR", 2, int.MaxValue),
        Logical("NOT", 1, 1),
    ];

    private static readonly Dictionary<string, CalculatorFunction> ByName =
        All.ToDictionary(function => function.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Names { get; } = All.Select(function => function.Name).ToArray();

    public static CalculatorFunction? Find(string name) =>
        ByName.TryGetValue(name, out var function) ? function : null;

    private static CalculatorFunction Numeric(string name, int min, int max, Func<IReadOnlyList<CalculatorValue>, CalculatorValue> invoke) =>
        new()
        {
            Name = name,
            MinArguments = min,
            MaxArguments = max,
            Invoke = invoke,
        };

    private static CalculatorFunction Logical(string name, int min, int max) =>
        new()
        {
            Name = name,
            MinArguments = min,
            MaxArguments = max,
            Invoke = _ => throw new InvalidOperationException("Logical functions are evaluated directly."),
        };

    private static decimal Number(CalculatorValue value)
    {
        if (!value.IsNumber || value.Number is not decimal number)
        {
            throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "A numeric value is required.");
        }

        return number;
    }

    private static int Digits(CalculatorValue value)
    {
        var number = Number(value);
        if (number != decimal.Truncate(number) || number is < -28 or > 28)
        {
            throw new FormulaEvaluationException(
                FormulaErrorCodes.InvalidArguments,
                "Rounding digits must be a whole number from -28 to 28.");
        }

        return (int)number;
    }
}
