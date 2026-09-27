namespace LithographERP.UnitTests;

public class CalculatorFunctionTests
{
    [Fact]
    public void Catalog_ContainsOnlyTheApprovedFunctions()
    {
        Assert.Equal(
            new[]
            {
                "ABS", "AND", "AVERAGE", "CEILING", "COUNT", "FLOOR", "IF", "MAX", "MIN", "MOD",
                "NOT", "OR", "POWER", "ROUND", "ROUNDDOWN", "ROUNDUP", "SQRT", "SUM",
            },
            CalculatorFunctionCatalog.Names.OrderBy(name => name, StringComparer.Ordinal));
        Assert.True(CalculatorFunctionCatalog.Names.Count < 30);
        Assert.Null(CalculatorFunctionCatalog.Find("VLOOKUP"));
    }

    [Fact]
    public void SumAverageMinMaxCount_CoverNormalAndInvalidArguments()
    {
        Assert.Equal(6m, Number("SUM(1, 2, 3)"));
        Assert.Equal(1m, Number("SUM(1)"));
        Assert.Equal(6m, Number("SUM(1, SUM(2, 3))"));
        Assert.Equal(2.5m, Number("AVERAGE(1, 2, 3, 4)"));
        Assert.Equal(5m, Number("AVERAGE(5)"));
        Assert.Equal(1m, Number("MIN(3, 1, 2)"));
        Assert.Equal(3m, Number("MAX(3, 1, 2)"));
        Assert.Equal(-2m, Number("MIN(-2, 4)"));
        Assert.Equal(3m, Number("COUNT(1, 2, 3)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("SUM(true)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("COUNT(true)"));
    }

    [Fact]
    public void Rounding_CoversBoundaries()
    {
        Assert.Equal(1.3m, Number("ROUND(1.25, 1)"));
        Assert.Equal(3m, Number("ROUND(2.5, 0)"));
        Assert.Equal(-2m, Number("ROUND(-1.5, 0)"));
        Assert.Equal(150m, Number("ROUND(148.5, -1)"));
        Assert.Equal(2m, Number("ROUNDUP(1.01, 0)"));
        Assert.Equal(-2m, Number("ROUNDUP(-1.01, 0)"));
        Assert.Equal(1.24m, Number("ROUNDUP(1.231, 2)"));
        Assert.Equal(2m, Number("ROUNDUP(2, 0)"));
        Assert.Equal(1m, Number("ROUNDDOWN(1.99, 0)"));
        Assert.Equal(-1m, Number("ROUNDDOWN(-1.99, 0)"));
        Assert.Equal(1.23m, Number("ROUNDDOWN(1.239, 2)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("ROUND(1.2, 1.5)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("ROUND(1, 29)"));
    }

    [Fact]
    public void CeilingFloorAbs_CoverBoundaries()
    {
        Assert.Equal(3m, Number("CEILING(2.1)"));
        Assert.Equal(2m, Number("CEILING(2)"));
        Assert.Equal(-2m, Number("CEILING(-2.5)"));
        Assert.Equal(6m, Number("CEILING(5.5, 2)"));
        Assert.Equal(1m, Number("FLOOR(1.9)"));
        Assert.Equal(2m, Number("FLOOR(2)"));
        Assert.Equal(-3m, Number("FLOOR(-2.5)"));
        Assert.Equal(4m, Number("FLOOR(5.5, 2)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("CEILING(1, 0)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("FLOOR(1, -1)"));
        Assert.Equal(3.5m, Number("ABS(-3.5)"));
        Assert.Equal(0m, Number("ABS(0)"));
    }

    [Fact]
    public void SqrtPowerMod_CoverNormalBoundariesAndInvalidArguments()
    {
        Assert.Equal(0m, Number("SQRT(0)"));
        Assert.Equal(1m, Number("SQRT(1)"));
        Assert.Equal(2m, Number("SQRT(4)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("SQRT(-1)"));
        Assert.Equal(8m, Number("POWER(2, 3)"));
        Assert.Equal(1m, Number("POWER(5, 0)"));
        Assert.Equal(0m, Number("POWER(0, 5)"));
        Assert.Equal(0.25m, Number("POWER(2, -2)"));
        Assert.Equal(-8m, Number("POWER(-2, 3)"));
        Assert.Equal(2m, Number("POWER(4, 0.5)"));
        Assert.Equal(3m, Number("POWER(9, 0.5)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("POWER(0, 0)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("POWER(-2, 0.5)"));
        Assert.Equal(FormulaErrorCodes.DivisionByZero, EvalError("POWER(0, -1)"));
        Assert.Equal(1m, Number("MOD(10, 3)"));
        Assert.Equal(-1m, Number("MOD(-10, 3)"));
        Assert.Equal(FormulaErrorCodes.DivisionByZero, EvalError("MOD(10, 0)"));
    }

    [Fact]
    public void IfAndOrNot_CoverBranchesAndInvalidArguments()
    {
        Assert.Equal(0.9m, Number("IF(quantity > 100, 0.9, 1)", Values(("quantity", 150m))));
        Assert.Equal(1m, Number("IF(quantity > 100, 0.9, 1)", Values(("quantity", 50m))));
        Assert.True(Boolean("AND(true, true)"));
        Assert.False(Boolean("AND(true, true, false)"));
        Assert.True(Boolean("OR(false, false, true)"));
        Assert.False(Boolean("OR(false, false)"));
        Assert.True(Boolean("NOT(false)"));
        Assert.False(Boolean("NOT(true)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("IF(1, 2, 3)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("AND(1, true)"));
        Assert.Equal(FormulaErrorCodes.InvalidArguments, EvalError("NOT(1)"));
        Assert.Equal(1m, Number("IF(true, 1, false)"));
    }

    [Fact]
    public void LargePower_ReturnsControlledOverflow() =>
        Assert.Equal(FormulaErrorCodes.NumericOverflow, EvalError("POWER(10, 40)"));

    private static decimal Number(string formula, IReadOnlyDictionary<string, CalculatorValue>? values = null)
    {
        var result = FormulaEvaluator.Evaluate(FormulaParser.Parse(formula), values ?? new Dictionary<string, CalculatorValue>());
        Assert.True(result.IsNumber, formula);
        return result.Number!.Value;
    }

    private static bool Boolean(string formula) =>
        FormulaEvaluator.Evaluate(FormulaParser.Parse(formula), new Dictionary<string, CalculatorValue>()).Boolean!.Value;

    private static string EvalError(string formula)
    {
        var exception = Assert.Throws<FormulaEvaluationException>(() => Number(formula));
        return exception.Code;
    }

    private static Dictionary<string, CalculatorValue> Values(params (string Key, decimal Value)[] fields) =>
        fields.ToDictionary(field => field.Key, field => CalculatorValue.FromNumber(field.Value));
}
