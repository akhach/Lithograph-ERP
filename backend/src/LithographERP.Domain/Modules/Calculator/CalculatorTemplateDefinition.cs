using System.Text.Json.Serialization;

namespace LithographERP.Domain.Modules.Calculator;

public sealed class CalculatorTemplateDefinition
{
    public int SchemaVersion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SellingPriceFieldKey { get; set; }

    public List<CalculatorElementDefinition> Elements { get; set; } = [];

    public static CalculatorTemplateDefinition Empty() => new()
    {
        SchemaVersion = CalculatorSchema.SupportedVersion,
        Elements = [],
    };
}

public sealed class CalculatorElementDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DefaultValue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Min { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Max { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DecimalPlaces { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultText { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DefaultChecked { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CalculatorDropdownOption>? Options { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CalculatorTableColumn>? Columns { get; set; }
}

public sealed class CalculatorDropdownOption
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? NumericValue { get; set; }
}

public sealed class CalculatorTableColumn
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Formula { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CalculatorDropdownOption>? Options { get; set; }
}
