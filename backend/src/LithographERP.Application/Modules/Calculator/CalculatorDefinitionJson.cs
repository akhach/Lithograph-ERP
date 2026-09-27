using System.Text.Json;
using System.Text.Json.Serialization;
using LithographERP.Domain.Modules.Calculator;

namespace LithographERP.Application.Modules.Calculator;

public static class CalculatorDefinitionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(CalculatorTemplateDefinition definition) =>
        JsonSerializer.Serialize(definition, Options);

    public static CalculatorTemplateDefinition Deserialize(string json)
    {
        return JsonSerializer.Deserialize<CalculatorTemplateDefinition>(json, Options)
            ?? throw new JsonException("Calculator definition is empty.");
    }

    public static CalculatorTemplateDefinition Clone(CalculatorTemplateDefinition definition) =>
        Deserialize(Serialize(definition));
}
