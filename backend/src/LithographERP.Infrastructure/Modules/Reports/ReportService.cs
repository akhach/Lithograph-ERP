using LithographERP.Application.Modules.Reports;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LithographERP.Infrastructure.Modules.Reports;

public sealed class ReportService(LithographDbContext db) : IReportService
{
    public async Task<ReportFilterOptions> OptionsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await db.Clients.AsNoTracking()
            .OrderBy(client => client.Name)
            .ThenBy(client => client.Id)
            .Select(client => new ReportClientOption(client.Id, client.BusinessId, client.Name, client.IsActive))
            .ToListAsync(cancellationToken);
        var projects = await db.Projects.AsNoTracking()
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Select(project => new ReportProjectOption(project.Id, project.BusinessId, project.Name, project.ClientId, project.Status))
            .ToListAsync(cancellationToken);
        var orderTypes = await db.OrderTypes.AsNoTracking()
            .OrderBy(type => type.Name)
            .ThenBy(type => type.Id)
            .Select(type => new ReportOrderTypeOption(type.Id, type.Name, type.IsActive))
            .ToListAsync(cancellationToken);
        var employees = await db.Employees.AsNoTracking()
            .OrderBy(employee => employee.FullName)
            .ThenBy(employee => employee.Id)
            .Select(employee => new ReportEmployeeOption(employee.Id, employee.FullName))
            .ToListAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .OrderBy(order => order.BusinessId)
            .Select(order => new ReportOrderOption(order.Id, order.BusinessId, order.Name, order.ProjectId))
            .ToListAsync(cancellationToken);
        return new ReportFilterOptions(clients, projects, orderTypes, employees, orders);
    }

    public async Task<ReportPage<OrderReportRow, OrderReportSummary>> OrdersAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        ReportQueryParser.RequireFinancialSort(query.Sort, access);
        var orders = FilteredOrders(query);
        var totals = await SumOrdersAsync(orders, access, cancellationToken);
        var items = await ProjectOrders(SortOrders(orders, query), access)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var rows = items.Select(item => new OrderReportRow(
            item.OrderId,
            item.OrderBusinessId,
            item.OrderName,
            item.ClientId,
            item.ClientBusinessId,
            item.ClientName,
            item.ProjectId,
            item.ProjectBusinessId,
            item.ProjectName,
            item.OrderTypeId,
            item.OrderTypeName,
            item.OrderTypeIsActive,
            item.Status,
            item.Priority,
            item.Deadline,
            item.CreatedAt,
            item.SellingPrice,
            item.CostPrice,
            Profit(access, item.SellingPrice, item.CostPrice))).ToArray();
        return new ReportPage<OrderReportRow, OrderReportSummary>(
            rows,
            query.Page,
            query.PageSize,
            totals.Count,
            PageCount(totals.Count, query.PageSize),
            new OrderReportSummary(totals.Count, Visible(access.SellingPrice, totals.Selling), Visible(access.CostPrice, totals.Cost), Profit(access, totals.Selling, totals.Cost)));
    }

    public async Task<ReportPage<ProjectReportRow, ProjectReportSummary>> ProjectsAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        ReportQueryParser.RequireFinancialSort(query.Sort, access);
        var orders = FilteredOrders(query);
        var totals = await SumOrdersAsync(orders, access, cancellationToken);
        var grouped = ProjectGroups(orders);
        var totalProjects = await grouped.CountAsync(cancellationToken);
        var page = await SortProjects(grouped, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var members = await MembersAsync(page.Select(row => row.ProjectId).ToArray(), cancellationToken);
        var rows = page.Select(row =>
        {
            members.TryGetValue(row.ProjectId, out var team);
            return new ProjectReportRow(
                row.ProjectId,
                row.ProjectBusinessId,
                row.ProjectName,
                row.ClientId,
                row.ClientBusinessId,
                row.ClientName,
                row.Status,
                team.OwnerId,
                team.OwnerName,
                team.AssigneeId,
                team.AssigneeName,
                row.OrderCount,
                Visible(access.SellingPrice, row.SellingTotal),
                Visible(access.CostPrice, row.CostTotal),
                Profit(access, row.SellingTotal, row.CostTotal));
        }).ToArray();
        return new ReportPage<ProjectReportRow, ProjectReportSummary>(
            rows,
            query.Page,
            query.PageSize,
            totalProjects,
            PageCount(totalProjects, query.PageSize),
            new ProjectReportSummary(
                totalProjects,
                totals.Count,
                Visible(access.SellingPrice, totals.Selling),
                Visible(access.CostPrice, totals.Cost),
                Profit(access, totals.Selling, totals.Cost)));
    }

    public async Task<ReportPage<ClientReportRow, ClientReportSummary>> ClientsAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        ReportQueryParser.RequireFinancialSort(query.Sort, access);
        var orders = FilteredOrders(query);
        var totals = await SumOrdersAsync(orders, access, cancellationToken);
        var totalProjects = await orders.Select(order => order.ProjectId).Distinct().CountAsync(cancellationToken);
        var grouped = ClientGroups(orders);
        var totalClients = await grouped.CountAsync(cancellationToken);
        var page = await SortClients(grouped, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var rows = page.Select(row => new ClientReportRow(
            row.ClientId,
            row.ClientBusinessId,
            row.ClientName,
            row.IsActive,
            row.ProjectCount,
            row.OrderCount,
            Visible(access.SellingPrice, row.SellingTotal),
            Visible(access.CostPrice, row.CostTotal),
            Profit(access, row.SellingTotal, row.CostTotal))).ToArray();
        return new ReportPage<ClientReportRow, ClientReportSummary>(
            rows,
            query.Page,
            query.PageSize,
            totalClients,
            PageCount(totalClients, query.PageSize),
            new ClientReportSummary(
                totalClients,
                totalProjects,
                totals.Count,
                Visible(access.SellingPrice, totals.Selling),
                Visible(access.CostPrice, totals.Cost),
                Profit(access, totals.Selling, totals.Cost)));
    }

    public async Task<ReportPage<OrderTypeReportRow, OrderTypeReportSummary>> OrderTypesAsync(
        ReportQuery query,
        ReportFinancialAccess access,
        CancellationToken cancellationToken = default)
    {
        ReportQueryParser.RequireFinancialSort(query.Sort, access);
        var orders = FilteredOrders(query);
        var totals = await SumOrdersAsync(orders, access, cancellationToken);
        var grouped = OrderTypeGroups(orders);
        var totalTypes = await grouped.CountAsync(cancellationToken);
        var page = await SortOrderTypes(grouped, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var rows = page.Select(row => new OrderTypeReportRow(
            row.OrderTypeId,
            row.OrderTypeName,
            row.IsActive,
            row.OrderCount,
            Visible(access.SellingPrice, row.SellingTotal),
            Visible(access.CostPrice, row.CostTotal),
            Profit(access, row.SellingTotal, row.CostTotal))).ToArray();
        return new ReportPage<OrderTypeReportRow, OrderTypeReportSummary>(
            rows,
            query.Page,
            query.PageSize,
            totalTypes,
            PageCount(totalTypes, query.PageSize),
            new OrderTypeReportSummary(
                totalTypes,
                totals.Count,
                Visible(access.SellingPrice, totals.Selling),
                Visible(access.CostPrice, totals.Cost),
                Profit(access, totals.Selling, totals.Cost)));
    }

    public async Task<ReportPage<CostReportRow, CostReportSummary>> CostsAsync(
        ReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var lines = FilteredCosts(query);
        var costItemCount = await lines.CountAsync(cancellationToken);
        var totalAmount = await lines.SumAsync(line => (decimal?)line.Amount, cancellationToken) ?? 0m;
        var grouped = GroupCosts(lines, query.GroupBy);
        var totalGroups = await grouped.CountAsync(cancellationToken);
        var page = await SortCosts(grouped, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var rows = page.Select(row => new CostReportRow(
            row.GroupKey,
            row.Label,
            row.OrderId,
            row.OrderBusinessId,
            row.OrderName,
            row.ProjectId,
            row.ProjectBusinessId,
            row.ProjectName,
            row.ClientId,
            row.ClientBusinessId,
            row.ClientName,
            row.OrderTypeId,
            row.OrderTypeName,
            row.OrderTypeIsActive,
            row.CostItemCount,
            row.TotalAmount)).ToArray();
        return new ReportPage<CostReportRow, CostReportSummary>(
            rows,
            query.Page,
            query.PageSize,
            totalGroups,
            PageCount(totalGroups, query.PageSize),
            new CostReportSummary(totalGroups, costItemCount, totalAmount));
    }

    private IQueryable<Order> FilteredOrders(ReportQuery query)
    {
        var orders = db.Orders.AsNoTracking();
        if (query.OrderStatus == OrderStatuses.Cancelled)
        {
            orders = orders.Where(order => order.Status == OrderStatuses.Cancelled);
        }
        else
        {
            if (query.OrderStatus is not null)
            {
                orders = orders.Where(order => order.Status == query.OrderStatus);
            }

            if (!query.IncludeCancelled)
            {
                orders = orders.Where(order => order.Status != OrderStatuses.Cancelled);
            }
        }

        // Calendar dates are inclusive UTC days: from_date starts at 00:00Z and to_date runs through 23:59:59.999Z.
        if (query.FromDate is DateOnly fromDate)
        {
            var start = UtcStart(fromDate);
            orders = orders.Where(order => order.CreatedAt >= start);
        }

        if (query.ToDate is DateOnly toDate)
        {
            var endExclusive = UtcStart(toDate.AddDays(1));
            orders = orders.Where(order => order.CreatedAt < endExclusive);
        }

        if (query.ClientId is not null)
        {
            orders = orders.Where(order => order.Project.ClientId == query.ClientId);
        }

        if (query.ProjectId is not null)
        {
            orders = orders.Where(order => order.ProjectId == query.ProjectId);
        }

        if (query.OrderTypeId is not null)
        {
            orders = orders.Where(order => order.OrderTypeId == query.OrderTypeId);
        }

        if (query.ProjectStatus is not null)
        {
            orders = orders.Where(order => order.Project.Status == query.ProjectStatus);
        }

        if (query.Priority is not null)
        {
            orders = orders.Where(order => order.Priority == query.Priority);
        }

        if (query.OwnerEmployeeId is not null)
        {
            orders = orders.Where(order => order.Project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Owner && member.EmployeeId == query.OwnerEmployeeId));
        }

        if (query.AssigneeEmployeeId is not null)
        {
            orders = orders.Where(order => order.Project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Assignee && member.EmployeeId == query.AssigneeEmployeeId));
        }

        if (query.Search is not null)
        {
            var pattern = ContainsPattern(query.Search);
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

    private IQueryable<CostJoined> FilteredCosts(ReportQuery query)
    {
        var items = db.CostItems.AsNoTracking();
        if (query.FromExpenseDate is DateOnly fromDate)
        {
            items = items.Where(item => item.ExpenseDate != null && item.ExpenseDate >= fromDate);
        }

        if (query.ToExpenseDate is DateOnly toDate)
        {
            items = items.Where(item => item.ExpenseDate != null && item.ExpenseDate <= toDate);
        }

        if (query.Category is not null)
        {
            var pattern = ContainsPattern(query.Category);
            items = items.Where(item => EF.Functions.ILike(item.Category, pattern, "\\"));
        }

        if (query.Supplier is not null)
        {
            var pattern = ContainsPattern(query.Supplier);
            items = items.Where(item => item.Supplier != null && EF.Functions.ILike(item.Supplier, pattern, "\\"));
        }

        if (query.OrderId is not null)
        {
            items = items.Where(item => item.OrderId == query.OrderId);
        }

        var orders = db.Orders.AsNoTracking();
        if (query.OrderStatus == OrderStatuses.Cancelled)
        {
            orders = orders.Where(order => order.Status == OrderStatuses.Cancelled);
        }
        else
        {
            if (query.OrderStatus is not null)
            {
                orders = orders.Where(order => order.Status == query.OrderStatus);
            }

            if (!query.IncludeCancelled)
            {
                orders = orders.Where(order => order.Status != OrderStatuses.Cancelled);
            }
        }

        if (query.ProjectId is not null)
        {
            orders = orders.Where(order => order.ProjectId == query.ProjectId);
        }

        if (query.OrderTypeId is not null)
        {
            orders = orders.Where(order => order.OrderTypeId == query.OrderTypeId);
        }

        if (query.ClientId is not null)
        {
            orders = orders.Where(order => order.Project.ClientId == query.ClientId);
        }

        return
            from item in items
            join order in orders on item.OrderId equals order.Id
            select new CostJoined
            {
                Amount = item.Amount,
                Category = item.Category,
                Supplier = item.Supplier,
                OrderId = order.Id,
                OrderBusinessId = order.BusinessId,
                OrderName = order.Name,
                ProjectId = order.ProjectId,
                ProjectBusinessId = order.Project.BusinessId,
                ProjectName = order.Project.Name,
                ClientId = order.Project.ClientId,
                ClientBusinessId = order.Project.Client.BusinessId,
                ClientName = order.Project.Client.Name,
                OrderTypeId = order.OrderTypeId,
                OrderTypeName = order.OrderType.Name,
                OrderTypeIsActive = order.OrderType.IsActive,
            };
    }

    private static IQueryable<OrderLine> ProjectOrders(IQueryable<Order> orders, ReportFinancialAccess access) =>
        orders.Select(order => new OrderLine
        {
            OrderId = order.Id,
            OrderBusinessId = order.BusinessId,
            OrderName = order.Name,
            ClientId = order.Project.ClientId,
            ClientBusinessId = order.Project.Client.BusinessId,
            ClientName = order.Project.Client.Name,
            ProjectId = order.ProjectId,
            ProjectBusinessId = order.Project.BusinessId,
            ProjectName = order.Project.Name,
            OrderTypeId = order.OrderTypeId,
            OrderTypeName = order.OrderType.Name,
            OrderTypeIsActive = order.OrderType.IsActive,
            Status = order.Status,
            Priority = order.Priority,
            Deadline = order.Deadline,
            CreatedAt = order.CreatedAt,
            SellingPrice = access.SellingPrice ? order.SellingPrice : null,
            CostPrice = access.CostPrice ? order.CostPrice : null,
        });

    private static IQueryable<ProjectAggregate> ProjectGroups(IQueryable<Order> orders) =>
        from order in orders
        group order by new
        {
            order.ProjectId,
            ProjectBusinessId = order.Project.BusinessId,
            ProjectName = order.Project.Name,
            ProjectStatus = order.Project.Status,
            ClientId = order.Project.ClientId,
            ClientBusinessId = order.Project.Client.BusinessId,
            ClientName = order.Project.Client.Name,
        }
        into grouped
        select new ProjectAggregate
        {
            ProjectId = grouped.Key.ProjectId,
            ProjectBusinessId = grouped.Key.ProjectBusinessId,
            ProjectName = grouped.Key.ProjectName,
            Status = grouped.Key.ProjectStatus,
            ClientId = grouped.Key.ClientId,
            ClientBusinessId = grouped.Key.ClientBusinessId,
            ClientName = grouped.Key.ClientName,
            OrderCount = grouped.Count(),
            SellingTotal = grouped.Sum(order => order.SellingPrice),
            CostTotal = grouped.Sum(order => order.CostPrice),
        };

    private static IQueryable<ClientAggregate> ClientGroups(IQueryable<Order> orders) =>
        from order in orders
        group order by new
        {
            ClientId = order.Project.ClientId,
            ClientBusinessId = order.Project.Client.BusinessId,
            ClientName = order.Project.Client.Name,
            IsActive = order.Project.Client.IsActive,
        }
        into grouped
        select new ClientAggregate
        {
            ClientId = grouped.Key.ClientId,
            ClientBusinessId = grouped.Key.ClientBusinessId,
            ClientName = grouped.Key.ClientName,
            IsActive = grouped.Key.IsActive,
            ProjectCount = grouped.Select(order => order.ProjectId).Distinct().Count(),
            OrderCount = grouped.Count(),
            SellingTotal = grouped.Sum(order => order.SellingPrice),
            CostTotal = grouped.Sum(order => order.CostPrice),
        };

    private static IQueryable<OrderTypeAggregate> OrderTypeGroups(IQueryable<Order> orders) =>
        from order in orders
        group order by new
        {
            order.OrderTypeId,
            OrderTypeName = order.OrderType.Name,
            IsActive = order.OrderType.IsActive,
        }
        into grouped
        select new OrderTypeAggregate
        {
            OrderTypeId = grouped.Key.OrderTypeId,
            OrderTypeName = grouped.Key.OrderTypeName,
            IsActive = grouped.Key.IsActive,
            OrderCount = grouped.Count(),
            SellingTotal = grouped.Sum(order => order.SellingPrice),
            CostTotal = grouped.Sum(order => order.CostPrice),
        };

    private static IQueryable<CostAggregate> GroupCosts(IQueryable<CostJoined> lines, string group) =>
        group switch
        {
            "supplier" => lines.GroupBy(line => line.Supplier).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key ?? string.Empty,
                Label = grouped.Key,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
            "order" => lines.GroupBy(line => new
            {
                line.OrderId,
                line.OrderBusinessId,
                line.OrderName,
                line.ProjectId,
                line.ProjectBusinessId,
                line.ProjectName,
                line.ClientId,
                line.ClientBusinessId,
                line.ClientName,
            }).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key.OrderBusinessId,
                Label = grouped.Key.OrderName,
                OrderId = grouped.Key.OrderId,
                OrderBusinessId = grouped.Key.OrderBusinessId,
                OrderName = grouped.Key.OrderName,
                ProjectId = grouped.Key.ProjectId,
                ProjectBusinessId = grouped.Key.ProjectBusinessId,
                ProjectName = grouped.Key.ProjectName,
                ClientId = grouped.Key.ClientId,
                ClientBusinessId = grouped.Key.ClientBusinessId,
                ClientName = grouped.Key.ClientName,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
            "project" => lines.GroupBy(line => new
            {
                line.ProjectId,
                line.ProjectBusinessId,
                line.ProjectName,
                line.ClientId,
                line.ClientBusinessId,
                line.ClientName,
            }).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key.ProjectBusinessId,
                Label = grouped.Key.ProjectName,
                ProjectId = grouped.Key.ProjectId,
                ProjectBusinessId = grouped.Key.ProjectBusinessId,
                ProjectName = grouped.Key.ProjectName,
                ClientId = grouped.Key.ClientId,
                ClientBusinessId = grouped.Key.ClientBusinessId,
                ClientName = grouped.Key.ClientName,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
            "client" => lines.GroupBy(line => new
            {
                line.ClientId,
                line.ClientBusinessId,
                line.ClientName,
            }).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key.ClientBusinessId,
                Label = grouped.Key.ClientName,
                ClientId = grouped.Key.ClientId,
                ClientBusinessId = grouped.Key.ClientBusinessId,
                ClientName = grouped.Key.ClientName,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
            "order_type" => lines.GroupBy(line => new
            {
                line.OrderTypeId,
                line.OrderTypeName,
                line.OrderTypeIsActive,
            }).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key.OrderTypeName,
                Label = grouped.Key.OrderTypeName,
                OrderTypeId = grouped.Key.OrderTypeId,
                OrderTypeName = grouped.Key.OrderTypeName,
                OrderTypeIsActive = grouped.Key.OrderTypeIsActive,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
            _ => lines.GroupBy(line => line.Category).Select(grouped => new CostAggregate
            {
                GroupKey = grouped.Key,
                Label = grouped.Key,
                CostItemCount = grouped.Count(),
                TotalAmount = grouped.Sum(line => line.Amount),
            }),
        };

    private static IOrderedQueryable<Order> SortOrders(IQueryable<Order> orders, ReportQuery query)
    {
        var ordered = query.Sort switch
        {
            "order_business_id" => Direction(orders, query.Descending, order => order.BusinessId),
            "order_name" => Direction(orders, query.Descending, order => order.Name),
            "client_name" => Direction(orders, query.Descending, order => order.Project.Client.Name),
            "project_name" => Direction(orders, query.Descending, order => order.Project.Name),
            "order_type" => Direction(orders, query.Descending, order => order.OrderType.Name),
            "status" => Direction(orders, query.Descending, order => order.Status),
            "priority" => Direction(orders, query.Descending, order => order.Priority),
            "deadline" => Direction(orders, query.Descending, order => order.Deadline),
            "selling_price" => Direction(orders, query.Descending, order => order.SellingPrice),
            "cost_price" => Direction(orders, query.Descending, order => order.CostPrice),
            "profit" => Direction(orders, query.Descending, order => order.SellingPrice - order.CostPrice),
            _ => Direction(orders, query.Descending, order => order.CreatedAt),
        };
        return ordered.ThenBy(order => order.Id);
    }

    private static IOrderedQueryable<ProjectAggregate> SortProjects(IQueryable<ProjectAggregate> rows, ReportQuery query)
    {
        var ordered = query.Sort switch
        {
            "project_business_id" => Direction(rows, query.Descending, row => row.ProjectBusinessId),
            "client_name" => Direction(rows, query.Descending, row => row.ClientName),
            "status" => Direction(rows, query.Descending, row => row.Status),
            "order_count" => Direction(rows, query.Descending, row => row.OrderCount),
            "selling_total" => Direction(rows, query.Descending, row => row.SellingTotal),
            "cost_total" => Direction(rows, query.Descending, row => row.CostTotal),
            "profit" => Direction(rows, query.Descending, row => row.SellingTotal - row.CostTotal),
            _ => Direction(rows, query.Descending, row => row.ProjectName),
        };
        return ordered.ThenBy(row => row.ProjectId);
    }

    private static IOrderedQueryable<ClientAggregate> SortClients(IQueryable<ClientAggregate> rows, ReportQuery query)
    {
        var ordered = query.Sort switch
        {
            "client_business_id" => Direction(rows, query.Descending, row => row.ClientBusinessId),
            "project_count" => Direction(rows, query.Descending, row => row.ProjectCount),
            "order_count" => Direction(rows, query.Descending, row => row.OrderCount),
            "selling_total" => Direction(rows, query.Descending, row => row.SellingTotal),
            "cost_total" => Direction(rows, query.Descending, row => row.CostTotal),
            "profit" => Direction(rows, query.Descending, row => row.SellingTotal - row.CostTotal),
            _ => Direction(rows, query.Descending, row => row.ClientName),
        };
        return ordered.ThenBy(row => row.ClientId);
    }

    private static IOrderedQueryable<OrderTypeAggregate> SortOrderTypes(IQueryable<OrderTypeAggregate> rows, ReportQuery query)
    {
        var ordered = query.Sort switch
        {
            "order_count" => Direction(rows, query.Descending, row => row.OrderCount),
            "selling_total" => Direction(rows, query.Descending, row => row.SellingTotal),
            "cost_total" => Direction(rows, query.Descending, row => row.CostTotal),
            "profit" => Direction(rows, query.Descending, row => row.SellingTotal - row.CostTotal),
            _ => Direction(rows, query.Descending, row => row.OrderTypeName),
        };
        return ordered.ThenBy(row => row.OrderTypeId);
    }

    private static IOrderedQueryable<CostAggregate> SortCosts(IQueryable<CostAggregate> rows, ReportQuery query)
    {
        var ordered = query.Sort switch
        {
            "cost_item_count" => Direction(rows, query.Descending, row => row.CostItemCount),
            "total_amount" => Direction(rows, query.Descending, row => row.TotalAmount),
            "order_business_id" => Direction(rows, query.Descending, row => row.OrderBusinessId),
            "order_name" => Direction(rows, query.Descending, row => row.OrderName),
            "project_business_id" => Direction(rows, query.Descending, row => row.ProjectBusinessId),
            "project_name" => Direction(rows, query.Descending, row => row.ProjectName),
            "client_business_id" => Direction(rows, query.Descending, row => row.ClientBusinessId),
            "client_name" => Direction(rows, query.Descending, row => row.ClientName),
            "order_type" => Direction(rows, query.Descending, row => row.OrderTypeName),
            _ => Direction(rows, query.Descending, row => row.Label),
        };
        return ordered.ThenBy(row => row.GroupKey);
    }

    private async Task<Dictionary<Guid, TeamNames>> MembersAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, TeamNames>();
        if (projectIds.Count == 0)
        {
            return map;
        }

        var members = await db.ProjectMembers.AsNoTracking()
            .Where(member => projectIds.Contains(member.ProjectId)
                && (member.ProjectRole == ProjectRoles.Owner || member.ProjectRole == ProjectRoles.Assignee))
            .Select(member => new
            {
                member.ProjectId,
                member.ProjectRole,
                member.EmployeeId,
                member.Employee.FullName,
            })
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            map.TryGetValue(member.ProjectId, out var team);
            if (member.ProjectRole == ProjectRoles.Owner)
            {
                team = team with { OwnerId = member.EmployeeId, OwnerName = member.FullName };
            }
            else
            {
                team = team with { AssigneeId = member.EmployeeId, AssigneeName = member.FullName };
            }

            map[member.ProjectId] = team;
        }

        return map;
    }

    private static async Task<(int Count, decimal Selling, decimal Cost)> SumOrdersAsync(
        IQueryable<Order> orders,
        ReportFinancialAccess access,
        CancellationToken cancellationToken)
    {
        var count = await orders.CountAsync(cancellationToken);
        var selling = access.SellingPrice
            ? await orders.SumAsync(order => (decimal?)order.SellingPrice, cancellationToken) ?? 0m
            : 0m;
        var cost = access.CostPrice
            ? await orders.SumAsync(order => (decimal?)order.CostPrice, cancellationToken) ?? 0m
            : 0m;
        return (count, selling, cost);
    }

    private static IOrderedQueryable<T> Direction<T, TKey>(
        IQueryable<T> query,
        bool descending,
        System.Linq.Expressions.Expression<Func<T, TKey>> key) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private static decimal? Visible(bool allowed, decimal value) => allowed ? value : null;

    private static decimal? Profit(ReportFinancialAccess access, decimal? selling, decimal? cost) =>
        access.SellingPrice && access.CostPrice ? selling.GetValueOrDefault() - cost.GetValueOrDefault() : null;

    private static decimal? Profit(ReportFinancialAccess access, decimal selling, decimal cost) =>
        access.SellingPrice && access.CostPrice ? selling - cost : null;

    private static int PageCount(int totalItems, int pageSize) =>
        totalItems == 0 ? 0 : (totalItems + pageSize - 1) / pageSize;

    private static DateTimeOffset UtcStart(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static string ContainsPattern(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private sealed class OrderLine
    {
        public Guid OrderId { get; set; }

        public string OrderBusinessId { get; set; } = string.Empty;

        public string OrderName { get; set; } = string.Empty;

        public Guid ClientId { get; set; }

        public string ClientBusinessId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }

        public string ProjectBusinessId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public Guid OrderTypeId { get; set; }

        public string OrderTypeName { get; set; } = string.Empty;

        public bool OrderTypeIsActive { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public DateOnly? Deadline { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public decimal? SellingPrice { get; set; }

        public decimal? CostPrice { get; set; }
    }

    private sealed class ProjectAggregate
    {
        public Guid ProjectId { get; set; }

        public string ProjectBusinessId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public Guid ClientId { get; set; }

        public string ClientBusinessId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public decimal SellingTotal { get; set; }

        public decimal CostTotal { get; set; }
    }

    private sealed class ClientAggregate
    {
        public Guid ClientId { get; set; }

        public string ClientBusinessId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int ProjectCount { get; set; }

        public int OrderCount { get; set; }

        public decimal SellingTotal { get; set; }

        public decimal CostTotal { get; set; }
    }

    private sealed class OrderTypeAggregate
    {
        public Guid OrderTypeId { get; set; }

        public string OrderTypeName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int OrderCount { get; set; }

        public decimal SellingTotal { get; set; }

        public decimal CostTotal { get; set; }
    }

    private sealed class CostJoined
    {
        public decimal Amount { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Supplier { get; set; }

        public Guid OrderId { get; set; }

        public string OrderBusinessId { get; set; } = string.Empty;

        public string OrderName { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }

        public string ProjectBusinessId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public Guid ClientId { get; set; }

        public string ClientBusinessId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public Guid OrderTypeId { get; set; }

        public string OrderTypeName { get; set; } = string.Empty;

        public bool OrderTypeIsActive { get; set; }
    }

    private sealed class CostAggregate
    {
        public string GroupKey { get; set; } = string.Empty;

        public string? Label { get; set; }

        public Guid? OrderId { get; set; }

        public string? OrderBusinessId { get; set; }

        public string? OrderName { get; set; }

        public Guid? ProjectId { get; set; }

        public string? ProjectBusinessId { get; set; }

        public string? ProjectName { get; set; }

        public Guid? ClientId { get; set; }

        public string? ClientBusinessId { get; set; }

        public string? ClientName { get; set; }

        public Guid? OrderTypeId { get; set; }

        public string? OrderTypeName { get; set; }

        public bool? OrderTypeIsActive { get; set; }

        public int CostItemCount { get; set; }

        public decimal TotalAmount { get; set; }
    }

    private readonly record struct TeamNames(Guid? OwnerId, string? OwnerName, Guid? AssigneeId, string? AssigneeName);
}
