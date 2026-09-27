using System.Text.Json.Serialization;

namespace LithographERP.Application.Modules.Orders;

public static class OrderErrorCodes
{
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderTypeNotFound = "ORDER_TYPE_NOT_FOUND";
    public const string OrderTypeInactive = "ORDER_TYPE_INACTIVE";
    public const string OrderTypeNameAlreadyExists = "ORDER_TYPE_NAME_ALREADY_EXISTS";
    public const string ProjectChangeNotAllowed = "ORDER_PROJECT_CHANGE_NOT_ALLOWED";
    public const string ChecklistItemNotFound = "CHECKLIST_ITEM_NOT_FOUND";
    public const string FolderLinkNotFound = "FOLDER_LINK_NOT_FOUND";
    public const string BusinessIdConflict = "ORDER_BUSINESS_ID_CONFLICT";
}

public sealed record OrderFinancialAccess(bool SellingPrice, bool CostPrice);

public sealed record OrderProjectSummary(Guid Id, string BusinessId, string Name);

public sealed record OrderProjectContext(Guid Id, string BusinessId, string Name, string Status, DateOnly? Deadline);

public sealed record OrderClientSummary(Guid Id, string BusinessId, string Name);

public sealed record OrderTypeSummary(Guid Id, string Name, bool IsActive);

public sealed record OrderEmployeeSummary(Guid Id, string FullName, string? Position, bool IsActive);

public sealed record OrderTeam(
    OrderEmployeeSummary? Owner,
    OrderEmployeeSummary? Assignee,
    IReadOnlyList<OrderEmployeeSummary> Participants,
    IReadOnlyList<OrderEmployeeSummary> Observers);

public sealed record ChecklistItemResponse(Guid Id, string Text, bool IsCompleted, int SortOrder);

public sealed record ChecklistProgressResponse(int Completed, int Total);

public sealed record FolderLinkResponse(Guid Id, string? Name, string Path, int SortOrder);

public sealed record OrderListItem(
    Guid Id,
    string BusinessId,
    string Name,
    OrderProjectSummary Project,
    OrderClientSummary Client,
    OrderTypeSummary OrderType,
    string Status,
    string Priority,
    DateOnly? Deadline,
    string? PreviewImagePath,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record OrderDetail(
    Guid Id,
    string BusinessId,
    OrderProjectContext Project,
    OrderClientSummary Client,
    OrderTypeSummary OrderType,
    string Name,
    string? Description,
    string Status,
    string Priority,
    DateOnly? Deadline,
    string? PreviewImagePath,
    OrderTeam Team,
    bool CalculatorConfigured,
    ChecklistProgressResponse ChecklistProgress,
    IReadOnlyList<ChecklistItemResponse> ChecklistItems,
    IReadOnlyList<FolderLinkResponse> FolderLinks,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record OrderPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record OrderListRequest(
    string? Search,
    Guid? ProjectId,
    Guid? ClientId,
    Guid? OrderTypeId,
    string? Status,
    string? Priority,
    Guid? OwnerEmployeeId,
    Guid? AssigneeEmployeeId,
    DateOnly? DeadlineFrom,
    DateOnly? DeadlineTo,
    int Page,
    int PageSize,
    string Sort,
    bool Descending);

public sealed record SaveOrderRequest(
    Guid ProjectId,
    Guid OrderTypeId,
    string Name,
    string? Description,
    string? Priority,
    DateOnly? Deadline,
    string? PreviewImagePath);

public sealed record OrderTypeResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SaveOrderTypeRequest(string Name, string? Description);

public sealed record SaveChecklistItemRequest(string Text, int? SortOrder);

public sealed record UpdateChecklistItemRequest(string? Text, bool? IsCompleted);

public sealed record SaveFolderLinkRequest(string? Name, string Path);

public sealed record ReorderRequest(IReadOnlyList<Guid> Ids);

public interface IOrderTypeAdminService
{
    Task<IReadOnlyList<OrderTypeResponse>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<OrderTypeResponse> GetAsync(Guid orderTypeId, CancellationToken cancellationToken = default);

    Task<OrderTypeResponse> CreateAsync(Guid actorId, SaveOrderTypeRequest request, CancellationToken cancellationToken = default);

    Task<OrderTypeResponse> UpdateAsync(Guid actorId, Guid orderTypeId, SaveOrderTypeRequest request, CancellationToken cancellationToken = default);

    Task<OrderTypeResponse> SetActiveAsync(Guid actorId, Guid orderTypeId, bool isActive, CancellationToken cancellationToken = default);
}

public interface IOrderAdminService
{
    Task<OrderPage<OrderListItem>> ListAsync(
        OrderListRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<OrderDetail> GetAsync(Guid orderId, OrderFinancialAccess access, CancellationToken cancellationToken = default);

    Task<OrderDetail> CreateAsync(
        Guid actorId,
        SaveOrderRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<OrderDetail> UpdateAsync(
        Guid actorId,
        Guid orderId,
        SaveOrderRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<OrderDetail> ChangeStatusAsync(
        Guid actorId,
        Guid orderId,
        string status,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChecklistItemResponse>> ListChecklistAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<ChecklistItemResponse> AddChecklistItemAsync(
        Guid orderId,
        SaveChecklistItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ChecklistItemResponse> UpdateChecklistItemAsync(
        Guid orderId,
        Guid itemId,
        UpdateChecklistItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteChecklistItemAsync(Guid orderId, Guid itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChecklistItemResponse>> ReorderChecklistAsync(
        Guid orderId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderLinkResponse>> ListFolderLinksAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<FolderLinkResponse> AddFolderLinkAsync(
        Guid orderId,
        SaveFolderLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<FolderLinkResponse> UpdateFolderLinkAsync(
        Guid orderId,
        Guid linkId,
        SaveFolderLinkRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteFolderLinkAsync(Guid orderId, Guid linkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FolderLinkResponse>> ReorderFolderLinksAsync(
        Guid orderId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default);
}
