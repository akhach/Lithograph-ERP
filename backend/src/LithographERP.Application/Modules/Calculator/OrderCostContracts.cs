namespace LithographERP.Application.Modules.Calculator;

public static class CostErrorCodes
{
    public const string ItemNotFound = "COST_ITEM_NOT_FOUND";
    public const string AmountInvalid = "COST_AMOUNT_INVALID";
    public const string MutationNotAllowed = "COST_MUTATION_NOT_ALLOWED";
}

public sealed record CostItemResponse(
    Guid Id,
    string Category,
    string? Supplier,
    DateOnly? ExpenseDate,
    string? Description,
    decimal Amount,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CostItemListResponse(IReadOnlyList<CostItemResponse> Items, decimal TotalCost);

public sealed record SaveCostItemRequest(
    string? Category,
    string? Supplier,
    DateOnly? ExpenseDate,
    string? Description,
    string? Amount,
    bool AmountSupplied,
    int? SortOrder);

public sealed record UpdateCostItemRequest(
    string? Category,
    string? Supplier,
    DateOnly? ExpenseDate,
    string? Description,
    string? Amount,
    bool AmountSupplied);

public interface IOrderCostService
{
    Task<CostItemListResponse> ListAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<CostItemResponse> CreateAsync(
        Guid actorId,
        Guid orderId,
        SaveCostItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CostItemResponse> UpdateAsync(
        Guid actorId,
        Guid orderId,
        Guid costItemId,
        UpdateCostItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid actorId, Guid orderId, Guid costItemId, CancellationToken cancellationToken = default);

    Task<CostItemListResponse> ReorderAsync(
        Guid actorId,
        Guid orderId,
        IReadOnlyList<Guid>? ids,
        CancellationToken cancellationToken = default);
}
