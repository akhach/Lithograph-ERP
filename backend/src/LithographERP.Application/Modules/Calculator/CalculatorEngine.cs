using LithographERP.Domain.Modules.Calculator;

namespace LithographERP.Application.Modules.Calculator;

public sealed record FieldCalculationError(string FieldKey, string Code, string Message);

public sealed record CalculatorEvaluation(
    bool Completed,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> EvaluationOrder,
    IReadOnlyDictionary<string, CalculatorValue> Values,
    IReadOnlyList<FieldCalculationError> FieldErrors)
{
    public static CalculatorEvaluation Failed(string code, string message) =>
        new(false, code, message, [], new Dictionary<string, CalculatorValue>(StringComparer.Ordinal), []);
}

public static class CalculatorEngine
{
    public static CalculatorEvaluation Evaluate(
        CalculatorTemplateDefinition definition,
        IReadOnlyDictionary<string, CalculatorValue>? inputs = null)
    {
        var copy = CalculatorDefinitionJson.Clone(definition);
        var validation = TemplateDefinitionValidator.Validate(copy);
        if (!validation.IsValid)
        {
            var first = validation.Errors[0];
            return CalculatorEvaluation.Failed(first.Code, first.Message);
        }

        TemplateDefinitionValidator.Normalize(copy);
        var fields = ReadFields(copy);
        var formulas = new Dictionary<string, FormulaExpression>(StringComparer.Ordinal);
        var dependencies = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        foreach (var field in fields.Where(candidate => candidate.Type == CalculatorElementTypes.CalculatedField))
        {
            var expression = FormulaParser.Parse(field.Formula);
            formulas[field.Key] = expression;
            dependencies[field.Key] = FormulaParser.FieldReferences(expression);
        }

        var analysis = DependencyGraph.Analyze(dependencies);
        if (analysis.HasCycle)
        {
            return CalculatorEvaluation.Failed(
                CalculatorIssueCodes.CircularReference,
                DependencyGraph.FormatCycle(analysis.Cycle));
        }

        var values = new Dictionary<string, CalculatorValue>(StringComparer.Ordinal);
        var supplied = inputs ?? new Dictionary<string, CalculatorValue>();
        foreach (var field in fields.Where(candidate => candidate.Type != CalculatorElementTypes.CalculatedField))
        {
            if (supplied.TryGetValue(field.Key, out var provided))
            {
                values[field.Key] = provided;
                continue;
            }

            if (field.Type == CalculatorElementTypes.NumberInput && field.DefaultValue is decimal number)
            {
                values[field.Key] = CalculatorValue.FromNumber(number);
            }
            else if (field.Type == CalculatorElementTypes.Checkbox && field.DefaultChecked is bool flag)
            {
                values[field.Key] = CalculatorValue.FromBoolean(flag);
            }
        }

        var fieldErrors = new List<FieldCalculationError>();
        foreach (var key in analysis.EvaluationOrder)
        {
            try
            {
                values[key] = FormulaEvaluator.Evaluate(formulas[key], Resolve);
            }
            catch (FormulaEvaluationException exception)
            {
                fieldErrors.Add(new FieldCalculationError(key, exception.Code, exception.Message));
            }
        }

        var completed = fieldErrors.Count == 0;
        return new CalculatorEvaluation(
            completed,
            completed ? null : fieldErrors[0].Code,
            completed ? null : fieldErrors[0].Message,
            analysis.EvaluationOrder,
            values,
            fieldErrors);

        CalculatorValue Resolve(string key)
        {
            var failed = fieldErrors.FirstOrDefault(error => error.FieldKey == key);
            if (failed is not null)
            {
                throw new FormulaEvaluationException(failed.Code, failed.Message);
            }

            if (values.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new FormulaEvaluationException(
                FormulaErrorCodes.MissingValue,
                $"Required value for '{key}' is missing.");
        }
    }

    private static List<RuntimeField> ReadFields(CalculatorTemplateDefinition definition)
    {
        var fields = new List<RuntimeField>();
        foreach (var element in definition.Elements)
        {
            if (element.Type == CalculatorElementTypes.Table)
            {
                foreach (var column in element.Columns ?? [])
                {
                    fields.Add(new RuntimeField(column.Key, column.Type, column.Formula, null, null));
                }

                continue;
            }

            if (CalculatorElementTypes.IsValueField(element.Type) && element.Key is not null)
            {
                fields.Add(new RuntimeField(element.Key, element.Type, element.Formula, element.DefaultValue, element.DefaultChecked));
            }
        }

        return fields;
    }

    private sealed record RuntimeField(string Key, string Type, string? Formula, decimal? DefaultValue, bool? DefaultChecked);
}
