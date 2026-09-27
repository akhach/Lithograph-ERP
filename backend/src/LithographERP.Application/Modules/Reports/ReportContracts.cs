using System.Text.Json.Serialization;

namespace LithographERP.Application.Modules.Reports;

public static class ReportErrorCodes
{
    public const string InvalidFilter = "REPORT_INVALID_FILTER";
    public const string InvalidSort = "REPORT_INVALID_SORT";
    public const string DateRangeInvalid = "REPORT_DATE_RANGE_INVALID";
}

public sealed record ReportFinancialAccess(bool SellingPrice, bool CostPrice);

public sealed record ReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? ClientId,
    Guid? ProjectId,
    Guid? OrderId,
    Guid? OrderTypeId,
    string? OrderStatus,
    string? ProjectStatus,
    string? Priority,
    Guid? OwnerEmployeeId,
    Guid? AssigneeEmployeeId,
    string? Search,
    bool IncludeCancelled,
    string? Category,
    string? Supplier,
    DateOnly? FromExpenseDate,
    DateOnly? ToExpenseDate,
    string GroupBy,
    int Page,
    int PageSize,
    string Sort,
    bool Descending);

public sealed record ReportClientOption(Guid Id, string BusinessId, string Name, bool IsActive);

public sealed record ReportProjectOption(Guid Id, string BusinessId, string Name, Guid ClientId, string Status);

public sealed record ReportOrderTypeOption(Guid Id, string Name, bool IsActive);

public sealed record ReportEmployeeOption(Guid Id, string FullName);

public sealed record ReportOrderOption(Guid Id, string BusinessId, string Name, Guid ProjectId);

public sealed record ReportFilterOptions(
    IReadOnlyList<ReportClientOption> Clients,
    IReadOnlyList<ReportProjectOption> Projects,
    IReadOnlyList<ReportOrderTypeOption> OrderTypes,
    IReadOnlyList<ReportEmployeeOption> Employees,
    IReadOnlyList<ReportOrderOption> Orders);

public sealed record ReportPage<TItem, TSummary>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    TSummary Summary);

public sealed record OrderReportRow(
    Guid OrderId,
    string OrderBusinessId,
    string OrderName,
    Guid ClientId,
    string ClientBusinessId,
    string ClientName,
    Guid ProjectId,
    string ProjectBusinessId,
    string ProjectName,
    Guid OrderTypeId,
    string OrderTypeName,
    bool OrderTypeIsActive,
    string Status,
    string Priority,
    DateOnly? Deadline,
    DateTimeOffset CreatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostPrice,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record OrderReportSummary(
    int TotalOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ProfitTotal);

public sealed record ProjectReportRow(
    Guid ProjectId,
    string ProjectBusinessId,
    string ProjectName,
    Guid ClientId,
    string ClientBusinessId,
    string ClientName,
    string Status,
    Guid? OwnerEmployeeId,
    string? OwnerName,
    Guid? AssigneeEmployeeId,
    string? AssigneeName,
    int OrderCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record ProjectReportSummary(
    int TotalProjects,
    int TotalOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ProfitTotal);

public sealed record ClientReportRow(
    Guid ClientId,
    string ClientBusinessId,
    string ClientName,
    bool IsActive,
    int ProjectCount,
    int OrderCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record ClientReportSummary(
    int TotalClients,
    int TotalProjects,
    int TotalOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ProfitTotal);

public sealed record OrderTypeReportRow(
    Guid OrderTypeId,
    string OrderTypeName,
    bool IsActive,
    int OrderCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? Profit);

public sealed record OrderTypeReportSummary(
    int TotalOrderTypes,
    int TotalOrders,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? SellingTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostTotal,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ProfitTotal);

public sealed record CostReportRow(
    string GroupKey,
    string? Label,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? OrderId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OrderBusinessId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OrderName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ProjectId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProjectBusinessId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProjectName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ClientId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClientBusinessId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ClientName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? OrderTypeId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OrderTypeName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? OrderTypeIsActive,
    int CostItemCount,
    decimal TotalAmount);

public sealed record CostReportSummary(int TotalGroups, int CostItemCount, decimal TotalAmount);

public interface IReportService
{
    Task<ReportFilterOptions> OptionsAsync(CancellationToken cancellationToken = default);

    Task<ReportPage<OrderReportRow, OrderReportSummary>> OrdersAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<ReportPage<ProjectReportRow, ProjectReportSummary>> ProjectsAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<ReportPage<ClientReportRow, ClientReportSummary>> ClientsAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<ReportPage<OrderTypeReportRow, OrderTypeReportSummary>> OrderTypesAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default);

    Task<ReportPage<CostReportRow, CostReportSummary>> CostsAsync(
        ReportQuery query,
        CancellationToken cancellationToken = default);
}
