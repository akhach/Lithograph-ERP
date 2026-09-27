namespace LithographERP.UnitTests;

public class FormulaEvaluatorTests
{
    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("5 - 3", 2)]
    [InlineData("4 * 2", 8)]
    [InlineData("10 / 2", 5)]
    [InlineData("10 % 3", 1)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("-2 * 3", -6)]
    [InlineData("2 * -3", -6)]
    [InlineData("10 / 2 / 5", 1)]
    [InlineData("10 % 3 + 1", 2)]
    public void Evaluator_AppliesDecimalArithmeticAndPrecedence(string formula, int expected) =>
        Assert.Equal(expected, Number(formula));

    [Fact]
    public void Evaluator_AddsDecimalFractionsExactly() =>
        Assert.Equal(0.3m, Number("0.1 + 0.2"));

    [Fact]
    public void Evaluator_ResolvesFieldReferences()
    {
        var result = Number(
            "(width_mm * height_mm) / 1000000",
            new Dictionary<string, CalculatorValue>
            {
                ["width_mm"] = CalculatorValue.FromNumber(1000),
                ["height_mm"] = CalculatorValue.FromNumber(500),
            });
        Assert.Equal(0.5m, result);
    }

    [Theory]
    [InlineData("1 = 1", true)]
    [InlineData("1 != 2", true)]
    [InlineData("2 > 1", true)]
    [InlineData("2 >= 2", true)]
    [InlineData("1 < 2", true)]
    [InlineData("2 <= 1", false)]
    [InlineData("true = false", false)]
    [InlineData("true != false", true)]
    public void Evaluator_ComparesNumbersAndBooleans(string formula, bool expected) =>
        Assert.Equal(expected, Boolean(formula));

    [Fact]
    public void Evaluator_ShortCircuitsIfAndOr()
    {
        Assert.Equal(2m, Number("IF(false, 1 / 0, 2)"));
        Assert.False(Boolean("AND(false, 1 / 0 > 1)"));
        Assert.True(Boolean("OR(true, 1 / 0 > 1)"));
    }

    [Fact]
    public void Evaluator_ReportsDivisionByZeroWithoutCrashing()
    {
        var divide = Assert.Throws<FormulaEvaluationException>(() => Number("1 / 0"));
        Assert.Equal(FormulaErrorCodes.DivisionByZero, divide.Code);
        var remainder = Assert.Throws<FormulaEvaluationException>(() => Number("10 % 0"));
        Assert.Equal(FormulaErrorCodes.DivisionByZero, remainder.Code);
        Assert.Equal("Division by zero.", divide.Message);
    }

    [Fact]
    public void Evaluator_DoesNotTreatMissingValuesAsZero()
    {
        var exception = Assert.Throws<FormulaEvaluationException>(() => Number(
            "width_mm + 1",
            new Dictionary<string, CalculatorValue>()));
        Assert.Equal(FormulaErrorCodes.MissingValue, exception.Code);
        Assert.Contains("width_mm", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluator_RejectsMixedComparisonTypes() =>
        Assert.Equal(
            FormulaErrorCodes.InvalidArguments,
            Assert.Throws<FormulaEvaluationException>(() => Boolean("true > 1")).Code);

    private static decimal Number(string formula, IReadOnlyDictionary<string, CalculatorValue>? values = null)
    {
        var result = FormulaEvaluator.Evaluate(FormulaParser.Parse(formula), values ?? new Dictionary<string, CalculatorValue>());
        Assert.True(result.IsNumber);
        return result.Number!.Value;
    }

    private static bool Boolean(string formula, IReadOnlyDictionary<string, CalculatorValue>? values = null)
    {
        var result = FormulaEvaluator.Evaluate(FormulaParser.Parse(formula), values ?? new Dictionary<string, CalculatorValue>());
        Assert.True(result.IsBoolean);
        return result.Boolean!.Value;
    }
}
