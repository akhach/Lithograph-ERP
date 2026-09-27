namespace LithographERP.Application.Modules.Calculator;

public static class FormulaParser
{
    public static FormulaExpression Parse(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
        {
            throw new FormulaParseException(CalculatorIssueCodes.InvalidFormulaSyntax, "Formula is empty.", 0);
        }

        if (formula.Length > 4000)
        {
            throw new FormulaParseException(CalculatorIssueCodes.InvalidFormulaSyntax, "Formula is too long.", 0);
        }

        var parser = new Parser(FormulaTokenizer.Tokenize(formula));
        var expression = parser.ParseComparison();
        parser.Expect(FormulaTokenKind.End, "Unexpected token.");
        return expression;
    }

    public static IReadOnlySet<string> FieldReferences(FormulaExpression expression)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        Collect(expression, fields);
        return fields;
    }

    private static void Collect(FormulaExpression expression, ISet<string> fields)
    {
        switch (expression)
        {
            case FieldReference field:
                fields.Add(field.Key);
                break;
            case UnaryExpression unary:
                Collect(unary.Operand, fields);
                break;
            case BinaryExpression binary:
                Collect(binary.Left, fields);
                Collect(binary.Right, fields);
                break;
            case FunctionCall call:
                foreach (var argument in call.Arguments)
                {
                    Collect(argument, fields);
                }

                break;
        }
    }

    private sealed class Parser(IReadOnlyList<FormulaToken> tokens)
    {
        private int _index;

        public FormulaExpression ParseComparison()
        {
            var left = ParseAdditive();
            if (!IsComparison(Current.Kind))
            {
                return left;
            }

            var op = Current;
            Advance();
            var right = ParseAdditive();
            return new BinaryExpression(op.Text, left, right);
        }

        private FormulaExpression ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (Current.Kind is FormulaTokenKind.Plus or FormulaTokenKind.Minus)
            {
                var op = Current.Text;
                Advance();
                var right = ParseMultiplicative();
                left = new BinaryExpression(op, left, right);
            }

            return left;
        }

        private FormulaExpression ParseMultiplicative()
        {
            var left = ParseUnary();
            while (Current.Kind is FormulaTokenKind.Star or FormulaTokenKind.Slash or FormulaTokenKind.Percent)
            {
                var op = Current.Text;
                Advance();
                var right = ParseUnary();
                left = new BinaryExpression(op, left, right);
            }

            return left;
        }

        private FormulaExpression ParseUnary()
        {
            if (Current.Kind == FormulaTokenKind.Minus)
            {
                Advance();
                return new UnaryExpression(ParseUnary());
            }

            if (Current.Kind == FormulaTokenKind.Plus)
            {
                Advance();
                return ParseUnary();
            }

            return ParsePrimary();
        }

        private FormulaExpression ParsePrimary()
        {
            if (Current.Kind == FormulaTokenKind.Number)
            {
                var number = Current.Number ?? throw Syntax("Invalid number.", Current.Position);
                Advance();
                return new NumberLiteral(number);
            }

            if (Current.Kind == FormulaTokenKind.Identifier)
            {
                var name = Current.Text;
                var position = Current.Position;
                Advance();
                if (Current.Kind == FormulaTokenKind.LeftParen)
                {
                    return ParseCall(name, position);
                }

                if (IsBoolean(name, out var boolean))
                {
                    return new BooleanLiteral(boolean);
                }

                return new FieldReference(name);
            }

            if (Current.Kind == FormulaTokenKind.LeftParen)
            {
                Advance();
                var inner = ParseComparison();
                Expect(FormulaTokenKind.RightParen, "Missing closing parenthesis.");
                return inner;
            }

            var message = Current.Kind == FormulaTokenKind.End ? "Unexpected token." : "Unexpected token.";
            throw Syntax(message, Current.Position);
        }

        private FunctionCall ParseCall(string name, int position)
        {
            Expect(FormulaTokenKind.LeftParen, "Missing closing parenthesis.");
            var arguments = new List<FormulaExpression>();
            if (Current.Kind != FormulaTokenKind.RightParen)
            {
                while (true)
                {
                    arguments.Add(ParseComparison());
                    if (Current.Kind != FormulaTokenKind.Comma)
                    {
                        break;
                    }

                    Advance();
                    if (Current.Kind is FormulaTokenKind.RightParen or FormulaTokenKind.End)
                    {
                        throw Syntax("Unexpected token.", Current.Position);
                    }
                }
            }

            Expect(FormulaTokenKind.RightParen, "Missing closing parenthesis.");
            var function = CalculatorFunctionCatalog.Find(name);
            if (function is null)
            {
                throw new FormulaParseException(
                    CalculatorIssueCodes.UnknownFunction,
                    "The formula uses an unsupported function.",
                    position);
            }

            if (arguments.Count < function.MinArguments || arguments.Count > function.MaxArguments)
            {
                throw new FormulaParseException(
                    CalculatorIssueCodes.InvalidFormulaArguments,
                    $"Function {function.Name} has an invalid number of arguments.",
                    position);
            }

            return new FunctionCall(function.Name, arguments);
        }

        public void Expect(FormulaTokenKind kind, string message)
        {
            if (Current.Kind != kind)
            {
                throw Syntax(message, Current.Position);
            }

            Advance();
        }

        private FormulaToken Current => tokens[_index];

        private void Advance()
        {
            if (_index < tokens.Count - 1)
            {
                _index++;
            }
        }

        private static bool IsComparison(FormulaTokenKind kind) =>
            kind is FormulaTokenKind.Equal
                or FormulaTokenKind.NotEqual
                or FormulaTokenKind.Greater
                or FormulaTokenKind.GreaterOrEqual
                or FormulaTokenKind.Less
                or FormulaTokenKind.LessOrEqual;

        private static bool IsBoolean(string name, out bool value)
        {
            if (name.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (name.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            value = false;
            return false;
        }

        private static FormulaParseException Syntax(string message, int position) =>
            new(CalculatorIssueCodes.InvalidFormulaSyntax, message, position);
    }
}
