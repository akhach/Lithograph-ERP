using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Reports;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Reports;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = PermissionCatalog.Reports.View)]
public sealed class ReportsController(IReportService reports, IAuthService auth) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ReportFilterOptions> Options(CancellationToken cancellationToken) =>
        await reports.OptionsAsync(cancellationToken);

    [HttpGet("orders")]
    public async Task<ReportPage<OrderReportRow, OrderReportSummary>> Orders(
        [FromQuery(Name = "from_date")] string? fromDate,
        [FromQuery(Name = "to_date")] string? toDate,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "project_id")] Guid? projectId,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery(Name = "owner_employee_id")] Guid? ownerEmployeeId,
        [FromQuery(Name = "assignee_employee_id")] Guid? assigneeEmployeeId,
        [FromQuery] string? search,
        [FromQuery(Name = "include_cancelled")] bool? includeCancelled,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(cancellationToken);
        RejectFinancialFilters(access);
        var query = ReportQueryParser.Orders(
            fromDate,
            toDate,
            clientId,
            projectId,
            orderTypeId,
            status,
            priority,
            ownerEmployeeId,
            assigneeEmployeeId,
            search,
            includeCancelled,
            page,
            pageSize,
            sort,
            direction);
        return await reports.OrdersAsync(query, access, cancellationToken);
    }

    [HttpGet("projects")]
    public async Task<ReportPage<ProjectReportRow, ProjectReportSummary>> Projects(
        [FromQuery(Name = "from_date")] string? fromDate,
        [FromQuery(Name = "to_date")] string? toDate,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "project_status")] string? projectStatus,
        [FromQuery(Name = "owner_employee_id")] Guid? ownerEmployeeId,
        [FromQuery(Name = "assignee_employee_id")] Guid? assigneeEmployeeId,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? status,
        [FromQuery(Name = "include_cancelled")] bool? includeCancelled,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(cancellationToken);
        RejectFinancialFilters(access);
        var query = ReportQueryParser.Projects(
            fromDate,
            toDate,
            clientId,
            projectStatus,
            ownerEmployeeId,
            assigneeEmployeeId,
            orderTypeId,
            status,
            includeCancelled,
            page,
            pageSize,
            sort,
            direction);
        return await reports.ProjectsAsync(query, access, cancellationToken);
    }

    [HttpGet("clients")]
    public async Task<ReportPage<ClientReportRow, ClientReportSummary>> Clients(
        [FromQuery(Name = "from_date")] string? fromDate,
        [FromQuery(Name = "to_date")] string? toDate,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "project_status")] string? projectStatus,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? status,
        [FromQuery(Name = "owner_employee_id")] Guid? ownerEmployeeId,
        [FromQuery(Name = "assignee_employee_id")] Guid? assigneeEmployeeId,
        [FromQuery(Name = "include_cancelled")] bool? includeCancelled,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(cancellationToken);
        RejectFinancialFilters(access);
        var query = ReportQueryParser.Clients(
            fromDate,
            toDate,
            clientId,
            projectStatus,
            orderTypeId,
            status,
            ownerEmployeeId,
            assigneeEmployeeId,
            includeCancelled,
            page,
            pageSize,
            sort,
            direction);
        return await reports.ClientsAsync(query, access, cancellationToken);
    }

    [HttpGet("order-types")]
    public async Task<ReportPage<OrderTypeReportRow, OrderTypeReportSummary>> OrderTypes(
        [FromQuery(Name = "from_date")] string? fromDate,
        [FromQuery(Name = "to_date")] string? toDate,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "project_id")] Guid? projectId,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery(Name = "include_cancelled")] bool? includeCancelled,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var access = await AccessAsync(cancellationToken);
        RejectFinancialFilters(access);
        var query = ReportQueryParser.OrderTypes(
            fromDate,
            toDate,
            clientId,
            projectId,
            orderTypeId,
            status,
            priority,
            includeCancelled,
            page,
            pageSize,
            sort,
            direction);
        return await reports.OrderTypesAsync(query, access, cancellationToken);
    }

    [HttpGet("costs")]
    public async Task<ReportPage<CostReportRow, CostReportSummary>> Costs(
        [FromQuery(Name = "from_expense_date")] string? fromExpenseDate,
        [FromQuery(Name = "to_expense_date")] string? toExpenseDate,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "project_id")] Guid? projectId,
        [FromQuery(Name = "order_id")] Guid? orderId,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? category,
        [FromQuery] string? supplier,
        [FromQuery] string? status,
        [FromQuery(Name = "include_cancelled")] bool? includeCancelled,
        [FromQuery(Name = "group_by")] string? groupBy,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        if (!user.Permissions.Contains(PermissionCatalog.Calculator.ViewCosts, StringComparer.Ordinal))
        {
            throw new AuthException(
                AuthErrorCodes.PermissionDenied,
                "You do not have permission to access this report data.",
                StatusCodes.Status403Forbidden);
        }

        var access = new ReportFinancialAccess(
            user.Permissions.Contains(PermissionCatalog.Orders.ViewSellingPrice, StringComparer.Ordinal),
            user.Permissions.Contains(PermissionCatalog.Orders.ViewCostPrice, StringComparer.Ordinal));
        RejectFinancialFilters(access);
        var query = ReportQueryParser.Costs(
            fromExpenseDate,
            toExpenseDate,
            clientId,
            projectId,
            orderId,
            orderTypeId,
            category,
            supplier,
            status,
            includeCancelled,
            groupBy,
            page,
            pageSize,
            sort,
            direction);
        return await reports.CostsAsync(query, cancellationToken);
    }

    private async Task<ReportFinancialAccess> AccessAsync(CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        return new ReportFinancialAccess(
            user.Permissions.Contains(PermissionCatalog.Orders.ViewSellingPrice, StringComparer.Ordinal),
            user.Permissions.Contains(PermissionCatalog.Orders.ViewCostPrice, StringComparer.Ordinal));
    }

    private void RejectFinancialFilters(ReportFinancialAccess access)
    {
        foreach (var key in Request.Query.Keys)
        {
            var name = key.ToLowerInvariant();
            var kind = name switch
            {
                "selling_price" or "min_selling_price" or "max_selling_price" => "selling",
                "cost_price" or "min_cost_price" or "max_cost_price" => "cost",
                "profit" or "min_profit" or "max_profit" => "profit",
                _ => null,
            };
            if (kind is null)
            {
                continue;
            }

            var allowed = kind switch
            {
                "selling" => access.SellingPrice,
                "cost" => access.CostPrice,
                _ => access.SellingPrice && access.CostPrice,
            };
            if (!allowed)
            {
                throw new AuthException(
                    AuthErrorCodes.PermissionDenied,
                    "You do not have permission to access this report data.",
                    StatusCodes.Status403Forbidden);
            }

            throw new AuthException(
                ReportErrorCodes.InvalidFilter,
                "One or more report filters are invalid.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]> { [name] = ["This financial filter is not supported."] });
        }
    }
}
