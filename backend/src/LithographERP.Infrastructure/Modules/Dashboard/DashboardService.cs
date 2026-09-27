using LithographERP.Application.Modules.Dashboard;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LithographERP.Infrastructure.Modules.Dashboard;

public sealed class DashboardService(LithographDbContext db, TimeProvider time) : IDashboardService
{
    public const int ListLimit = 10;

    public async Task<DashboardResponse> GetAsync(
        bool includeOrders,
        bool includeProjects,
        CancellationToken cancellationToken = default)
    {
        DashboardSummary? summary = null;
        IReadOnlyList<DashboardOrderRow>? active = null;
        IReadOnlyList<DashboardOrderRow>? urgent = null;
        IReadOnlyList<DashboardOrderRow>? dueSoon = null;
        IReadOnlyList<DashboardOrderRow>? overdue = null;
        IReadOnlyList<DashboardOrderRow>? recentOrders = null;
        if (includeOrders)
        {
            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var dueSoonEnd = today.AddDays(7);
            var orders = db.Orders.AsNoTracking();
            summary = await CountAsync(orders, today, dueSoonEnd, cancellationToken);
            active = await ListAsync(SortActive(OpenAttention(orders, OrderStatuses.Active)), cancellationToken);
            urgent = await ListAsync(SortUrgent(Urgent(orders), today), cancellationToken);
            dueSoon = await ListAsync(SortByDeadline(DueSoon(orders, today, dueSoonEnd)), cancellationToken);
            overdue = await ListAsync(SortByDeadline(Overdue(orders, today)), cancellationToken);
            recentOrders = await ListAsync(
                orders.Where(order => order.Status != OrderStatuses.Cancelled)
                    .OrderByDescending(order => order.CreatedAt)
                    .ThenBy(order => order.Id),
                cancellationToken);
        }

        IReadOnlyList<DashboardProjectRow>? recentProjects = null;
        if (includeProjects)
        {
            recentProjects = await db.Projects.AsNoTracking()
                .OrderByDescending(project => project.CreatedAt)
                .ThenBy(project => project.Id)
                .Take(ListLimit)
                .Select(project => new DashboardProjectRow(
                    project.Id,
                    project.BusinessId,
                    project.Name,
                    project.Client.Name,
                    project.Status,
                    db.ProjectMembers
                        .Where(member => member.ProjectId == project.Id && member.ProjectRole == ProjectRoles.Owner)
                        .Select(member => member.Employee.FullName)
                        .FirstOrDefault()))
                .ToListAsync(cancellationToken);
        }

        return new DashboardResponse(summary, active, urgent, dueSoon, overdue, recentOrders, recentProjects);
    }

    private static IQueryable<Order> OpenAttention(IQueryable<Order> orders, string status) =>
        orders.Where(order => order.Status == status);

    private static IQueryable<Order> Urgent(IQueryable<Order> orders) =>
        orders.Where(order =>
            order.Priority == OrderPriorities.Urgent
            && order.Status != OrderStatuses.Completed
            && order.Status != OrderStatuses.Cancelled);

    private static IQueryable<Order> DueSoon(IQueryable<Order> orders, DateOnly today, DateOnly dueSoonEnd) =>
        orders.Where(order =>
            order.Deadline != null
            && order.Deadline >= today
            && order.Deadline <= dueSoonEnd
            && order.Status != OrderStatuses.Completed
            && order.Status != OrderStatuses.Cancelled);

    private static IQueryable<Order> Overdue(IQueryable<Order> orders, DateOnly today) =>
        orders.Where(order =>
            order.Deadline != null
            && order.Deadline < today
            && order.Status != OrderStatuses.Completed
            && order.Status != OrderStatuses.Cancelled);

    private static IOrderedQueryable<Order> SortActive(IQueryable<Order> orders) =>
        orders
            .OrderByDescending(order => order.Priority == OrderPriorities.Urgent)
            .ThenBy(order => order.Deadline == null)
            .ThenBy(order => order.Deadline)
            .ThenByDescending(order => order.UpdatedAt ?? order.CreatedAt)
            .ThenBy(order => order.Id);

    private static IOrderedQueryable<Order> SortUrgent(IQueryable<Order> orders, DateOnly today) =>
        orders
            .OrderByDescending(order => order.Deadline != null && order.Deadline < today)
            .ThenBy(order => order.Deadline == null)
            .ThenBy(order => order.Deadline)
            .ThenByDescending(order => order.UpdatedAt ?? order.CreatedAt)
            .ThenBy(order => order.Id);

    private static IOrderedQueryable<Order> SortByDeadline(IQueryable<Order> orders) =>
        orders.OrderBy(order => order.Deadline).ThenBy(order => order.Id);

    private static async Task<DashboardSummary> CountAsync(
        IQueryable<Order> orders,
        DateOnly today,
        DateOnly dueSoonEnd,
        CancellationToken cancellationToken)
    {
        var counts = await orders
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Active = group.Count(order => order.Status == OrderStatuses.Active),
                Urgent = group.Count(order =>
                    order.Priority == OrderPriorities.Urgent
                    && order.Status != OrderStatuses.Completed
                    && order.Status != OrderStatuses.Cancelled),
                DueSoon = group.Count(order =>
                    order.Deadline != null
                    && order.Deadline >= today
                    && order.Deadline <= dueSoonEnd
                    && order.Status != OrderStatuses.Completed
                    && order.Status != OrderStatuses.Cancelled),
                Overdue = group.Count(order =>
                    order.Deadline != null
                    && order.Deadline < today
                    && order.Status != OrderStatuses.Completed
                    && order.Status != OrderStatuses.Cancelled),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return counts is null
            ? new DashboardSummary(0, 0, 0, 0)
            : new DashboardSummary(counts.Active, counts.Urgent, counts.DueSoon, counts.Overdue);
    }

    private static async Task<IReadOnlyList<DashboardOrderRow>> ListAsync(
        IQueryable<Order> orders,
        CancellationToken cancellationToken) =>
        await orders
            .Take(ListLimit)
            .Select(order => new DashboardOrderRow(
                order.Id,
                order.BusinessId,
                order.Name,
                order.Project.Client.Name,
                order.Project.Name,
                order.OrderType.Name,
                order.Status,
                order.Priority,
                order.Deadline,
                order.CreatedAt))
            .ToListAsync(cancellationToken);
}
