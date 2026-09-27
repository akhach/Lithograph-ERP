namespace LithographERP.UnitTests;

public class FormulaParserTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("10.5")]
    [InlineData("width_mm")]
    [InlineData("true")]
    [InlineData("FALSE")]
    [InlineData("(1 + 2)")]
    public void Parser_AcceptsDocumentedTokens(string formula)
    {
        var expression = FormulaParser.Parse(formula);
        Assert.NotNull(expression);
    }

    [Fact]
    public void Parser_BuildsPrecedenceAndParentheses()
    {
        var multiplied = Assert.IsType<BinaryExpression>(FormulaParser.Parse("2 + 3 * 4"));
        Assert.Equal("+", multiplied.Operator);
        Assert.Equal(2m, Assert.IsType<NumberLiteral>(multiplied.Left).Value);
        var product = Assert.IsType<BinaryExpression>(multiplied.Right);
        Assert.Equal("*", product.Operator);

        var grouped = Assert.IsType<BinaryExpression>(FormulaParser.Parse("(2 + 3) * 4"));
        Assert.Equal("*", grouped.Operator);
        Assert.IsType<BinaryExpression>(grouped.Left);
    }

    [Fact]
    public void Parser_ReadsFunctionCallsAndFieldReferences()
    {
        var call = Assert.IsType<FunctionCall>(FormulaParser.Parse("sum(width_mm, 2)"));
        Assert.Equal("SUM", call.Name);
        Assert.Equal("width_mm", Assert.IsType<FieldReference>(call.Arguments[0]).Key);
        Assert.Equal(new[] { "width_mm" }, FormulaParser.FieldReferences(call));
    }

    [Theory]
    [InlineData("1 +")]
    [InlineData("SUM(")]
    [InlineData("width_mm **")]
    [InlineData("(1 + 2")]
    [InlineData("1 == 2")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parser_RejectsInvalidSyntax(string formula)
    {
        var exception = Assert.Throws<FormulaParseException>(() => FormulaParser.Parse(formula));
        Assert.Equal(CalculatorIssueCodes.InvalidFormulaSyntax, exception.Code);
    }

    [Theory]
    [InlineData("VLOOKUP(a, b, 1)")]
    [InlineData("fetch(1)")]
    [InlineData("FOOBAR(1)")]
    public void Parser_RejectsUnknownFunctions(string formula)
    {
        var exception = Assert.Throws<FormulaParseException>(() => FormulaParser.Parse(formula));
        Assert.Equal(CalculatorIssueCodes.UnknownFunction, exception.Code);
        Assert.Equal("The formula uses an unsupported function.", exception.Message);
    }

    [Theory]
    [InlineData("System.IO.File.Delete(1)")]
    [InlineData("process.start(1)")]
    [InlineData("SELECT * FROM users")]
    public void Parser_RejectsHostExecutionAttempts(string formula)
    {
        var exception = Assert.Throws<FormulaParseException>(() => FormulaParser.Parse(formula));
        Assert.Equal(CalculatorIssueCodes.InvalidFormulaSyntax, exception.Code);
    }

    [Fact]
    public void Parser_RejectsWrongArgumentCounts()
    {
        var exception = Assert.Throws<FormulaParseException>(() => FormulaParser.Parse("SUM()"));
        Assert.Equal(CalculatorIssueCodes.InvalidFormulaArguments, exception.Code);
        Assert.Throws<FormulaParseException>(() => FormulaParser.Parse("NOT(true, false)"));
        Assert.Throws<FormulaParseException>(() => FormulaParser.Parse("IF(true, 1)"));
    }

    [Fact]
    public void Tokenizer_ReadsNumbersIdentifiersAndOperators()
    {
        var tokens = FormulaTokenizer.Tokenize("10.5 + width_mm >= 1");
        Assert.Equal(
            new[]
            {
                FormulaTokenKind.Number,
                FormulaTokenKind.Plus,
                FormulaTokenKind.Identifier,
                FormulaTokenKind.GreaterOrEqual,
                FormulaTokenKind.Number,
                FormulaTokenKind.End,
            },
            tokens.Select(token => token.Kind));
        Assert.Equal(10.5m, tokens[0].Number);
    }
}
