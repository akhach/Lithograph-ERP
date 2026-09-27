using System.Text.Json.Serialization;
using LithographERP.Application.Modules.Calculator;

namespace LithographERP.Api.Errors;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<CalculatorValidationIssue>? Validation = null);
