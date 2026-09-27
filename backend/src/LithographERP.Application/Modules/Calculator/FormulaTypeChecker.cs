namespace LithographERP.Application.Modules.Calculator;

public static class FormulaTypeChecker
{
    public static bool TryCheck(
        FormulaExpression expression,
        IReadOnlyDictionary<string, FormulaValueKind> fieldTypes,
        out FormulaValueKind kind,
        out string? error)
    {
        try
        {
            kind = Check(expression, fieldTypes);
            error = null;
            return true;
        }
        catch (FormulaTypeException exception)
        {
            kind = default;
            error = exception.Message;
            return false;
        }
    }

    private static FormulaValueKind Check(
        FormulaExpression expression,
        IReadOnlyDictionary<string, FormulaValueKind> fieldTypes) =>
        expression switch
        {
            NumberLiteral => FormulaValueKind.Number,
            BooleanLiteral => FormulaValueKind.Boolean,
            FieldReference field => Field(field.Key, fieldTypes),
            UnaryExpression unary => Number(Check(unary.Operand, fieldTypes)),
            BinaryExpression binary => Binary(binary, fieldTypes),
            FunctionCall call => Call(call, fieldTypes),
            _ => throw new FormulaTypeException("The formula could not be checked."),
        };

    private static FormulaValueKind Binary(
        BinaryExpression binary,
        IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        var left = Check(binary.Left, fieldTypes);
        var right = Check(binary.Right, fieldTypes);
        if (binary.Operator is "=" or "!=")
        {
            if (left != right)
            {
                throw new FormulaTypeException("Values cannot be compared.");
            }

            return FormulaValueKind.Boolean;
        }

        if (binary.Operator is ">" or ">=" or "<" or "<=")
        {
            Number(left);
            Number(right);
            return FormulaValueKind.Boolean;
        }

        Number(left);
        Number(right);
        return FormulaValueKind.Number;
    }

    private static FormulaValueKind Call(
        FunctionCall call,
        IReadOnlyDictionary<string, FormulaValueKind> fieldTypes) =>
        call.Name switch
        {
            "IF" => If(call, fieldTypes),
            "AND" or "OR" => Logical(call, fieldTypes),
            "NOT" => Not(call, fieldTypes),
            _ => Numeric(call, fieldTypes),
        };

    private static FormulaValueKind If(FunctionCall call, IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        Boolean(Check(call.Arguments[0], fieldTypes));
        var whenTrue = Check(call.Arguments[1], fieldTypes);
        var whenFalse = Check(call.Arguments[2], fieldTypes);
        if (whenTrue != whenFalse)
        {
            throw new FormulaTypeException("IF branches must return the same kind of value.");
        }

        return whenTrue;
    }

    private static FormulaValueKind Logical(FunctionCall call, IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        foreach (var argument in call.Arguments)
        {
            Boolean(Check(argument, fieldTypes));
        }

        return FormulaValueKind.Boolean;
    }

    private static FormulaValueKind Not(FunctionCall call, IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        Boolean(Check(call.Arguments[0], fieldTypes));
        return FormulaValueKind.Boolean;
    }

    private static FormulaValueKind Numeric(FunctionCall call, IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        foreach (var argument in call.Arguments)
        {
            Number(Check(argument, fieldTypes));
        }

        return FormulaValueKind.Number;
    }

    private static FormulaValueKind Field(string key, IReadOnlyDictionary<string, FormulaValueKind> fieldTypes)
    {
        if (fieldTypes.TryGetValue(key, out var kind))
        {
            return kind;
        }

        throw new FormulaTypeException($"Field '{key}' cannot be used in a formula.");
    }

    private static FormulaValueKind Number(FormulaValueKind kind)
    {
        if (kind != FormulaValueKind.Number)
        {
            throw new FormulaTypeException("A numeric value is required.");
        }

        return kind;
    }

    private static void Boolean(FormulaValueKind kind)
    {
        if (kind != FormulaValueKind.Boolean)
        {
            throw new FormulaTypeException("A boolean value is required.");
        }
    }

    private sealed class FormulaTypeException(string message) : Exception(message);
}
