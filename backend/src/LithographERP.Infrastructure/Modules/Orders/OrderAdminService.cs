using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Numbering;
using LithographERP.Application.Modules.Orders;
using LithographERP.Application.Modules.Projects;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using LithographERP.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Orders;

public sealed class OrderAdminService(
    LithographDbContext db,
    IBusinessIdGenerator businessIds,
    TimeProvider time,
    ILogger<OrderAdminService> logger) : IOrderAdminService
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 200;
    private const int ChecklistTextMaxLength = 500;
    private const int PathMaxLength = 2000;
    private const int PreviewPathMaxLength = 2000;

    public async Task<OrderPage<OrderListItem>> ListAsync(
        OrderListRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        RequireFinancialSort(request.Sort, access);
        var query = Filtered(request);
        var total = await query.CountAsync(cancellationToken);
        var rows = await Ordered(query, request)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new OrderListRow(
                order.Id,
                order.BusinessId,
                order.Name,
                order.Project.Id,
                order.Project.BusinessId,
                order.Project.Name,
                order.Project.Client.Id,
                order.Project.Client.BusinessId,
                order.Project.Client.Name,
                order.OrderType.Id,
                order.OrderType.Name,
                order.OrderType.IsActive,
                order.Status,
                order.Priority,
                order.Deadline,
                order.PreviewImagePath,
                order.SellingPrice,
                order.CostPrice))
            .ToListAsync(cancellationToken);
        return Page(rows.Select(row => ToListItem(row, access)).ToArray(), request, total);
    }

    public Task<OrderDetail> GetAsync(Guid orderId, OrderFinancialAccess access, CancellationToken cancellationToken = default) =>
        DetailAsync(orderId, access, cancellationToken);

    public async Task<OrderDetail> CreateAsync(
        Guid actorId,
        SaveOrderRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        var name = RequireName(request.Name);
        var priority = request.Priority is null || string.IsNullOrWhiteSpace(request.Priority)
            ? OrderPriorities.Normal
            : RequirePriority(request.Priority);
        await RequireOpenProjectAsync(request.ProjectId, cancellationToken);
        await RequireSelectableOrderTypeAsync(request.OrderTypeId, cancellationToken);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = await businessIds.GenerateOrderBusinessIdAsync(cancellationToken),
            ProjectId = request.ProjectId,
            OrderTypeId = request.OrderTypeId,
            Name = name,
            Description = Text(request.Description),
            Status = OrderStatuses.Draft,
            Priority = priority,
            SellingPrice = 0,
            CostPrice = 0,
            Deadline = request.Deadline,
            PreviewImagePath = RequirePreviewPath(request.PreviewImagePath),
            CreatedAt = time.GetUtcNow(),
            CreatedBy = actorId,
        };
        db.Orders.Add(order);
        await SaveOrderAsync(cancellationToken);
        return await DetailAsync(order.Id, access, cancellationToken);
    }

    public async Task<OrderDetail> UpdateAsync(
        Guid actorId,
        Guid orderId,
        SaveOrderRequest request,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(orderId, cancellationToken);
        var name = RequireName(request.Name);
        if (request.ProjectId != order.ProjectId)
        {
            if (order.Status != OrderStatuses.Draft)
            {
                throw new AuthException(
                    OrderErrorCodes.ProjectChangeNotAllowed,
                    "The Project can only be changed while the Order is in Draft status.",
                    409);
            }

            await RequireOpenProjectAsync(request.ProjectId, cancellationToken);
            order.ProjectId = request.ProjectId;
        }

        if (request.OrderTypeId != order.OrderTypeId)
        {
            if (await db.OrderCalculators.AnyAsync(calculator => calculator.OrderId == order.Id, cancellationToken))
            {
                throw new AuthException(
                    OrderErrorCodes.OrderTypeChangeRequiresCalculatorReset,
                    "Changing the Order Type requires an explicit Calculator reset.",
                    409);
            }

            await RequireSelectableOrderTypeAsync(request.OrderTypeId, cancellationToken);
            order.OrderTypeId = request.OrderTypeId;
        }

        order.Name = name;
        order.Description = Text(request.Description);
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            order.Priority = RequirePriority(request.Priority);
        }

        order.Deadline = request.Deadline;
        order.PreviewImagePath = RequirePreviewPath(request.PreviewImagePath);
        Touch(order, actorId);
        await SaveOrderAsync(cancellationToken);
        return await DetailAsync(order.Id, access, cancellationToken);
    }

    public async Task<OrderDetail> ChangeStatusAsync(
        Guid actorId,
        Guid orderId,
        string status,
        OrderFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        if (!OrderStatuses.TryParse(status, out var parsed))
        {
            throw AuthException.Validation("status", "Status is not recognized.");
        }

        var order = await LoadAsync(orderId, cancellationToken);
        if (order.Status != parsed)
        {
            order.Status = parsed;
            Touch(order, actorId);
            await SaveOrderAsync(cancellationToken);
        }

        return await DetailAsync(order.Id, access, cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemResponse>> ListChecklistAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var items = await ChecklistQuery(orderId).ToListAsync(cancellationToken);
        return items.Select(ToChecklist).ToArray();
    }

    public async Task<ChecklistItemResponse> AddChecklistItemAsync(
        Guid orderId,
        SaveChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var sortOrder = request.SortOrder ?? await NextSortOrderAsync(
            db.ChecklistItems.Where(item => item.OrderId == orderId).Select(item => item.SortOrder),
            cancellationToken);
        if (sortOrder < 0)
        {
            throw AuthException.Validation("sortOrder", "Sort order cannot be negative.");
        }

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Text = RequireChecklistText(request.Text),
            IsCompleted = false,
            SortOrder = sortOrder,
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ToChecklist(item);
    }

    public async Task<ChecklistItemResponse> UpdateChecklistItemAsync(
        Guid orderId,
        Guid itemId,
        UpdateChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await LoadChecklistItemAsync(orderId, itemId, cancellationToken);
        if (request.Text is not null)
        {
            item.Text = RequireChecklistText(request.Text);
        }

        if (request.IsCompleted is bool completed)
        {
            item.IsCompleted = completed;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToChecklist(item);
    }

    public async Task DeleteChecklistItemAsync(Guid orderId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await LoadChecklistItemAsync(orderId, itemId, cancellationToken);
        db.ChecklistItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemResponse>> ReorderChecklistAsync(
        Guid orderId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var items = await db.ChecklistItems.Where(item => item.OrderId == orderId).ToListAsync(cancellationToken);
        ApplyOrder(items, ids, item => item.Id, (item, sortOrder) => item.SortOrder = sortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return items.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).Select(ToChecklist).ToArray();
    }

    public async Task<IReadOnlyList<FolderLinkResponse>> ListFolderLinksAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var links = await FolderQuery(orderId).ToListAsync(cancellationToken);
        return links.Select(ToFolderLink).ToArray();
    }

    public async Task<FolderLinkResponse> AddFolderLinkAsync(
        Guid orderId,
        SaveFolderLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var link = new FolderLink
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Name = RequireFolderName(request.Name),
            Path = RequirePath(request.Path),
            SortOrder = await NextSortOrderAsync(
                db.FolderLinks.Where(link => link.OrderId == orderId).Select(link => link.SortOrder),
                cancellationToken),
        };
        db.FolderLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return ToFolderLink(link);
    }

    public async Task<FolderLinkResponse> UpdateFolderLinkAsync(
        Guid orderId,
        Guid linkId,
        SaveFolderLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var link = await LoadFolderLinkAsync(orderId, linkId, cancellationToken);
        link.Name = RequireFolderName(request.Name);
        link.Path = RequirePath(request.Path);
        await db.SaveChangesAsync(cancellationToken);
        return ToFolderLink(link);
    }

    public async Task DeleteFolderLinkAsync(Guid orderId, Guid linkId, CancellationToken cancellationToken = default)
    {
        var link = await LoadFolderLinkAsync(orderId, linkId, cancellationToken);
        db.FolderLinks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderLinkResponse>> ReorderFolderLinksAsync(
        Guid orderId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var links = await db.FolderLinks.Where(link => link.OrderId == orderId).ToListAsync(cancellationToken);
        ApplyOrder(links, ids, link => link.Id, (link, sortOrder) => link.SortOrder = sortOrder);
        await db.SaveChangesAsync(cancellationToken);
        return links.OrderBy(link => link.SortOrder).ThenBy(link => link.Id).Select(ToFolderLink).ToArray();
    }

    public static OrderListRequest Normalize(
        string? search,
        Guid? projectId,
        Guid? clientId,
        Guid? orderTypeId,
        string? status,
        string? priority,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        string? deadlineFrom,
        string? deadlineTo,
        int? page,
        int? pageSize,
        string? sort,
        string? direction)
    {
        if (page is < 1)
        {
            throw AuthException.Validation("page", "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw AuthException.Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = status.Trim().ToLowerInvariant();
            if (normalizedStatus != "open" && !OrderStatuses.TryParse(normalizedStatus, out normalizedStatus))
            {
                throw AuthException.Validation("status", "Status is not recognized.");
            }
        }

        string? normalizedPriority = null;
        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!OrderPriorities.TryParse(priority, out normalizedPriority))
            {
                throw AuthException.Validation("priority", "Priority is not recognized.");
            }
        }

        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "created_at" : sort.Trim().ToLowerInvariant();
        if (normalizedSort is not ("business_id" or "name" or "status" or "priority" or "deadline" or "created_at" or "selling_price" or "cost_price"))
        {
            throw AuthException.Validation(
                "sort",
                "Sort must be business_id, name, status, priority, deadline, created_at, selling_price, or cost_price.");
        }

        var normalizedDirection = string.IsNullOrWhiteSpace(direction) ? "desc" : direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
        {
            throw AuthException.Validation("direction", "Direction must be asc or desc.");
        }

        var parsedDeadlineFrom = ParseDate(deadlineFrom, "deadlineFrom");
        var parsedDeadlineTo = ParseDate(deadlineTo, "deadlineTo");
        if (parsedDeadlineFrom is not null && parsedDeadlineTo is not null && parsedDeadlineTo < parsedDeadlineFrom)
        {
            throw AuthException.Validation("deadlineTo", "The end of the range cannot be before the start.");
        }

        return new OrderListRequest(
            string.IsNullOrWhiteSpace(search) ? null : search,
            projectId,
            clientId,
            orderTypeId,
            normalizedStatus,
            normalizedPriority,
            ownerEmployeeId,
            assigneeEmployeeId,
            parsedDeadlineFrom,
            parsedDeadlineTo,
            page ?? 1,
            pageSize ?? DefaultPageSize,
            normalizedSort,
            normalizedDirection == "desc");
    }

    private IQueryable<Order> Filtered(OrderListRequest request)
    {
        var orders = db.Orders.AsNoTracking();
        if (request.Status == "open")
        {
            orders = orders.Where(order =>
                order.Status == OrderStatuses.Draft
                || order.Status == OrderStatuses.Active
                || order.Status == OrderStatuses.OnHold);
        }
        else if (request.Status is not null)
        {
            orders = orders.Where(order => order.Status == request.Status);
        }

        if (request.ProjectId is not null)
        {
            orders = orders.Where(order => order.ProjectId == request.ProjectId);
        }

        if (request.ClientId is not null)
        {
            orders = orders.Where(order => order.Project.ClientId == request.ClientId);
        }

        if (request.OrderTypeId is not null)
        {
            orders = orders.Where(order => order.OrderTypeId == request.OrderTypeId);
        }

        if (request.Priority is not null)
        {
            orders = orders.Where(order => order.Priority == request.Priority);
        }

        if (request.OwnerEmployeeId is not null)
        {
            orders = orders.Where(order => order.Project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Owner && member.EmployeeId == request.OwnerEmployeeId));
        }

        if (request.AssigneeEmployeeId is not null)
        {
            orders = orders.Where(order => order.Project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Assignee && member.EmployeeId == request.AssigneeEmployeeId));
        }

        if (request.DeadlineFrom is not null)
        {
            orders = orders.Where(order => order.Deadline != null && order.Deadline >= request.DeadlineFrom);
        }

        if (request.DeadlineTo is not null)
        {
            orders = orders.Where(order => order.Deadline != null && order.Deadline <= request.DeadlineTo);
        }

        if (request.Search is not null)
        {
            var pattern = LikePattern(request.Search);
            orders = orders.Where(order =>
                EF.Functions.ILike(order.BusinessId, pattern, "\\")
                || EF.Functions.ILike(order.Name, pattern, "\\")
                || EF.Functions.ILike(order.Project.BusinessId, pattern, "\\")
                || EF.Functions.ILike(order.Project.Name, pattern, "\\")
                || EF.Functions.ILike(order.Project.Client.BusinessId, pattern, "\\")
                || EF.Functions.ILike(order.Project.Client.Name, pattern, "\\"));
        }

        return orders;
    }

    private static IQueryable<Order> Ordered(IQueryable<Order> orders, OrderListRequest request) =>
        request.Sort switch
        {
            "business_id" => Direction(orders, request.Descending, order => order.BusinessId),
            "name" => Direction(orders, request.Descending, order => order.Name),
            "status" => Direction(orders, request.Descending, order => order.Status),
            "priority" => request.Descending
                ? orders.OrderByDescending(order =>
                    order.Priority == OrderPriorities.Urgent ? 4 :
                    order.Priority == OrderPriorities.High ? 3 :
                    order.Priority == OrderPriorities.Normal ? 2 : 1).ThenBy(order => order.Id)
                : orders.OrderBy(order =>
                    order.Priority == OrderPriorities.Urgent ? 4 :
                    order.Priority == OrderPriorities.High ? 3 :
                    order.Priority == OrderPriorities.Normal ? 2 : 1).ThenBy(order => order.Id),
            "deadline" => Direction(orders, request.Descending, order => order.Deadline),
            "created_at" => Direction(orders, request.Descending, order => order.CreatedAt),
            "selling_price" => Direction(orders, request.Descending, order => order.SellingPrice),
            "cost_price" => Direction(orders, request.Descending, order => order.CostPrice),
            _ => orders.OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id),
        };

    private static IOrderedQueryable<Order> Direction<TKey>(
        IQueryable<Order> orders,
        bool descending,
        System.Linq.Expressions.Expression<Func<Order, TKey>> key) =>
        descending
            ? orders.OrderByDescending(key).ThenBy(order => order.Id)
            : orders.OrderBy(key).ThenBy(order => order.Id);

    private async Task<OrderDetail> DetailAsync(Guid orderId, OrderFinancialAccess access, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(candidate => candidate.Project)
            .ThenInclude(project => project.Client)
            .Include(candidate => candidate.Project)
            .ThenInclude(project => project.Members)
            .ThenInclude(member => member.Employee)
            .Include(candidate => candidate.OrderType)
            .Include(candidate => candidate.ChecklistItems)
            .Include(candidate => candidate.FolderLinks)
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw OrderNotFound();
        }

        var calculatorConfigured = await db.OrderCalculators.AnyAsync(
            calculator => calculator.OrderId == order.Id,
            cancellationToken);
        return ToDetail(order, access, calculatorConfigured);
    }

    private async Task<Order> LoadAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw OrderNotFound();
        }

        return order;
    }

    private async Task EnsureOrderExistsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        if (!await db.Orders.AnyAsync(order => order.Id == orderId, cancellationToken))
        {
            throw OrderNotFound();
        }
    }

    private async Task RequireOpenProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .Select(candidate => new { candidate.Id, candidate.Status })
            .SingleOrDefaultAsync(candidate => candidate.Id == projectId, cancellationToken);
        if (project is null)
        {
            throw new AuthException(ProjectErrorCodes.ProjectNotFound, "The Project could not be found.", 404);
        }

        if (!ProjectStatuses.IsOpen(project.Status))
        {
            throw new AuthException(
                ProjectErrorCodes.ProjectClosed,
                "This action is not allowed for a completed or cancelled Project.",
                409);
        }
    }

    private async Task RequireSelectableOrderTypeAsync(Guid orderTypeId, CancellationToken cancellationToken)
    {
        var type = await db.OrderTypes.AsNoTracking()
            .Select(candidate => new { candidate.Id, candidate.IsActive })
            .SingleOrDefaultAsync(candidate => candidate.Id == orderTypeId, cancellationToken);
        if (type is null)
        {
            throw new AuthException(OrderErrorCodes.OrderTypeNotFound, "The Order Type could not be found.", 404);
        }

        if (!type.IsActive)
        {
            throw new AuthException(
                OrderErrorCodes.OrderTypeInactive,
                "Inactive Order Types cannot be selected for new Orders.",
                400);
        }
    }

    private IQueryable<ChecklistItem> ChecklistQuery(Guid orderId) =>
        db.ChecklistItems.AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id);

    private IQueryable<FolderLink> FolderQuery(Guid orderId) =>
        db.FolderLinks.AsNoTracking()
            .Where(link => link.OrderId == orderId)
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Id);

    private async Task<ChecklistItem> LoadChecklistItemAsync(Guid orderId, Guid itemId, CancellationToken cancellationToken)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var item = await db.ChecklistItems.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.OrderId == orderId,
            cancellationToken);
        if (item is null)
        {
            throw new AuthException(OrderErrorCodes.ChecklistItemNotFound, "The Checklist Item could not be found.", 404);
        }

        return item;
    }

    private async Task<FolderLink> LoadFolderLinkAsync(Guid orderId, Guid linkId, CancellationToken cancellationToken)
    {
        await EnsureOrderExistsAsync(orderId, cancellationToken);
        var link = await db.FolderLinks.SingleOrDefaultAsync(
            candidate => candidate.Id == linkId && candidate.OrderId == orderId,
            cancellationToken);
        if (link is null)
        {
            throw new AuthException(OrderErrorCodes.FolderLinkNotFound, "The Folder Link could not be found.", 404);
        }

        return link;
    }

    private static async Task<int> NextSortOrderAsync(IQueryable<int> sortOrders, CancellationToken cancellationToken)
    {
        var max = await sortOrders.Select(value => (int?)value).MaxAsync(cancellationToken);
        return (max ?? 0) + 10;
    }

    private async Task SaveOrderAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && (postgres.ConstraintName ?? string.Empty).Contains("business_id", StringComparison.Ordinal))
        {
            logger.LogWarning(exception, "Order Business ID conflict while saving an Order.");
            throw new AuthException(OrderErrorCodes.BusinessIdConflict, "The Order ID could not be assigned. Try again.", 409);
        }
    }

    private void Touch(Order order, Guid actorId)
    {
        order.UpdatedAt = time.GetUtcNow();
        order.UpdatedBy = actorId;
    }

    private static void RequireFinancialSort(string sort, OrderFinancialAccess access)
    {
        if (sort == "selling_price" && !access.SellingPrice)
        {
            throw new AuthException(AuthErrorCodes.PermissionDenied, "You do not have permission to perform this action.", 403);
        }

        if (sort == "cost_price" && !access.CostPrice)
        {
            throw new AuthException(AuthErrorCodes.PermissionDenied, "You do not have permission to perform this action.", 403);
        }
    }

    private static void ApplyOrder<T>(List<T> rows, IReadOnlyList<Guid>? ids, Func<T, Guid> id, Action<T, int> assign)
    {
        if (ids is null)
        {
            throw AuthException.Validation("ids", "A value is required.");
        }
        var distinct = ids.Distinct().Count();
        var rowIds = rows.Select(id).ToHashSet();
        if (ids.Count != distinct || rows.Count != ids.Count || ids.Any(itemId => !rowIds.Contains(itemId)))
        {
            throw AuthException.Validation("ids", "The order does not match the current items.");
        }

        var position = new Dictionary<Guid, int>(ids.Count);
        for (var index = 0; index < ids.Count; index++)
        {
            position[ids[index]] = index;
        }

        foreach (var row in rows)
        {
            assign(row, (position[id(row)] + 1) * 10);
        }
    }

    private static OrderListItem ToListItem(OrderListRow row, OrderFinancialAccess access) =>
        new(
            row.Id,
            row.BusinessId,
            row.Name,
            new OrderProjectSummary(row.ProjectId, row.ProjectBusinessId, row.ProjectName),
            new OrderClientSummary(row.ClientId, row.ClientBusinessId, row.ClientName),
            new OrderTypeSummary(row.OrderTypeId, row.OrderTypeName, row.OrderTypeIsActive),
            row.Status,
            row.Priority,
            row.Deadline,
            row.PreviewImagePath,
            Money(access.SellingPrice, row.SellingPrice),
            Money(access.CostPrice, row.CostPrice),
            Profit(access, row.SellingPrice, row.CostPrice));

    private static OrderDetail ToDetail(Order order, OrderFinancialAccess access, bool calculatorConfigured)
    {
        var checklist = order.ChecklistItems.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).Select(ToChecklist).ToArray();
        var progress = ChecklistProgress.Calculate(order.ChecklistItems.Select(item => item.IsCompleted));
        var links = order.FolderLinks.OrderBy(link => link.SortOrder).ThenBy(link => link.Id).Select(ToFolderLink).ToArray();
        return new OrderDetail(
            order.Id,
            order.BusinessId,
            new OrderProjectContext(order.Project.Id, order.Project.BusinessId, order.Project.Name, order.Project.Status, order.Project.Deadline),
            new OrderClientSummary(order.Project.Client.Id, order.Project.Client.BusinessId, order.Project.Client.Name),
            new OrderTypeSummary(order.OrderType.Id, order.OrderType.Name, order.OrderType.IsActive),
            order.Name,
            order.Description,
            order.Status,
            order.Priority,
            order.Deadline,
            order.PreviewImagePath,
            ToTeam(order.Project),
            calculatorConfigured,
            new ChecklistProgressResponse(progress.Completed, progress.Total),
            checklist,
            links,
            order.CreatedAt,
            order.UpdatedAt,
            Money(access.SellingPrice, order.SellingPrice),
            Money(access.CostPrice, order.CostPrice),
            Profit(access, order.SellingPrice, order.CostPrice));
    }

    private static OrderTeam ToTeam(Project project)
    {
        OrderEmployeeSummary Person(ProjectMember member) =>
            new(member.Employee.Id, member.Employee.FullName, member.Employee.Position, member.Employee.IsActive);

        OrderEmployeeSummary? Singular(string role) =>
            project.Members.Where(member => member.ProjectRole == role).Select(Person).FirstOrDefault();

        IReadOnlyList<OrderEmployeeSummary> Many(string role) =>
            project.Members.Where(member => member.ProjectRole == role)
                .OrderBy(member => member.Employee.FullName)
                .Select(Person)
                .ToArray();

        return new OrderTeam(
            Singular(ProjectRoles.Owner),
            Singular(ProjectRoles.Assignee),
            Many(ProjectRoles.Participant),
            Many(ProjectRoles.Observer));
    }

    private static ChecklistItemResponse ToChecklist(ChecklistItem item) =>
        new(item.Id, item.Text, item.IsCompleted, item.SortOrder);

    private static FolderLinkResponse ToFolderLink(FolderLink link) =>
        new(link.Id, link.Name, link.Path, link.SortOrder);

    private static decimal? Money(bool allowed, decimal value) => allowed ? value : null;

    private static decimal? Profit(OrderFinancialAccess access, decimal sellingPrice, decimal costPrice) =>
        access.SellingPrice && access.CostPrice ? sellingPrice - costPrice : null;

    private static OrderPage<T> Page<T>(IReadOnlyList<T> items, OrderListRequest request, int total)
    {
        var pageCount = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        return new OrderPage<T>(items, request.Page, request.PageSize, total, pageCount);
    }

    private static string RequireName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("name", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > OrderConfiguration.NameMaxLength)
        {
            throw AuthException.Validation("name", $"Must be at most {OrderConfiguration.NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string RequirePriority(string value)
    {
        if (!OrderPriorities.TryParse(value, out var priority))
        {
            throw AuthException.Validation("priority", "Priority is not recognized.");
        }

        return priority;
    }

    private static string RequireChecklistText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("text", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > ChecklistTextMaxLength)
        {
            throw AuthException.Validation("text", $"Must be at most {ChecklistTextMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? RequireFolderName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > FolderLinkConfiguration.NameMaxLength)
        {
            throw AuthException.Validation("name", $"Must be at most {FolderLinkConfiguration.NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string RequirePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("path", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > PathMaxLength)
        {
            throw AuthException.Validation("path", $"Must be at most {PathMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? RequirePreviewPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > PreviewPathMaxLength)
        {
            throw AuthException.Validation("previewImagePath", $"Must be at most {PreviewPathMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string LikePattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private static DateOnly? ParseDate(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            throw AuthException.Validation(field, "Enter a valid date.");
        }

        return date;
    }

    private static AuthException OrderNotFound() =>
        new(OrderErrorCodes.OrderNotFound, "The Order could not be found.", 404);

    private sealed record OrderListRow(
        Guid Id,
        string BusinessId,
        string Name,
        Guid ProjectId,
        string ProjectBusinessId,
        string ProjectName,
        Guid ClientId,
        string ClientBusinessId,
        string ClientName,
        Guid OrderTypeId,
        string OrderTypeName,
        bool OrderTypeIsActive,
        string Status,
        string Priority,
        DateOnly? Deadline,
        string? PreviewImagePath,
        decimal SellingPrice,
        decimal CostPrice);
}
