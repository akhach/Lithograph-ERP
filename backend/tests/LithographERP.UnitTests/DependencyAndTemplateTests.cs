namespace LithographERP.UnitTests;

public class DependencyAndTemplateTests
{
    [Fact]
    public void DependencyGraph_OrdersAChainAndRejectsCycles()
    {
        var valid = DependencyGraph.Analyze(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["b"] = ["a"],
            ["c"] = ["b"],
            ["d"] = ["c"],
        });
        Assert.False(valid.HasCycle);
        Assert.Equal(new[] { "b", "c", "d" }, valid.EvaluationOrder);

        var direct = DependencyGraph.Analyze(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["a"] = ["a"],
        });
        Assert.Equal(new[] { "a", "a" }, direct.Cycle);

        var pair = DependencyGraph.Analyze(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["a"] = ["b"],
            ["b"] = ["a"],
        });
        Assert.Equal(new[] { "a", "b", "a" }, pair.Cycle);

        var loop = DependencyGraph.Analyze(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["a"] = ["b"],
            ["b"] = ["c"],
            ["c"] = ["d"],
            ["d"] = ["a"],
        });
        Assert.True(loop.HasCycle);
        Assert.Contains("a", loop.Cycle);
        Assert.Contains("d", loop.Cycle);
    }

    [Fact]
    public void Engine_EvaluatesDependenciesBeforeDisplayOrder()
    {
        var definition = PriceTemplate(
            Input("selling_late", "number_input", "quantity"),
            Calculated("selling_price", "line_total * 2"),
            Calculated("line_total", "quantity * 4"));
        var result = CalculatorEngine.Evaluate(definition, Values(("quantity", 3m)));
        Assert.True(result.Completed);
        Assert.True(result.EvaluationOrder.ToList().IndexOf("line_total") < result.EvaluationOrder.ToList().IndexOf("selling_price"));
        Assert.Equal(12m, result.Values["line_total"].Number);
        Assert.Equal(24m, result.Values["selling_price"].Number);
    }

    [Fact]
    public void Engine_UsesDefaultsAndDoesNotReplaceCalculatedFields()
    {
        var definition = PriceTemplate(
            new CalculatorElementDefinition
            {
                Id = "width",
                Type = "number_input",
                Key = "width_mm",
                Label = "Width",
                Visibility = "general",
                DefaultValue = 5m,
            },
            Calculated("selling_price", "width_mm + 1"));
        var withDefault = CalculatorEngine.Evaluate(definition);
        Assert.Equal(6m, withDefault.Values["selling_price"].Number);

        var overridden = CalculatorEngine.Evaluate(definition, Values(("width_mm", 9m), ("selling_price", 999m)));
        Assert.Equal(10m, overridden.Values["selling_price"].Number);
    }

    [Fact]
    public void Engine_ReportsMissingValuesAndDivisionByZero()
    {
        var missing = CalculatorEngine.Evaluate(PriceTemplate(
            Input("height", "number_input", "height_mm"),
            Calculated("selling_price", "height_mm / 2")));
        Assert.False(missing.Completed);
        Assert.Equal(FormulaErrorCodes.MissingValue, missing.ErrorCode);
        Assert.False(missing.Values.ContainsKey("selling_price"));

        var divided = CalculatorEngine.Evaluate(
            PriceTemplate(Input("qty", "number_input", "quantity"), Calculated("selling_price", "1 / quantity")),
            Values(("quantity", 0m)));
        Assert.Equal(FormulaErrorCodes.DivisionByZero, divided.ErrorCode);
    }

    [Theory]
    [InlineData("a", "a + 1", null, null)]
    [InlineData("a", "b + 1", "b", "a + 1")]
    public void Validator_RejectsCircularReferences(string leftKey, string leftFormula, string? rightKey, string? rightFormula)
    {
        var elements = new List<CalculatorElementDefinition>
        {
            Calculated(leftKey, leftFormula),
        };
        if (rightKey is not null && rightFormula is not null)
        {
            elements.Add(Calculated(rightKey, rightFormula));
        }

        if (leftKey != "selling_price" && rightKey != "selling_price")
        {
            elements.Add(Calculated("selling_price", "1"));
        }

        var result = TemplateDefinitionValidator.Validate(new CalculatorTemplateDefinition
        {
            SchemaVersion = 1,
            SellingPriceFieldKey = "selling_price",
            Elements = elements,
        });
        Assert.Contains(result.Errors, error => error.Code == CalculatorIssueCodes.CircularReference);
        Assert.Contains("→", result.Errors.Single(error => error.Code == CalculatorIssueCodes.CircularReference).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validator_RejectsMultiFieldCyclesAndAcceptsChains()
    {
        var cycle = TemplateDefinitionValidator.Validate(PriceTemplate(
            Calculated("a", "b"),
            Calculated("b", "c"),
            Calculated("c", "d"),
            Calculated("d", "a"),
            Calculated("selling_price", "1")));
        Assert.Contains(cycle.Errors, error => error.Code == CalculatorIssueCodes.CircularReference);

        var chain = TemplateDefinitionValidator.Validate(PriceTemplate(
            Input("d", "number_input", "d"),
            Calculated("c", "d"),
            Calculated("b", "c"),
            Calculated("a", "b"),
            Calculated("selling_price", "a")));
        Assert.True(chain.IsValid);
    }

    [Fact]
    public void Validator_ChecksKeysElementsDropdownsAndSellingPrice()
    {
        Assert.True(TemplateDefinitionValidator.Validate(Sample()).IsValid);

        var duplicate = Sample();
        duplicate.Elements.Add(Input("other", "number_input", "width_mm"));
        Assert.Contains(TemplateDefinitionValidator.Validate(duplicate).Errors, error => error.Code == CalculatorIssueCodes.DuplicateFieldKey);

        var invalidKey = Sample();
        invalidKey.Elements[0].Key = "Width";
        Assert.Contains(TemplateDefinitionValidator.Validate(invalidKey).Errors, error => error.Code == CalculatorIssueCodes.InvalidFieldKey);

        var reserved = Sample();
        reserved.Elements[0].Key = "true";
        Assert.Contains(TemplateDefinitionValidator.Validate(reserved).Errors, error => error.Code == CalculatorIssueCodes.InvalidFieldKey);

        var unknownType = Sample();
        unknownType.Elements.Add(new CalculatorElementDefinition { Id = "pivot", Type = "pivot", Label = "Pivot" });
        Assert.Contains(TemplateDefinitionValidator.Validate(unknownType).Errors, error => error.Code == CalculatorIssueCodes.UnknownElementType);

        var dropdown = Sample();
        dropdown.Elements.Add(new CalculatorElementDefinition
        {
            Id = "material",
            Type = "dropdown",
            Key = "material",
            Label = "Material",
            Visibility = "general",
            Options =
            [
                new CalculatorDropdownOption { Value = "a", Label = "A", NumericValue = 1m },
                new CalculatorDropdownOption { Value = "b", Label = "B" },
            ],
        });
        Assert.Contains(
            TemplateDefinitionValidator.Validate(dropdown).Errors,
            error => error.Code == CalculatorIssueCodes.InvalidDropdown);

        var visibility = Sample();
        visibility.Elements[0].Visibility = "secret";
        Assert.Contains(TemplateDefinitionValidator.Validate(visibility).Errors, error => error.Code == CalculatorIssueCodes.InvalidVisibility);

        var unknownField = Sample();
        unknownField.Elements[2].Formula = "width_mm * unknown_height";
        var unknown = TemplateDefinitionValidator.Validate(unknownField);
        Assert.Contains(unknown.Errors, error => error.Code == CalculatorIssueCodes.UnknownFieldReference && error.FieldKey == "area_m2");

        var missingSelling = Sample();
        missingSelling.SellingPriceFieldKey = null;
        Assert.Contains(TemplateDefinitionValidator.Validate(missingSelling).Errors, error => error.Code == CalculatorIssueCodes.SellingPriceFieldMissing);

        var unknownSelling = Sample();
        unknownSelling.SellingPriceFieldKey = "missing_price";
        Assert.Contains(TemplateDefinitionValidator.Validate(unknownSelling).Errors, error => error.Code == CalculatorIssueCodes.SellingPriceFieldInvalid);

        var inputSelling = Sample();
        inputSelling.SellingPriceFieldKey = "width_mm";
        Assert.Contains(TemplateDefinitionValidator.Validate(inputSelling).Errors, error => error.Code == CalculatorIssueCodes.SellingPriceFieldInvalid);

        var booleanSelling = Sample();
        booleanSelling.Elements[2] = Calculated("area_m2", "width_mm > 1");
        booleanSelling.SellingPriceFieldKey = "area_m2";
        booleanSelling.Elements.RemoveAt(3);
        Assert.Contains(TemplateDefinitionValidator.Validate(booleanSelling).Errors, error => error.Code == CalculatorIssueCodes.SellingPriceFieldInvalid);

        var mixed = Sample();
        mixed.Elements[3].Formula = "IF(width_mm > 1, 1, false)";
        Assert.Contains(TemplateDefinitionValidator.Validate(mixed).Errors, error => error.Code == CalculatorIssueCodes.InvalidFormulaArguments);

        var future = Sample();
        future.SchemaVersion = 2;
        var unsupported = TemplateDefinitionValidator.Validate(future);
        Assert.Equal(CalculatorIssueCodes.UnsupportedSchemaVersion, Assert.Single(unsupported.Errors).Code);
    }

    [Fact]
    public void Validator_AcceptsLabelsSectionsAndMinimalTables()
    {
        var definition = Sample();
        definition.Elements.Insert(0, new CalculatorElementDefinition { Id = "section", Type = "section", Label = "Dimensions" });
        definition.Elements.Insert(1, new CalculatorElementDefinition { Id = "note", Type = "label", Label = "Material Information" });
        definition.Elements.Add(new CalculatorElementDefinition
        {
            Id = "lines",
            Type = "table",
            Label = "Lines",
            Columns =
            [
                new CalculatorTableColumn { Key = "qty", Label = "Qty", Type = "number_input", Visibility = "general" },
                new CalculatorTableColumn { Key = "price", Label = "Price", Type = "number_input", Visibility = "cost" },
                new CalculatorTableColumn { Key = "line_total", Label = "Total", Type = "calculated_field", Visibility = "general", Formula = "qty * price" },
            ],
        });
        Assert.True(TemplateDefinitionValidator.Validate(definition).IsValid);
    }

    [Fact]
    public void DefinitionJson_RoundTripsSchemaElementsKeysAndFormulas()
    {
        var definition = Sample();
        var restored = CalculatorDefinitionJson.Deserialize(CalculatorDefinitionJson.Serialize(definition));
        Assert.Equal(definition.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(definition.SellingPriceFieldKey, restored.SellingPriceFieldKey);
        Assert.Equal(definition.Elements.Select(element => element.Key), restored.Elements.Select(element => element.Key));
        Assert.Equal(definition.Elements.Select(element => element.Formula), restored.Elements.Select(element => element.Formula));
    }

    private static CalculatorTemplateDefinition Sample() => new()
    {
        SchemaVersion = 1,
        SellingPriceFieldKey = "selling_price",
        Elements =
        [
            Input("width", "number_input", "width_mm"),
            Input("height", "number_input", "height_mm"),
            Calculated("area_m2", "(width_mm * height_mm) / 1000000"),
            Calculated("selling_price", "area_m2 * 1000"),
        ],
    };

    private static CalculatorTemplateDefinition PriceTemplate(params CalculatorElementDefinition[] elements) => new()
    {
        SchemaVersion = 1,
        SellingPriceFieldKey = "selling_price",
        Elements = elements.ToList(),
    };

    private static CalculatorElementDefinition Input(string id, string type, string key) => new()
    {
        Id = id,
        Type = type,
        Key = key,
        Label = key,
        Visibility = "general",
    };

    private static CalculatorElementDefinition Calculated(string key, string formula) => new()
    {
        Id = key,
        Type = "calculated_field",
        Key = key,
        Label = key,
        Visibility = key == "selling_price" ? "selling" : "general",
        Formula = formula,
    };

    private static Dictionary<string, CalculatorValue> Values(params (string Key, decimal Value)[] fields) =>
        fields.ToDictionary(field => field.Key, field => CalculatorValue.FromNumber(field.Value));
}
