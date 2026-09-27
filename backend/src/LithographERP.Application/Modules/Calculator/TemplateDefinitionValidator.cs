using LithographERP.Domain.Modules.Calculator;

namespace LithographERP.Application.Modules.Calculator;

public static class TemplateDefinitionValidator
{
    public static void Normalize(CalculatorTemplateDefinition definition)
    {
        definition.SellingPriceFieldKey = TrimToNull(definition.SellingPriceFieldKey);
        foreach (var element in definition.Elements)
        {
            element.Id = (element.Id ?? string.Empty).Trim();
            element.Type = (element.Type ?? string.Empty).Trim();
            element.Key = TrimToNull(element.Key);
            element.Label = TrimToNull(element.Label);
            element.Visibility = TrimToNull(element.Visibility);
            element.Formula = TrimToNull(element.Formula);
            element.DefaultText = TrimToNull(element.DefaultText);
            if (element.Options is not null)
            {
                foreach (var option in element.Options)
                {
                    option.Value = (option.Value ?? string.Empty).Trim();
                    option.Label = (option.Label ?? string.Empty).Trim();
                }
            }

            if (element.Columns is not null)
            {
                foreach (var column in element.Columns)
                {
                    column.Key = (column.Key ?? string.Empty).Trim();
                    column.Label = (column.Label ?? string.Empty).Trim();
                    column.Type = (column.Type ?? string.Empty).Trim();
                    column.Visibility = TrimToNull(column.Visibility);
                    column.Formula = TrimToNull(column.Formula);
                    if (column.Options is null)
                    {
                        continue;
                    }

                    foreach (var option in column.Options)
                    {
                        option.Value = (option.Value ?? string.Empty).Trim();
                        option.Label = (option.Label ?? string.Empty).Trim();
                    }
                }
            }
        }
    }

    public static void EnsureCanStore(CalculatorTemplateDefinition definition)
    {
        if (definition.Elements is null)
        {
            throw CalculatorRequestException.Field("definition", "Elements are required.");
        }

        Normalize(definition);
        if (definition.SchemaVersion != CalculatorSchema.SupportedVersion)
        {
            throw UnsupportedSchema();
        }

        if (definition.Elements.Count > CalculatorLimits.MaxElements)
        {
            throw CalculatorRequestException.Field(
                "definition",
                $"A template can contain at most {CalculatorLimits.MaxElements} elements.");
        }

        foreach (var element in definition.Elements)
        {
            EnsureFormulaLength(element.Formula);
            if (element.Columns is null)
            {
                continue;
            }

            if (element.Columns.Count > CalculatorLimits.MaxColumns)
            {
                throw CalculatorRequestException.Field(
                    "definition",
                    $"A table can contain at most {CalculatorLimits.MaxColumns} columns.");
            }

            foreach (var column in element.Columns)
            {
                EnsureFormulaLength(column.Formula);
            }
        }
    }

    public static TemplateValidationResult Validate(CalculatorTemplateDefinition definition)
    {
        var copy = CalculatorDefinitionJson.Clone(definition);
        if (copy.Elements is null)
        {
            return Invalid(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Elements are required."));
        }

        Normalize(copy);
        if (copy.SchemaVersion != CalculatorSchema.SupportedVersion)
        {
            return Invalid(Issue(
                CalculatorIssueCodes.UnsupportedSchemaVersion,
                "This Calculator Template uses an unsupported definition version."));
        }

        var errors = new List<CalculatorValidationIssue>();
        if (copy.Elements.Count > CalculatorLimits.MaxElements)
        {
            errors.Add(Issue(
                CalculatorIssueCodes.InvalidElement,
                $"A template can contain at most {CalculatorLimits.MaxElements} elements."));
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var fields = new List<FieldSite>();
        foreach (var element in copy.Elements)
        {
            ReadElement(element, errors, seenIds, fields);
        }

        var unique = new Dictionary<string, FieldSite>(StringComparer.Ordinal);
        var duplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (!FieldKeyRules.TryValidate(field.Key, out var keyMessage))
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidFieldKey, keyMessage, field.ElementId, field.Key));
                continue;
            }

            knownKeys.Add(field.Key);
            if (!unique.TryAdd(field.Key, field))
            {
                duplicateKeys.Add(field.Key);
            }
        }

        foreach (var key in duplicateKeys)
        {
            foreach (var field in fields.Where(candidate => candidate.Key == key))
            {
                errors.Add(Issue(
                    CalculatorIssueCodes.DuplicateFieldKey,
                    "Each Calculator field key must be unique.",
                    field.ElementId,
                    key));
            }

            unique.Remove(key);
        }

        var formulas = new Dictionary<string, FormulaExpression>(StringComparer.Ordinal);
        var dependencies = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        foreach (var field in unique.Values.Where(candidate => candidate.Type == CalculatorElementTypes.CalculatedField))
        {
            if (string.IsNullOrWhiteSpace(field.Formula))
            {
                continue;
            }

            FormulaExpression expression;
            try
            {
                expression = FormulaParser.Parse(field.Formula);
            }
            catch (FormulaParseException exception)
            {
                errors.Add(Issue(exception.Code, FormulaMessage(field.Key, exception), field.ElementId, field.Key));
                continue;
            }

            var references = FormulaParser.FieldReferences(expression);
            foreach (var reference in references)
            {
                if (!knownKeys.Contains(reference))
                {
                    errors.Add(Issue(
                        CalculatorIssueCodes.UnknownFieldReference,
                        $"Formula references unknown field '{reference}'.",
                        field.ElementId,
                        field.Key));
                }
            }

            formulas[field.Key] = expression;
            dependencies[field.Key] = references;
        }

        var analysis = DependencyGraph.Analyze(dependencies);
        if (analysis.HasCycle)
        {
            errors.Add(Issue(CalculatorIssueCodes.CircularReference, DependencyGraph.FormatCycle(analysis.Cycle)));
        }

        var types = new Dictionary<string, FormulaValueKind>(StringComparer.Ordinal);
        foreach (var field in unique.Values)
        {
            if (field.Type == CalculatorElementTypes.NumberInput)
            {
                types[field.Key] = FormulaValueKind.Number;
            }
            else if (field.Type == CalculatorElementTypes.Checkbox)
            {
                types[field.Key] = FormulaValueKind.Boolean;
            }
            else if (field.Type == CalculatorElementTypes.Dropdown && field.NumericDropdown)
            {
                types[field.Key] = FormulaValueKind.Number;
            }
        }

        var pending = formulas.Keys.ToHashSet(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var ready = pending.Where(key => dependencies[key]
                .Where(formulas.ContainsKey)
                .All(types.ContainsKey))
                .ToArray();
            if (ready.Length == 0)
            {
                break;
            }

            foreach (var key in ready)
            {
                pending.Remove(key);
                if (!unique.TryGetValue(key, out var field))
                {
                    continue;
                }

                if (!FormulaTypeChecker.TryCheck(formulas[key], types, out var kind, out var typeError))
                {
                    errors.Add(Issue(
                        CalculatorIssueCodes.InvalidFormulaArguments,
                        typeError ?? "The formula arguments are invalid.",
                        field.ElementId,
                        field.Key));
                    continue;
                }

                types[key] = kind;
            }
        }

        ValidateSellingPrice(copy, unique, types, errors);
        return errors.Count == 0 ? TemplateValidationResult.Valid : new TemplateValidationResult(false, errors);
    }

    private static void ValidateSellingPrice(
        CalculatorTemplateDefinition definition,
        IReadOnlyDictionary<string, FieldSite> fields,
        IReadOnlyDictionary<string, FormulaValueKind> types,
        List<CalculatorValidationIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(definition.SellingPriceFieldKey))
        {
            errors.Add(Issue(
                CalculatorIssueCodes.SellingPriceFieldMissing,
                "A Selling Price output field must be configured before publication."));
            return;
        }

        var key = definition.SellingPriceFieldKey;
        if (!fields.TryGetValue(key, out var field)
            || field.Type != CalculatorElementTypes.CalculatedField
            || !types.TryGetValue(key, out var kind)
            || kind != FormulaValueKind.Number)
        {
            errors.Add(Issue(
                CalculatorIssueCodes.SellingPriceFieldInvalid,
                "The Selling Price output must be a numeric calculated field.",
                field?.ElementId,
                key));
        }
    }

    private static void ReadElement(
        CalculatorElementDefinition element,
        List<CalculatorValidationIssue> errors,
        ISet<string> seenIds,
        List<FieldSite> fields)
    {
        if (string.IsNullOrWhiteSpace(element.Id))
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Element id is required."));
        }
        else if (element.Id.Length > CalculatorLimits.MaxElementIdLength)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Element id is too long.", element.Id));
        }
        else if (!seenIds.Add(element.Id))
        {
            errors.Add(Issue(CalculatorIssueCodes.DuplicateElementId, "Each element id must be unique.", element.Id));
        }

        if (!CalculatorElementTypes.All.Contains(element.Type))
        {
            errors.Add(Issue(CalculatorIssueCodes.UnknownElementType, "The element type is not supported.", element.Id));
            return;
        }

        if (element.Type is CalculatorElementTypes.Label or CalculatorElementTypes.Section)
        {
            RequireLabel(element.Label, element.Id, errors);
            RejectValueProperties(element, errors);
            if (element.Columns is { Count: > 0 })
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain columns.", element.Id));
            }

            return;
        }

        if (element.Type == CalculatorElementTypes.Table)
        {
            if (element.Key is not null)
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "A table cannot have a field key.", element.Id));
            }

            if (element.Formula is not null)
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "A table cannot contain a formula.", element.Id));
            }

            ReadColumns(element, errors, fields);
            return;
        }

        ReadValueField(
            element.Id,
            element.Type,
            element.Key,
            element.Label,
            element.Visibility,
            element.Formula,
            element.Options,
            element.DefaultValue,
            element.Min,
            element.Max,
            element.DecimalPlaces,
            element.DefaultText,
            element.DefaultChecked,
            element.Columns,
            errors,
            fields);
    }

    private static void ReadColumns(
        CalculatorElementDefinition element,
        List<CalculatorValidationIssue> errors,
        List<FieldSite> fields)
    {
        if (element.Columns is null || element.Columns.Count == 0)
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "A table must contain columns.", element.Id));
            return;
        }

        if (element.Columns.Count > CalculatorLimits.MaxColumns)
        {
            errors.Add(Issue(
                CalculatorIssueCodes.InvalidElement,
                $"A table can contain at most {CalculatorLimits.MaxColumns} columns.",
                element.Id));
        }

        foreach (var column in element.Columns)
        {
            if (!CalculatorElementTypes.IsValueField(column.Type))
            {
                errors.Add(Issue(CalculatorIssueCodes.UnknownElementType, "The column type is not supported.", element.Id, column.Key));
                continue;
            }

            ReadValueField(
                element.Id,
                column.Type,
                string.IsNullOrWhiteSpace(column.Key) ? null : column.Key,
                column.Label,
                column.Visibility,
                column.Formula,
                column.Options,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                errors,
                fields);
        }
    }

    private static void ReadValueField(
        string elementId,
        string type,
        string? key,
        string? label,
        string? visibility,
        string? formula,
        List<CalculatorDropdownOption>? options,
        decimal? defaultValue,
        decimal? min,
        decimal? max,
        int? decimalPlaces,
        string? defaultText,
        bool? defaultChecked,
        List<CalculatorTableColumn>? columns,
        List<CalculatorValidationIssue> errors,
        List<FieldSite> fields)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Field key is required.", elementId));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Label is required.", elementId, key));
        }
        else if (label.Length > CalculatorLimits.MaxLabelLength)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Label is too long.", elementId, key));
        }

        if (string.IsNullOrWhiteSpace(visibility))
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Visibility is required.", elementId, key));
        }
        else if (!FieldVisibilityScopes.All.Contains(visibility))
        {
            errors.Add(Issue(
                CalculatorIssueCodes.InvalidVisibility,
                "Visibility must be general, selling, or cost.",
                elementId,
                key));
        }

        if (columns is { Count: > 0 })
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain columns.", elementId, key));
        }

        var numericDropdown = false;
        switch (type)
        {
            case CalculatorElementTypes.NumberInput:
                RejectFormula(formula, elementId, key, errors);
                RejectOptions(options, elementId, key, errors);
                RejectText(defaultText, elementId, key, errors);
                RejectChecked(defaultChecked, elementId, key, errors);
                if (decimalPlaces is < 0 or > CalculatorLimits.MaxDecimalPlaces)
                {
                    errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Decimal places are invalid.", elementId, key));
                }

                if (min is decimal minimum && max is decimal maximum && minimum > maximum)
                {
                    errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Minimum cannot be greater than maximum.", elementId, key));
                }

                if (defaultValue is decimal chosen && ((min is decimal low && chosen < low) || (max is decimal high && chosen > high)))
                {
                    errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Default value is outside the allowed range.", elementId, key));
                }

                break;
            case CalculatorElementTypes.TextInput:
                RejectFormula(formula, elementId, key, errors);
                RejectOptions(options, elementId, key, errors);
                RejectNumberProps(defaultValue, min, max, decimalPlaces, elementId, key, errors);
                RejectChecked(defaultChecked, elementId, key, errors);
                if (defaultText is { Length: > 500 })
                {
                    errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Default text is too long.", elementId, key));
                }

                break;
            case CalculatorElementTypes.Checkbox:
                RejectFormula(formula, elementId, key, errors);
                RejectOptions(options, elementId, key, errors);
                RejectNumberProps(defaultValue, min, max, decimalPlaces, elementId, key, errors);
                RejectText(defaultText, elementId, key, errors);
                break;
            case CalculatorElementTypes.Dropdown:
                RejectFormula(formula, elementId, key, errors);
                RejectNumberProps(defaultValue, min, max, decimalPlaces, elementId, key, errors);
                RejectText(defaultText, elementId, key, errors);
                RejectChecked(defaultChecked, elementId, key, errors);
                numericDropdown = ValidateDropdown(options, elementId, key, errors);
                break;
            case CalculatorElementTypes.CalculatedField:
                RejectOptions(options, elementId, key, errors);
                RejectNumberProps(defaultValue, min, max, decimalPlaces, elementId, key, errors);
                RejectText(defaultText, elementId, key, errors);
                RejectChecked(defaultChecked, elementId, key, errors);
                if (string.IsNullOrWhiteSpace(formula))
                {
                    errors.Add(Issue(CalculatorIssueCodes.InvalidFormulaSyntax, "Formula is required.", elementId, key));
                }

                break;
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            fields.Add(new FieldSite(elementId, key, type, formula, numericDropdown));
        }
    }

    private static bool ValidateDropdown(
        List<CalculatorDropdownOption>? options,
        string elementId,
        string? key,
        List<CalculatorValidationIssue> errors)
    {
        if (options is null || options.Count == 0)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidDropdown, "Dropdown options are required.", elementId, key));
            return false;
        }

        if (options.Count > CalculatorLimits.MaxOptions)
        {
            errors.Add(Issue(
                CalculatorIssueCodes.InvalidDropdown,
                $"A dropdown can contain at most {CalculatorLimits.MaxOptions} options.",
                elementId,
                key));
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        var numeric = 0;
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Value) || option.Value.Length > CalculatorLimits.MaxOptionValueLength)
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidDropdown, "Each dropdown option needs a value.", elementId, key));
            }
            else if (!values.Add(option.Value))
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidDropdown, "Dropdown option values must be unique.", elementId, key));
            }

            if (string.IsNullOrWhiteSpace(option.Label) || option.Label.Length > CalculatorLimits.MaxLabelLength)
            {
                errors.Add(Issue(CalculatorIssueCodes.InvalidDropdown, "Each dropdown option needs a label.", elementId, key));
            }

            if (option.NumericValue is not null)
            {
                numeric++;
            }
        }

        if (numeric > 0 && numeric != options.Count)
        {
            errors.Add(Issue(
                CalculatorIssueCodes.InvalidDropdown,
                "Every dropdown option must define a numeric value when any option does.",
                elementId,
                key));
            return false;
        }

        return numeric == options.Count && options.Count > 0;
    }

    private static void RequireLabel(string? label, string elementId, List<CalculatorValidationIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(Issue(CalculatorIssueCodes.MissingRequiredProperty, "Label is required.", elementId));
        }
        else if (label.Length > CalculatorLimits.MaxLabelLength)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "Label is too long.", elementId));
        }
    }

    private static void RejectValueProperties(CalculatorElementDefinition element, List<CalculatorValidationIssue> errors)
    {
        if (element.Key is not null || element.Formula is not null || element.Visibility is not null || element.Options is { Count: > 0 })
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot store a Calculator value.", element.Id));
        }
    }

    private static void RejectFormula(string? formula, string elementId, string? key, List<CalculatorValidationIssue> errors)
    {
        if (formula is not null)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain a formula.", elementId, key));
        }
    }

    private static void RejectOptions(
        List<CalculatorDropdownOption>? options,
        string elementId,
        string? key,
        List<CalculatorValidationIssue> errors)
    {
        if (options is { Count: > 0 })
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain options.", elementId, key));
        }
    }

    private static void RejectNumberProps(
        decimal? defaultValue,
        decimal? min,
        decimal? max,
        int? decimalPlaces,
        string elementId,
        string? key,
        List<CalculatorValidationIssue> errors)
    {
        if (defaultValue is not null || min is not null || max is not null || decimalPlaces is not null)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain numeric limits.", elementId, key));
        }
    }

    private static void RejectText(string? defaultText, string elementId, string? key, List<CalculatorValidationIssue> errors)
    {
        if (defaultText is not null)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain default text.", elementId, key));
        }
    }

    private static void RejectChecked(bool? defaultChecked, string elementId, string? key, List<CalculatorValidationIssue> errors)
    {
        if (defaultChecked is not null)
        {
            errors.Add(Issue(CalculatorIssueCodes.InvalidElement, "This element cannot contain a default checked value.", elementId, key));
        }
    }

    private static void EnsureFormulaLength(string? formula)
    {
        if (formula is { Length: > CalculatorLimits.MaxFormulaLength })
        {
            throw CalculatorRequestException.Field(
                "definition",
                $"A formula can contain at most {CalculatorLimits.MaxFormulaLength} characters.");
        }
    }

    private static string FormulaMessage(string fieldKey, FormulaParseException exception) =>
        exception.Code == CalculatorIssueCodes.UnknownFunction
            ? exception.Message
            : $"Formula for '{fieldKey}' is invalid: {exception.Message}";

    private static CalculatorRequestException UnsupportedSchema() =>
        new(
            CalculatorErrorCodes.SchemaVersionUnsupported,
            "This Calculator Template uses an unsupported definition version.",
            409,
            [
                new CalculatorValidationIssue(
                    CalculatorIssueCodes.UnsupportedSchemaVersion,
                    "This Calculator Template uses an unsupported definition version."),
            ]);

    private static TemplateValidationResult Invalid(CalculatorValidationIssue issue) =>
        new(false, [issue]);

    private static CalculatorValidationIssue Issue(string code, string message, string? elementId = null, string? fieldKey = null) =>
        new(code, message, elementId, fieldKey);

    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record FieldSite(string ElementId, string Key, string Type, string? Formula, bool NumericDropdown);
}
