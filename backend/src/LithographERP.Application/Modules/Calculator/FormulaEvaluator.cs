namespace LithographERP.Application.Modules.Calculator;

public static class FormulaEvaluator
{
    public static CalculatorValue Evaluate(
        FormulaExpression expression,
        IReadOnlyDictionary<string, CalculatorValue> values) =>
        Evaluate(expression, key =>
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new FormulaEvaluationException(
                FormulaErrorCodes.MissingValue,
                $"Required value for '{key}' is missing.");
        });

    public static CalculatorValue Evaluate(FormulaExpression expression, Func<string, CalculatorValue> resolve)
    {
        try
        {
            return EvaluateCore(expression, resolve);
        }
        catch (OverflowException)
        {
            throw new FormulaEvaluationException(FormulaErrorCodes.NumericOverflow, "The calculation is too large.");
        }
    }

    private static CalculatorValue EvaluateCore(FormulaExpression expression, Func<string, CalculatorValue> resolve) =>
        expression switch
        {
            NumberLiteral number => CalculatorValue.FromNumber(number.Value),
            BooleanLiteral boolean => CalculatorValue.FromBoolean(boolean.Value),
            FieldReference field => resolve(field.Key),
            UnaryExpression unary => CalculatorValue.FromNumber(CalculatorMath.Subtract(0m, RequireNumber(EvaluateCore(unary.Operand, resolve)))),
            BinaryExpression binary => EvaluateBinary(binary, resolve),
            FunctionCall call => EvaluateCall(call, resolve),
            _ => throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "The formula could not be evaluated."),
        };

    private static CalculatorValue EvaluateBinary(BinaryExpression binary, Func<string, CalculatorValue> resolve)
    {
        var left = EvaluateCore(binary.Left, resolve);
        var right = EvaluateCore(binary.Right, resolve);
        if (binary.Operator is "=" or "!=")
        {
            if (left.IsNumber && right.IsNumber)
            {
                var equal = left.Number == right.Number;
                return CalculatorValue.FromBoolean(binary.Operator == "=" ? equal : !equal);
            }

            if (left.IsBoolean && right.IsBoolean)
            {
                var equal = left.Boolean == right.Boolean;
                return CalculatorValue.FromBoolean(binary.Operator == "=" ? equal : !equal);
            }

            throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "Values cannot be compared.");
        }

        var leftNumber = RequireNumber(left);
        var rightNumber = RequireNumber(right);
        var numeric = binary.Operator switch
        {
            "+" => CalculatorMath.Add(leftNumber, rightNumber),
            "-" => CalculatorMath.Subtract(leftNumber, rightNumber),
            "*" => CalculatorMath.Multiply(leftNumber, rightNumber),
            "/" => CalculatorMath.Divide(leftNumber, rightNumber),
            "%" => CalculatorMath.Remainder(leftNumber, rightNumber),
            _ => (decimal?)null,
        };
        if (numeric is decimal number)
        {
            return CalculatorValue.FromNumber(number);
        }

        var comparison = binary.Operator switch
        {
            ">" => leftNumber > rightNumber,
            ">=" => leftNumber >= rightNumber,
            "<" => leftNumber < rightNumber,
            "<=" => leftNumber <= rightNumber,
            _ => throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "Unsupported operator."),
        };
        return CalculatorValue.FromBoolean(comparison);
    }

    private static CalculatorValue EvaluateCall(FunctionCall call, Func<string, CalculatorValue> resolve)
    {
        var function = CalculatorFunctionCatalog.Find(call.Name)
            ?? throw new FormulaEvaluationException(CalculatorIssueCodes.UnknownFunction, "The formula uses an unsupported function.");
        if (call.Arguments.Count < function.MinArguments || call.Arguments.Count > function.MaxArguments)
        {
            throw new FormulaEvaluationException(
                FormulaErrorCodes.InvalidArguments,
                $"Function {function.Name} has an invalid number of arguments.");
        }

        return call.Name switch
        {
            "IF" => EvaluateIf(call, resolve),
            "AND" => EvaluateAnd(call, resolve),
            "OR" => EvaluateOr(call, resolve),
            "NOT" => CalculatorValue.FromBoolean(!RequireBoolean(EvaluateCore(call.Arguments[0], resolve))),
            _ => function.Invoke(call.Arguments.Select(argument => EvaluateCore(argument, resolve)).ToArray()),
        };
    }

    private static CalculatorValue EvaluateIf(FunctionCall call, Func<string, CalculatorValue> resolve)
    {
        var condition = RequireBoolean(EvaluateCore(call.Arguments[0], resolve));
        return EvaluateCore(condition ? call.Arguments[1] : call.Arguments[2], resolve);
    }

    private static CalculatorValue EvaluateAnd(FunctionCall call, Func<string, CalculatorValue> resolve)
    {
        foreach (var argument in call.Arguments)
        {
            if (!RequireBoolean(EvaluateCore(argument, resolve)))
            {
                return CalculatorValue.FromBoolean(false);
            }
        }

        return CalculatorValue.FromBoolean(true);
    }

    private static CalculatorValue EvaluateOr(FunctionCall call, Func<string, CalculatorValue> resolve)
    {
        foreach (var argument in call.Arguments)
        {
            if (RequireBoolean(EvaluateCore(argument, resolve)))
            {
                return CalculatorValue.FromBoolean(true);
            }
        }

        return CalculatorValue.FromBoolean(false);
    }

    private static decimal RequireNumber(CalculatorValue value)
    {
        if (!value.IsNumber || value.Number is not decimal number)
        {
            throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "A numeric value is required.");
        }

        return number;
    }

    private static bool RequireBoolean(CalculatorValue value)
    {
        if (!value.IsBoolean || value.Boolean is not bool boolean)
        {
            throw new FormulaEvaluationException(FormulaErrorCodes.InvalidArguments, "A boolean value is required.");
        }

        return boolean;
    }
}
