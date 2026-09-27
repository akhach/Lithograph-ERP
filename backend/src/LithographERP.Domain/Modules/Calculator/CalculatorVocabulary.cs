namespace LithographERP.Domain.Modules.Calculator;

public static class CalculatorSchema
{
    public const int SupportedVersion = 1;
}

public static class TemplateVersionStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Retired = "retired";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Published,
        Retired,
    };
}

public static class CalculatorElementTypes
{
    public const string Label = "label";
    public const string NumberInput = "number_input";
    public const string TextInput = "text_input";
    public const string Dropdown = "dropdown";
    public const string Checkbox = "checkbox";
    public const string CalculatedField = "calculated_field";
    public const string Table = "table";
    public const string Section = "section";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Label,
        NumberInput,
        TextInput,
        Dropdown,
        Checkbox,
        CalculatedField,
        Table,
        Section,
    };

    public static bool IsValueField(string? type) =>
        type is NumberInput or TextInput or Dropdown or Checkbox or CalculatedField;
}

public static class FieldVisibilityScopes
{
    public const string General = "general";
    public const string Selling = "selling";
    public const string Cost = "cost";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        General,
        Selling,
        Cost,
    };
}

public static class CalculatorLimits
{
    public const int TemplateNameMaxLength = 200;
    public const int DescriptionMaxLength = 8000;
    public const int MaxElements = 200;
    public const int MaxColumns = 40;
    public const int MaxOptions = 100;
    public const int MaxFormulaLength = 4000;
    public const int MaxLabelLength = 200;
    public const int MaxElementIdLength = 100;
    public const int MaxFieldKeyLength = 64;
    public const int MaxOptionValueLength = 100;
    public const int MaxDecimalPlaces = 8;
}
