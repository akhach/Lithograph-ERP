using System.Globalization;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Reports;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;

namespace LithographERP.Infrastructure.Modules.Reports;

public static class ReportQueryParser
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 200;

    private static readonly string[] OrderSorts =
    [
        "order_business_id", "order_name", "client_name", "project_name", "order_type",
        "status", "priority", "deadline", "created_at", "selling_price", "cost_price", "profit",
    ];

    private static readonly string[] ProjectSorts =
    [
        "project_business_id", "project_name", "client_name", "status", "order_count",
        "selling_total", "cost_total", "profit",
    ];

    private static readonly string[] ClientSorts =
    [
        "client_business_id", "client_name", "project_count", "order_count",
        "selling_total", "cost_total", "profit",
    ];

    private static readonly string[] OrderTypeSorts =
    [
        "order_type", "order_count", "selling_total", "cost_total", "profit",
    ];

    private static readonly string[] CostGroups =
    [
        "category", "supplier", "order", "project", "client", "order_type",
    ];

    public static ReportQuery Orders(
        string? fromDate,
        string? toDate,
        Guid? clientId,
        Guid? projectId,
        Guid? orderTypeId,
        string? status,
        string? priority,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        string? search,
        bool? includeCancelled,
        int? page,
        int? pageSize,
        string? sort,
        string? direction) =>
        Parse(
            fromDate,
            toDate,
            clientId,
            projectId,
            orderId: null,
            orderTypeId,
            status,
            projectStatus: null,
            priority,
            ownerEmployeeId,
            assigneeEmployeeId,
            search,
            includeCancelled,
            category: null,
            supplier: null,
            fromExpenseDate: null,
            toExpenseDate: null,
            groupBy: "category",
            page,
            pageSize,
            sort,
            direction,
            OrderSorts,
            defaultSort: "created_at",
            defaultDescending: true);

    public static ReportQuery Projects(
        string? fromDate,
        string? toDate,
        Guid? clientId,
        string? projectStatus,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        Guid? orderTypeId,
        string? status,
        bool? includeCancelled,
        int? page,
        int? pageSize,
        string? sort,
        string? direction) =>
        Parse(
            fromDate,
            toDate,
            clientId,
            projectId: null,
            orderId: null,
            orderTypeId,
            status,
            projectStatus,
            priority: null,
            ownerEmployeeId,
            assigneeEmployeeId,
            search: null,
            includeCancelled,
            category: null,
            supplier: null,
            fromExpenseDate: null,
            toExpenseDate: null,
            groupBy: "category",
            page,
            pageSize,
            sort,
            direction,
            ProjectSorts,
            defaultSort: "project_name",
            defaultDescending: false);

    public static ReportQuery Clients(
        string? fromDate,
        string? toDate,
        Guid? clientId,
        string? projectStatus,
        Guid? orderTypeId,
        string? status,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        bool? includeCancelled,
        int? page,
        int? pageSize,
        string? sort,
        string? direction) =>
        Parse(
            fromDate,
            toDate,
            clientId,
            projectId: null,
            orderId: null,
            orderTypeId,
            status,
            projectStatus,
            priority: null,
            ownerEmployeeId,
            assigneeEmployeeId,
            search: null,
            includeCancelled,
            category: null,
            supplier: null,
            fromExpenseDate: null,
            toExpenseDate: null,
            groupBy: "category",
            page,
            pageSize,
            sort,
            direction,
            ClientSorts,
            defaultSort: "client_name",
            defaultDescending: false);

    public static ReportQuery OrderTypes(
        string? fromDate,
        string? toDate,
        Guid? clientId,
        Guid? projectId,
        Guid? orderTypeId,
        string? status,
        string? priority,
        bool? includeCancelled,
        int? page,
        int? pageSize,
        string? sort,
        string? direction) =>
        Parse(
            fromDate,
            toDate,
            clientId,
            projectId,
            orderId: null,
            orderTypeId,
            status,
            projectStatus: null,
            priority,
            ownerEmployeeId: null,
            assigneeEmployeeId: null,
            search: null,
            includeCancelled,
            category: null,
            supplier: null,
            fromExpenseDate: null,
            toExpenseDate: null,
            groupBy: "category",
            page,
            pageSize,
            sort,
            direction,
            OrderTypeSorts,
            defaultSort: "order_type",
            defaultDescending: false);

    public static ReportQuery Costs(
        string? fromExpenseDate,
        string? toExpenseDate,
        Guid? clientId,
        Guid? projectId,
        Guid? orderId,
        Guid? orderTypeId,
        string? category,
        string? supplier,
        string? status,
        bool? includeCancelled,
        string? groupBy,
        int? page,
        int? pageSize,
        string? sort,
        string? direction)
    {
        var group = string.IsNullOrWhiteSpace(groupBy) ? "category" : groupBy.Trim().ToLowerInvariant();
        if (!CostGroups.Contains(group, StringComparer.Ordinal))
        {
            throw InvalidFilter("group_by", "Group must be category, supplier, order, project, client, or order_type.");
        }

        var allowedSorts = group switch
        {
            "order" => new[] { "label", "cost_item_count", "total_amount", "order_business_id", "order_name", "project_name", "client_name" },
            "project" => new[] { "label", "cost_item_count", "total_amount", "project_business_id", "project_name", "client_name" },
            "client" => new[] { "label", "cost_item_count", "total_amount", "client_business_id", "client_name" },
            "order_type" => new[] { "label", "cost_item_count", "total_amount", "order_type" },
            _ => new[] { "label", "cost_item_count", "total_amount" },
        };

        return Parse(
            fromDate: null,
            toDate: null,
            clientId,
            projectId,
            orderId,
            orderTypeId,
            status,
            projectStatus: null,
            priority: null,
            ownerEmployeeId: null,
            assigneeEmployeeId: null,
            search: null,
            includeCancelled,
            category,
            supplier,
            fromExpenseDate,
            toExpenseDate,
            group,
            page,
            pageSize,
            sort,
            direction,
            allowedSorts,
            defaultSort: "total_amount",
            defaultDescending: true);
    }

    public static void RequireFinancialSort(string sort, ReportFinancialAccess access)
    {
        var needsSelling = sort is "selling_price" or "selling_total";
        var needsCost = sort is "cost_price" or "cost_total";
        var needsProfit = sort is "profit";
        if ((needsSelling && !access.SellingPrice)
            || (needsCost && !access.CostPrice)
            || (needsProfit && !(access.SellingPrice && access.CostPrice)))
        {
            throw new AuthException(
                AuthErrorCodes.PermissionDenied,
                "You do not have permission to access this report data.",
                403);
        }
    }

    private static ReportQuery Parse(
        string? fromDate,
        string? toDate,
        Guid? clientId,
        Guid? projectId,
        Guid? orderId,
        Guid? orderTypeId,
        string? status,
        string? projectStatus,
        string? priority,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        string? search,
        bool? includeCancelled,
        string? category,
        string? supplier,
        string? fromExpenseDate,
        string? toExpenseDate,
        string groupBy,
        int? page,
        int? pageSize,
        string? sort,
        string? direction,
        string[] allowedSorts,
        string defaultSort,
        bool defaultDescending)
    {
        if (page is < 1)
        {
            throw AuthException.Validation("page", "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw AuthException.Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var parsedFrom = ParseDate(fromDate, "from_date");
        var parsedTo = ParseDate(toDate, "to_date");
        if (parsedFrom is not null && parsedTo is not null && parsedFrom > parsedTo)
        {
            throw DateRange();
        }

        var parsedExpenseFrom = ParseDate(fromExpenseDate, "from_expense_date");
        var parsedExpenseTo = ParseDate(toExpenseDate, "to_expense_date");
        if (parsedExpenseFrom is not null && parsedExpenseTo is not null && parsedExpenseFrom > parsedExpenseTo)
        {
            throw DateRange();
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OrderStatuses.TryParse(status, out normalizedStatus))
            {
                throw InvalidFilter("status", "Status is not recognized.");
            }
        }

        string? normalizedProjectStatus = null;
        if (!string.IsNullOrWhiteSpace(projectStatus))
        {
            if (!ProjectStatuses.TryParse(projectStatus, out normalizedProjectStatus))
            {
                throw InvalidFilter("project_status", "Project status is not recognized.");
            }
        }

        string? normalizedPriority = null;
        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!OrderPriorities.TryParse(priority, out normalizedPriority))
            {
                throw InvalidFilter("priority", "Priority is not recognized.");
            }
        }

        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? defaultSort : sort.Trim().ToLowerInvariant();
        if (!allowedSorts.Contains(normalizedSort, StringComparer.Ordinal))
        {
            throw new AuthException(
                ReportErrorCodes.InvalidSort,
                "The selected report sort field is not supported.",
                400);
        }

        var normalizedDirection = string.IsNullOrWhiteSpace(direction)
            ? (defaultDescending ? "desc" : "asc")
            : direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
        {
            throw InvalidFilter("direction", "Direction must be asc or desc.");
        }

        return new ReportQuery(
            parsedFrom,
            parsedTo,
            clientId,
            projectId,
            orderId,
            orderTypeId,
            normalizedStatus,
            normalizedProjectStatus,
            normalizedPriority,
            ownerEmployeeId,
            assigneeEmployeeId,
            NormalizeText(search, "search"),
            includeCancelled ?? false,
            NormalizeText(category, "category"),
            NormalizeText(supplier, "supplier"),
            parsedExpenseFrom,
            parsedExpenseTo,
            groupBy,
            page ?? 1,
            pageSize ?? DefaultPageSize,
            normalizedSort,
            normalizedDirection == "desc");
    }

    private static DateOnly? ParseDate(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw InvalidFilter(field, "Date must use yyyy-MM-dd.");
        }

        return date;
    }

    private static string? NormalizeText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            throw InvalidFilter(field, "Filter text cannot be longer than 200 characters.");
        }

        return trimmed;
    }

    private static AuthException InvalidFilter(string field, string message) =>
        new(
            ReportErrorCodes.InvalidFilter,
            "One or more report filters are invalid.",
            400,
            new Dictionary<string, string[]> { [field] = [message] });

    private static AuthException DateRange() =>
        new(
            ReportErrorCodes.DateRangeInvalid,
            "The start date cannot be after the end date.",
            400);
}
