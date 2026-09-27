using System.Text.Json;

namespace LithographERP.Application.Modules.Calculator;

public sealed record OrderCalculatorAccess(bool SellingPrice, bool Costs);

public sealed record OrderCalculatorTemplateSummary(Guid Id, string Name);

public sealed record OrderCalculatorVersionSummary(Guid Id, int VersionNumber);

public sealed record OrderCalculatorOption(string Value, string Label, decimal? NumericValue);

public sealed record OrderCalculatorColumn(
    string Key,
    string Label,
    string Type,
    string Visibility,
    IReadOnlyList<OrderCalculatorOption>? Options);

public sealed record OrderCalculatorElement(
    string Id,
    string Type,
    string? Key,
    string? Label,
    string? Visibility,
    decimal? DefaultValue,
    decimal? Min,
    decimal? Max,
    int? DecimalPlaces,
    string? DefaultText,
    bool? DefaultChecked,
    IReadOnlyList<OrderCalculatorOption>? Options,
    IReadOnlyList<OrderCalculatorColumn>? Columns);

public sealed record OrderCalculatorFieldError(string FieldKey, string Code, string Message);

public sealed record OrderCalculatorRuntime(
    Guid OrderId,
    Guid CalculatorId,
    OrderCalculatorTemplateSummary Template,
    OrderCalculatorVersionSummary TemplateVersion,
    string? SellingPriceFieldKey,
    IReadOnlyList<OrderCalculatorElement> Elements,
    IReadOnlyDictionary<string, JsonElement> Values,
    IReadOnlyDictionary<string, JsonElement> CalculatedValues,
    decimal? SellingPrice,
    bool CalculationComplete,
    IReadOnlyList<OrderCalculatorFieldError> FieldErrors,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastCalculatedAt);

public sealed record SaveOrderCalculatorRequest(
    JsonElement FieldValues,
    DateTimeOffset UpdatedAt);

public sealed record ResetOrderCalculatorRequest(DateTimeOffset UpdatedAt);

public sealed record ChangeOrderTypeRequest(Guid OrderTypeId, bool ResetCalculator, DateTimeOffset? UpdatedAt);

public interface IOrderCalculatorService
{
    Task<OrderCalculatorRuntime> GetAsync(
        Guid actorId,
        Guid orderId,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default);

    Task<OrderCalculatorRuntime> SaveAsync(
        Guid actorId,
        Guid orderId,
        SaveOrderCalculatorRequest request,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default);

    Task<OrderCalculatorRuntime> ResetAsync(
        Guid actorId,
        Guid orderId,
        ResetOrderCalculatorRequest request,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default);

    Task ChangeOrderTypeAsync(
        Guid actorId,
        Guid orderId,
        ChangeOrderTypeRequest request,
        CancellationToken cancellationToken = default);
}
