using System.Text.Json;
using LithographERP.Domain.Modules.Calculator;

namespace LithographERP.Application.Modules.Calculator;

public abstract record StoredCalculatorValue
{
    public sealed record Number(decimal Value) : StoredCalculatorValue;

    public sealed record Text(string Value) : StoredCalculatorValue;

    public sealed record Flag(bool Value) : StoredCalculatorValue;
}

public enum OrderCalculatorOutcome
{
    Complete,
    Incomplete,
    CalculationFailed,
}

public sealed record OrderCalculatorComputation(
    OrderCalculatorOutcome Outcome,
    decimal? AuthoritativeSellingPrice,
    string? FailureMessage,
    IReadOnlyList<OrderCalculatorFieldError> FieldErrors,
    IReadOnlyDictionary<string, StoredCalculatorValue> CalculatedValues);

public static class OrderCalculatorEvaluation
{
    public const int MaxTextLength = 4000;

    public const decimal MaxSellingPrice = 9_999_999_999_999_999.99m;

    public const string MissingRequiredValue = "MISSING_REQUIRED_VALUE";

    public static Dictionary<string, StoredCalculatorValue> ReadStored(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Calculator field values must be a JSON object.");
        }

        var values = new Dictionary<string, StoredCalculatorValue>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.Number when property.Value.TryGetDecimal(out var number) => new StoredCalculatorValue.Number(number),
                JsonValueKind.String => new StoredCalculatorValue.Text(property.Value.GetString() ?? string.Empty),
                JsonValueKind.True => new StoredCalculatorValue.Flag(true),
                JsonValueKind.False => new StoredCalculatorValue.Flag(false),
                _ => throw new InvalidOperationException("Calculator field values contain an unsupported JSON value."),
            };
        }

        return values;
    }

    public static string WriteStored(IReadOnlyDictionary<string, StoredCalculatorValue> values)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var pair in values.OrderBy(candidate => candidate.Key, StringComparer.Ordinal))
            {
                writer.WritePropertyName(pair.Key);
                switch (pair.Value)
                {
                    case StoredCalculatorValue.Number number:
                        writer.WriteNumberValue(number.Value);
                        break;
                    case StoredCalculatorValue.Text text:
                        writer.WriteStringValue(text.Value);
                        break;
                    case StoredCalculatorValue.Flag flag:
                        writer.WriteBooleanValue(flag.Value);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Calculator value.");
                }
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public static bool TryPrepare(
        CalculatorTemplateDefinition definition,
        JsonElement submitted,
        IReadOnlyDictionary<string, StoredCalculatorValue> existing,
        OrderCalculatorAccess access,
        out Dictionary<string, StoredCalculatorValue> values,
        out IReadOnlyDictionary<string, string[]> errors)
    {
        values = new Dictionary<string, StoredCalculatorValue>(StringComparer.Ordinal);
        var fieldErrors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (submitted.ValueKind != JsonValueKind.Object)
        {
            errors = new Dictionary<string, string[]> { ["fieldValues"] = ["A value is required."] };
            return false;
        }

        var catalog = Catalog(definition);
        var submittedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in submitted.EnumerateObject())
        {
            if (!submittedKeys.Add(property.Name))
            {
                continue;
            }

            if (!catalog.TryGetValue(property.Name, out var field))
            {
                Add(fieldErrors, property.Name, "Unknown Calculator field.");
                continue;
            }

            if (!field.IsInput)
            {
                Add(fieldErrors, property.Name, "Calculated fields cannot be submitted.");
                continue;
            }

            if (!CanView(field.Visibility, access))
            {
                Add(fieldErrors, property.Name, "You cannot edit this Calculator field.");
                continue;
            }

            if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(property.Value.GetString()))
            {
                continue;
            }

            if (TryReadInput(field, property.Value, out var stored, out var message))
            {
                values[property.Name] = stored;
            }
            else
            {
                Add(fieldErrors, property.Name, message);
            }
        }

        if (fieldErrors.Count > 0)
        {
            errors = fieldErrors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
            values.Clear();
            return false;
        }

        foreach (var field in catalog.Values.Where(candidate => candidate.IsInput && !CanView(candidate.Visibility, access)))
        {
            if (existing.TryGetValue(field.Key, out var hidden))
            {
                values[field.Key] = hidden;
            }
        }

        errors = new Dictionary<string, string[]>();
        return true;
    }

    public static OrderCalculatorComputation Compute(
        CalculatorTemplateDefinition definition,
        IReadOnlyDictionary<string, StoredCalculatorValue> stored)
    {
        var catalog = Catalog(definition);
        var missing = MissingInputs(catalog, stored);
        var evaluation = CalculatorEngine.Evaluate(definition, EngineInputs(catalog, stored));
        if (missing.Count > 0)
        {
            var errors = missing
                .Select(key => new OrderCalculatorFieldError(
                    key,
                    MissingRequiredValue,
                    $"Required value for '{key}' is missing."))
                .ToArray();
            return new OrderCalculatorComputation(
                OrderCalculatorOutcome.Incomplete,
                0m,
                null,
                errors,
                Calculated(catalog, evaluation));
        }

        if (!evaluation.Completed)
        {
            var errors = evaluation.FieldErrors
                .Select(error => new OrderCalculatorFieldError(error.FieldKey, MapCode(error.Code), error.Message))
                .ToArray();
            if (errors.Any(error => error.Code == MissingRequiredValue))
            {
                return new OrderCalculatorComputation(
                    OrderCalculatorOutcome.Incomplete,
                    0m,
                    null,
                    errors,
                    Calculated(catalog, evaluation));
            }

            return new OrderCalculatorComputation(
                OrderCalculatorOutcome.CalculationFailed,
                null,
                "The Calculator could not be calculated.",
                errors,
                new Dictionary<string, StoredCalculatorValue>());
        }

        var sellingKey = definition.SellingPriceFieldKey ?? string.Empty;
        if (!evaluation.Values.TryGetValue(sellingKey, out var selling) || selling.Number is not decimal raw)
        {
            return new OrderCalculatorComputation(
                OrderCalculatorOutcome.CalculationFailed,
                null,
                "The Calculator could not be calculated.",
                [new OrderCalculatorFieldError(sellingKey, CalculatorErrorCodes.CalculationError, "The Selling Price could not be calculated.")],
                new Dictionary<string, StoredCalculatorValue>());
        }

        if (raw < 0)
        {
            return new OrderCalculatorComputation(
                OrderCalculatorOutcome.CalculationFailed,
                null,
                "Selling Price cannot be negative.",
                [new OrderCalculatorFieldError(sellingKey, CalculatorErrorCodes.CalculationError, "Selling Price cannot be negative.")],
                new Dictionary<string, StoredCalculatorValue>());
        }

        var money = decimal.Round(raw, 2, MidpointRounding.AwayFromZero);
        if (money > MaxSellingPrice)
        {
            return new OrderCalculatorComputation(
                OrderCalculatorOutcome.CalculationFailed,
                null,
                "Selling Price is outside the allowed range.",
                [new OrderCalculatorFieldError(sellingKey, CalculatorErrorCodes.CalculationError, "Selling Price is outside the allowed range.")],
                new Dictionary<string, StoredCalculatorValue>());
        }

        return new OrderCalculatorComputation(
            OrderCalculatorOutcome.Complete,
            money,
            null,
            [],
            Calculated(catalog, evaluation));
    }

    public static IReadOnlyList<OrderCalculatorElement> VisibleElements(
        CalculatorTemplateDefinition definition,
        OrderCalculatorAccess access)
    {
        var elements = new List<OrderCalculatorElement>();
        foreach (var element in definition.Elements)
        {
            if (element.Type is CalculatorElementTypes.Label or CalculatorElementTypes.Section)
            {
                elements.Add(MapElement(element, null));
                continue;
            }

            if (element.Type == CalculatorElementTypes.Table)
            {
                var columns = (element.Columns ?? [])
                    .Where(column => CanView(column.Visibility, access))
                    .Select(MapColumn)
                    .ToArray();
                if (columns.Length == 0)
                {
                    continue;
                }

                elements.Add(MapElement(element, columns));
                continue;
            }

            if (CanView(element.Visibility, access))
            {
                elements.Add(MapElement(element, null));
            }
        }

        return elements;
    }

    public static Dictionary<string, JsonElement> VisibleValues(
        CalculatorTemplateDefinition definition,
        IReadOnlyDictionary<string, StoredCalculatorValue> stored,
        OrderCalculatorAccess access)
    {
        var catalog = Catalog(definition);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var pair in stored)
        {
            if (catalog.TryGetValue(pair.Key, out var field) && field.IsInput && CanView(field.Visibility, access))
            {
                values[pair.Key] = ToJson(pair.Value);
            }
        }

        return values;
    }

    public static Dictionary<string, JsonElement> VisibleCalculated(
        CalculatorTemplateDefinition definition,
        IReadOnlyDictionary<string, StoredCalculatorValue> calculated,
        OrderCalculatorAccess access)
    {
        var catalog = Catalog(definition);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var pair in calculated)
        {
            if (catalog.TryGetValue(pair.Key, out var field) && !field.IsInput && CanView(field.Visibility, access))
            {
                values[pair.Key] = ToJson(pair.Value);
            }
        }

        return values;
    }

    public static bool CanView(string? visibility, OrderCalculatorAccess access) => visibility switch
    {
        FieldVisibilityScopes.General => true,
        FieldVisibilityScopes.Selling => access.SellingPrice,
        FieldVisibilityScopes.Cost => access.Costs,
        _ => false,
    };

    public static JsonElement ToJson(StoredCalculatorValue value) => value switch
    {
        StoredCalculatorValue.Number number => JsonSerializer.SerializeToElement(number.Value),
        StoredCalculatorValue.Text text => JsonSerializer.SerializeToElement(text.Value),
        StoredCalculatorValue.Flag flag => JsonSerializer.SerializeToElement(flag.Value),
        _ => throw new InvalidOperationException("Unsupported Calculator value."),
    };

    private static Dictionary<string, StoredCalculatorValue> Calculated(
        IReadOnlyDictionary<string, CatalogField> catalog,
        CalculatorEvaluation evaluation)
    {
        var calculated = new Dictionary<string, StoredCalculatorValue>(StringComparer.Ordinal);
        foreach (var pair in evaluation.Values)
        {
            if (!catalog.TryGetValue(pair.Key, out var field) || field.IsInput)
            {
                continue;
            }

            if (evaluation.FieldErrors.Any(error => error.FieldKey == pair.Key))
            {
                continue;
            }

            if (pair.Value.Number is decimal number)
            {
                calculated[pair.Key] = new StoredCalculatorValue.Number(number);
            }
            else if (pair.Value.Boolean is bool flag)
            {
                calculated[pair.Key] = new StoredCalculatorValue.Flag(flag);
            }
        }

        return calculated;
    }

    private static List<string> MissingInputs(
        IReadOnlyDictionary<string, CatalogField> catalog,
        IReadOnlyDictionary<string, StoredCalculatorValue> stored)
    {
        var missing = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in catalog.Values.Where(candidate => !candidate.IsInput && candidate.Formula is not null))
        {
            foreach (var reference in FormulaParser.FieldReferences(FormulaParser.Parse(field.Formula)))
            {
                if (!catalog.TryGetValue(reference, out var input) || !input.IsInput || !seen.Add(reference))
                {
                    continue;
                }

                if (!HasInput(input, stored))
                {
                    missing.Add(reference);
                }
            }
        }

        return missing;
    }

    private static bool HasInput(CatalogField field, IReadOnlyDictionary<string, StoredCalculatorValue> stored)
    {
        if (stored.TryGetValue(field.Key, out var value) && ValueMatches(field, value))
        {
            return true;
        }

        return field.Type switch
        {
            CalculatorElementTypes.NumberInput => field.DefaultValue is not null,
            CalculatorElementTypes.Checkbox => field.DefaultChecked is not null,
            _ => false,
        };
    }

    private static bool ValueMatches(CatalogField field, StoredCalculatorValue value) => field.Type switch
    {
        CalculatorElementTypes.NumberInput => value is StoredCalculatorValue.Number,
        CalculatorElementTypes.TextInput => value is StoredCalculatorValue.Text,
        CalculatorElementTypes.Dropdown => value is StoredCalculatorValue.Text text && Option(field, text.Value) is not null,
        CalculatorElementTypes.Checkbox => value is StoredCalculatorValue.Flag,
        _ => false,
    };

    private static Dictionary<string, CalculatorValue> EngineInputs(
        IReadOnlyDictionary<string, CatalogField> catalog,
        IReadOnlyDictionary<string, StoredCalculatorValue> stored)
    {
        var inputs = new Dictionary<string, CalculatorValue>(StringComparer.Ordinal);
        foreach (var field in catalog.Values.Where(candidate => candidate.IsInput))
        {
            if (!stored.TryGetValue(field.Key, out var value))
            {
                continue;
            }

            switch (field.Type)
            {
                case CalculatorElementTypes.NumberInput when value is StoredCalculatorValue.Number number:
                    inputs[field.Key] = CalculatorValue.FromNumber(number.Value);
                    break;
                case CalculatorElementTypes.Checkbox when value is StoredCalculatorValue.Flag flag:
                    inputs[field.Key] = CalculatorValue.FromBoolean(flag.Value);
                    break;
                case CalculatorElementTypes.Dropdown when value is StoredCalculatorValue.Text text:
                    var option = Option(field, text.Value);
                    if (option?.NumericValue is decimal numeric)
                    {
                        inputs[field.Key] = CalculatorValue.FromNumber(numeric);
                    }

                    break;
            }
        }

        return inputs;
    }

    private static bool TryReadInput(
        CatalogField field,
        JsonElement submitted,
        out StoredCalculatorValue stored,
        out string message)
    {
        stored = null!;
        message = string.Empty;
        switch (field.Type)
        {
            case CalculatorElementTypes.NumberInput:
                if (submitted.ValueKind != JsonValueKind.Number || !submitted.TryGetDecimal(out var number))
                {
                    message = "Expected a number.";
                    return false;
                }

                if (field.Min is decimal min && number < min)
                {
                    message = "Value is less than the allowed minimum.";
                    return false;
                }

                if (field.Max is decimal max && number > max)
                {
                    message = "Value is greater than the allowed maximum.";
                    return false;
                }

                stored = new StoredCalculatorValue.Number(number);
                return true;
            case CalculatorElementTypes.TextInput:
                if (submitted.ValueKind != JsonValueKind.String)
                {
                    message = "Expected text.";
                    return false;
                }

                var text = submitted.GetString() ?? string.Empty;
                if (text.Length == 0)
                {
                    message = "Expected text.";
                    return false;
                }

                if (text.Length > MaxTextLength)
                {
                    message = "Text is too long.";
                    return false;
                }

                stored = new StoredCalculatorValue.Text(text);
                return true;
            case CalculatorElementTypes.Checkbox:
                if (submitted.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
                {
                    message = "Expected true or false.";
                    return false;
                }

                stored = new StoredCalculatorValue.Flag(submitted.GetBoolean());
                return true;
            case CalculatorElementTypes.Dropdown:
                if (submitted.ValueKind != JsonValueKind.String)
                {
                    message = "Selected option is not valid.";
                    return false;
                }

                var selected = submitted.GetString() ?? string.Empty;
                if (Option(field, selected) is null)
                {
                    message = "Selected option is not valid.";
                    return false;
                }

                stored = new StoredCalculatorValue.Text(selected);
                return true;
            default:
                message = "Unknown Calculator field.";
                return false;
        }
    }

    private static CalculatorDropdownOption? Option(CatalogField field, string value) =>
        field.Options?.FirstOrDefault(option => option.Value == value);

    private static Dictionary<string, CatalogField> Catalog(CalculatorTemplateDefinition definition)
    {
        var catalog = new Dictionary<string, CatalogField>(StringComparer.Ordinal);
        foreach (var element in definition.Elements)
        {
            if (element.Type == CalculatorElementTypes.Table)
            {
                foreach (var column in element.Columns ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(column.Key))
                    {
                        catalog[column.Key] = FromColumn(column);
                    }
                }

                continue;
            }

            if (CalculatorElementTypes.IsValueField(element.Type) && !string.IsNullOrWhiteSpace(element.Key))
            {
                catalog[element.Key] = FromElement(element);
            }
        }

        return catalog;
    }

    private static CatalogField FromElement(CalculatorElementDefinition element) => new(
        element.Key!,
        element.Type,
        element.Visibility ?? string.Empty,
        element.Type != CalculatorElementTypes.CalculatedField,
        element.Formula,
        element.DefaultValue,
        element.Min,
        element.Max,
        element.DefaultChecked,
        element.Options);

    private static CatalogField FromColumn(CalculatorTableColumn column) => new(
        column.Key,
        column.Type,
        column.Visibility ?? string.Empty,
        column.Type != CalculatorElementTypes.CalculatedField,
        column.Formula,
        null,
        null,
        null,
        null,
        column.Options);

    private static OrderCalculatorElement MapElement(
        CalculatorElementDefinition element,
        IReadOnlyList<OrderCalculatorColumn>? columns) => new(
        element.Id,
        element.Type,
        element.Key,
        element.Label,
        element.Visibility,
        element.DefaultValue,
        element.Min,
        element.Max,
        element.DecimalPlaces,
        element.DefaultText,
        element.DefaultChecked,
        element.Options?.Select(option => new OrderCalculatorOption(option.Value, option.Label, option.NumericValue)).ToArray(),
        columns);

    private static OrderCalculatorColumn MapColumn(CalculatorTableColumn column) => new(
        column.Key,
        column.Label,
        column.Type,
        column.Visibility ?? string.Empty,
        column.Options?.Select(option => new OrderCalculatorOption(option.Value, option.Label, option.NumericValue)).ToArray());

    private static string MapCode(string code) =>
        code == FormulaErrorCodes.MissingValue ? MissingRequiredValue : code;

    private static void Add(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private sealed record CatalogField(
        string Key,
        string Type,
        string Visibility,
        bool IsInput,
        string? Formula,
        decimal? DefaultValue,
        decimal? Min,
        decimal? Max,
        bool? DefaultChecked,
        List<CalculatorDropdownOption>? Options);
}
