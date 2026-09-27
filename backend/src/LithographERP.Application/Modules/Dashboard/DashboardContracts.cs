using System.Text.Json.Serialization;

namespace LithographERP.Application.Modules.Dashboard;

public sealed record DashboardResponse(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DashboardSummary? Summary,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardOrderRow>? ActiveOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardOrderRow>? UrgentOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardOrderRow>? DueSoonOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardOrderRow>? OverdueOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardOrderRow>? RecentOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<DashboardProjectRow>? RecentProjects);

public sealed record DashboardSummary(int ActiveOrders, int UrgentOrders, int DueSoonOrders, int OverdueOrders);

public sealed record DashboardOrderRow(
    Guid Id,
    string BusinessId,
    string Name,
    string ClientName,
    string ProjectName,
    string OrderTypeName,
    string Status,
    string Priority,
    DateOnly? Deadline,
    DateTimeOffset CreatedAt);

public sealed record DashboardProjectRow(
    Guid Id,
    string BusinessId,
    string Name,
    string ClientName,
    string Status,
    string? OwnerName);

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(
        bool includeOrders,
        bool includeProjects,
        CancellationToken cancellationToken = default);
}
