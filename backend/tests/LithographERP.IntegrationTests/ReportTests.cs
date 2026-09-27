using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ReportTests(LithographApiFactory factory)
{
    [Fact]
    public async Task OrdersReport_PaginatesFiltersSorts_AndSummarizesTheFullSet()
    {
        using var director = await DirectorAsync();
        var client = await CreateClientAsync(director, "North Press");
        var otherClient = await CreateClientAsync(director, "South Press");
        var project = await CreateProjectAsync(director, client.Id, "North Catalog");
        var otherProject = await CreateProjectAsync(director, otherClient.Id, "South Catalog");
        var orderType = await CreateOrderTypeAsync(director, "Brochure");
        var alpha = await CreateOrderAsync(director, project.Id, orderType.Id, "Alpha Poster");
        var bravo = await CreateOrderAsync(director, project.Id, orderType.Id, "Bravo Poster");
        var charlie = await CreateOrderAsync(director, otherProject.Id, orderType.Id, "Charlie Poster");
        var cancelled = await CreateOrderAsync(director, project.Id, orderType.Id, "Cancelled Poster");
        await SetOrderAsync(alpha.Id, 10.10m, 1.00m, "active", "urgent", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        await SetOrderAsync(bravo.Id, 20.25m, 2.00m, "completed", "normal", new DateTimeOffset(2026, 3, 2, 23, 59, 0, TimeSpan.Zero));
        await SetOrderAsync(charlie.Id, 5.00m, 1.00m, "draft", "low", new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero));
        await SetOrderAsync(cancelled.Id, 100m, 9m, "cancelled", "high", new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));

        var page1 = await GetOrdersAsync(director, "page_size=1&sort=order_name&direction=asc");
        var page2 = await GetOrdersAsync(director, "page=2&page_size=1&sort=order_name&direction=asc");
        Assert.Equal(3, page1.TotalItems);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal("Alpha Poster", page1.Items.Single().OrderName);
        Assert.Equal("Bravo Poster", page2.Items.Single().OrderName);
        Assert.Equal(35.35m, page1.Summary.SellingTotal);
        Assert.Equal(4.00m, page1.Summary.CostTotal);
        Assert.Equal(31.35m, page1.Summary.ProfitTotal);
        Assert.Equal(page1.Summary, page2.Summary);
        Assert.Equal(page1.Items[0].SellingPrice - page1.Items[0].CostPrice, page1.Items[0].Profit);
        Assert.Equal(alpha.Id, page1.Items[0].OrderId);
        Assert.Equal(project.Id, page1.Items[0].ProjectId);
        Assert.Equal(client.Id, page1.Items[0].ClientId);

        var byClient = await GetOrdersAsync(director, $"client_id={client.Id}&sort=order_name&direction=asc");
        Assert.Equal(2, byClient.TotalItems);
        Assert.Equal(30.35m, byClient.Summary.SellingTotal);
        Assert.DoesNotContain(byClient.Items, row => row.OrderId == charlie.Id);

        var byStatus = await GetOrdersAsync(director, "status=completed");
        Assert.Equal(bravo.Id, byStatus.Items.Single().OrderId);

        var byPriority = await GetOrdersAsync(director, "priority=urgent");
        Assert.Equal(alpha.Id, byPriority.Items.Single().OrderId);

        var bySearch = await GetOrdersAsync(director, $"search={page1.Items[0].OrderBusinessId}");
        Assert.Equal(alpha.Id, bySearch.Items.Single().OrderId);

        var byName = await GetOrdersAsync(director, "search=south press");
        Assert.Equal(charlie.Id, byName.Items.Single().OrderId);

        var inRange = await GetOrdersAsync(director, "from_date=2026-03-01&to_date=2026-03-02&sort=order_name&direction=asc");
        Assert.Equal(2, inRange.TotalItems);
        Assert.Equal(30.35m, inRange.Summary.SellingTotal);
        Assert.DoesNotContain(inRange.Items, row => row.OrderId == charlie.Id);

        var reversed = await director.GetAsync("/api/reports/orders?from_date=2026-04-02&to_date=2026-04-01");
        await AuthApi.AssertErrorAsync(reversed, HttpStatusCode.BadRequest, "REPORT_DATE_RANGE_INVALID");
        Assert.Contains("The start date cannot be after the end date.", await reversed.Content.ReadAsStringAsync());

        var badSort = await director.GetAsync("/api/reports/orders?sort=secret");
        await AuthApi.AssertErrorAsync(badSort, HttpStatusCode.BadRequest, "REPORT_INVALID_SORT");
        Assert.Contains("The selected report sort field is not supported.", await badSort.Content.ReadAsStringAsync());

        var unknownClient = await GetOrdersAsync(director, $"client_id={Guid.NewGuid()}");
        Assert.Empty(unknownClient.Items);
        Assert.Equal(0, unknownClient.Summary.TotalOrders);
        Assert.Equal(0m, unknownClient.Summary.SellingTotal);
    }

    [Fact]
    public async Task OrdersReport_ExcludesCancelledByDefault_AndCanIncludeThem()
    {
        using var director = await DirectorAsync();
        var graph = await GraphAsync(director);
        var kept = await CreateOrderAsync(director, graph.ProjectId, graph.OrderTypeId, "Kept");
        var dropped = await CreateOrderAsync(director, graph.ProjectId, graph.OrderTypeId, "Dropped");
        await SetOrderAsync(kept.Id, 15m, 5m, "completed");
        await SetOrderAsync(dropped.Id, 40m, 10m, "cancelled");

        var excluded = await GetOrdersAsync(director);
        Assert.Equal(kept.Id, excluded.Items.Single().OrderId);
        Assert.Equal(15m, excluded.Summary.SellingTotal);
        Assert.Equal(5m, excluded.Summary.CostTotal);
        Assert.Equal(10m, excluded.Summary.ProfitTotal);

        var included = await GetOrdersAsync(director, "include_cancelled=true&sort=order_name&direction=asc");
        Assert.Equal(2, included.TotalItems);
        Assert.Equal(55m, included.Summary.SellingTotal);
        Assert.Equal(15m, included.Summary.CostTotal);
        Assert.Equal(40m, included.Summary.ProfitTotal);

        var onlyCancelled = await GetOrdersAsync(director, "status=cancelled");
        Assert.Equal(dropped.Id, onlyCancelled.Items.Single().OrderId);
        Assert.Equal(40m, onlyCancelled.Summary.SellingTotal);
    }

    [Fact]
    public async Task OrdersReport_ProtectsSellingCostAndProfit()
    {
        using var director = await DirectorAsync();
        var graph = await GraphAsync(director);
        var order = await CreateOrderAsync(director, graph.ProjectId, graph.OrderTypeId, "Priced");
        await SetOrderAsync(order.Id, 12.50m, 4.25m, "active");
        var reportsOnly = await UserAsync(director, "reports", PermissionCatalog.Reports.View);
        var sellingOnly = await UserAsync(director, "selling", PermissionCatalog.Reports.View, PermissionCatalog.Orders.ViewSellingPrice);
        var costOnly = await UserAsync(director, "costing", PermissionCatalog.Reports.View, PermissionCatalog.Orders.ViewCostPrice);
        var both = await UserAsync(director, "finance", PermissionCatalog.Reports.View, PermissionCatalog.Orders.ViewSellingPrice, PermissionCatalog.Orders.ViewCostPrice);

        var hidden = await reportsOnly.GetStringAsync("/api/reports/orders");
        Assert.DoesNotContain("\"sellingPrice\"", hidden);
        Assert.DoesNotContain("\"costPrice\"", hidden);
        Assert.DoesNotContain("\"profit\"", hidden);
        Assert.DoesNotContain("\"sellingTotal\"", hidden);
        Assert.DoesNotContain("\"costTotal\"", hidden);
        Assert.DoesNotContain("\"profitTotal\"", hidden);
        Assert.Contains("\"orderName\":\"Priced\"", hidden);

        var selling = await sellingOnly.GetStringAsync("/api/reports/orders");
        Assert.Contains("\"sellingPrice\":12.50", selling);
        Assert.DoesNotContain("\"costPrice\"", selling);
        Assert.DoesNotContain("\"profit\"", selling);
        Assert.Contains("\"sellingTotal\":12.50", selling);
        Assert.DoesNotContain("\"costTotal\"", selling);
        Assert.DoesNotContain("\"profitTotal\"", selling);

        var cost = await costOnly.GetStringAsync("/api/reports/orders");
        Assert.Contains("\"costPrice\":4.25", cost);
        Assert.DoesNotContain("\"sellingPrice\"", cost);
        Assert.DoesNotContain("\"profit\"", cost);

        var full = await GetOrdersAsync(both);
        Assert.Equal(12.50m, full.Items.Single().SellingPrice);
        Assert.Equal(4.25m, full.Items.Single().CostPrice);
        Assert.Equal(8.25m, full.Items.Single().Profit);
        Assert.Equal(8.25m, full.Summary.ProfitTotal);

        await AuthApi.AssertErrorAsync(
            await reportsOnly.GetAsync("/api/reports/orders?sort=selling_price"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await sellingOnly.GetAsync("/api/reports/orders?sort=cost_price"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await sellingOnly.GetAsync("/api/reports/orders?sort=profit"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await costOnly.GetAsync("/api/reports/orders?sort=profit"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await reportsOnly.GetAsync("/api/reports/orders?selling_price=12.50"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await both.GetAsync("/api/reports/orders?min_cost_price=1"),
            HttpStatusCode.BadRequest,
            "REPORT_INVALID_FILTER");

        var sorted = await GetOrdersAsync(both, "sort=selling_price&direction=asc");
        Assert.Equal(order.Id, sorted.Items.Single().OrderId);
    }

    [Fact]
    public async Task ProjectAndClientReports_AggregateThroughProjects()
    {
        using var director = await DirectorAsync();
        var north = await CreateClientAsync(director, "North Client");
        var south = await CreateClientAsync(director, "South Client");
        var first = await CreateProjectAsync(director, north.Id, "First Project");
        var second = await CreateProjectAsync(director, north.Id, "Second Project");
        var empty = await CreateProjectAsync(director, north.Id, "Empty Project");
        var southProject = await CreateProjectAsync(director, south.Id, "South Project");
        var brochure = await CreateOrderTypeAsync(director, "Brochure");
        var poster = await CreateOrderTypeAsync(director, "Poster");
        var owner = await CreateEmployeeAsync(director, "Mara Owner");
        var assignee = await CreateEmployeeAsync(director, "Levon Assignee");
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{first.Id}/owner", new { employeeId = owner.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{first.Id}/assignee", new { employeeId = assignee.Id })).StatusCode);

        var a = await CreateOrderAsync(director, first.Id, brochure.Id, "A");
        var b = await CreateOrderAsync(director, first.Id, poster.Id, "B");
        var c = await CreateOrderAsync(director, second.Id, brochure.Id, "C");
        var d = await CreateOrderAsync(director, southProject.Id, brochure.Id, "D");
        var dropped = await CreateOrderAsync(director, first.Id, brochure.Id, "Dropped");
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsJsonAsync($"/api/projects/{second.Id}/status", new { status = "completed" })).StatusCode);
        await SetOrderAsync(a.Id, 10m, 3m, "active");
        await SetOrderAsync(b.Id, 20m, 4m, "active");
        await SetOrderAsync(c.Id, 7m, 1m, "completed");
        await SetOrderAsync(d.Id, 5m, 2m, "active");
        await SetOrderAsync(dropped.Id, 100m, 50m, "cancelled");

        var projects = await director.GetFromJsonAsync<ProjectReportBody>("/api/reports/projects?sort=project_name&direction=asc");
        Assert.NotNull(projects);
        Assert.Equal(3, projects.TotalItems);
        Assert.DoesNotContain(projects.Items, row => row.ProjectId == empty.Id);
        var firstRow = projects.Items.Single(row => row.ProjectId == first.Id);
        Assert.Equal(2, firstRow.OrderCount);
        Assert.Equal(30m, firstRow.SellingTotal);
        Assert.Equal(7m, firstRow.CostTotal);
        Assert.Equal(23m, firstRow.Profit);
        Assert.Equal(owner.Id, firstRow.OwnerEmployeeId);
        Assert.Equal("Mara Owner", firstRow.OwnerName);
        Assert.Equal(assignee.Id, firstRow.AssigneeEmployeeId);
        Assert.Equal(north.Id, firstRow.ClientId);
        Assert.Equal(4, projects.Summary.TotalOrders);
        Assert.Equal(42m, projects.Summary.SellingTotal);
        Assert.Equal(10m, projects.Summary.CostTotal);
        Assert.Equal(32m, projects.Summary.ProfitTotal);

        var brochureOnly = await director.GetFromJsonAsync<ProjectReportBody>($"/api/reports/projects?order_type_id={brochure.Id}");
        var brochureFirst = brochureOnly!.Items.Single(row => row.ProjectId == first.Id);
        Assert.Equal(1, brochureFirst.OrderCount);
        Assert.Equal(10m, brochureFirst.SellingTotal);

        var byOwner = await director.GetFromJsonAsync<ProjectReportBody>($"/api/reports/projects?owner_employee_id={owner.Id}");
        Assert.Equal(first.Id, byOwner!.Items.Single().ProjectId);

        var completedProjects = await director.GetFromJsonAsync<ProjectReportBody>("/api/reports/projects?project_status=completed");
        Assert.Equal(second.Id, completedProjects!.Items.Single().ProjectId);

        var withCancelled = await director.GetFromJsonAsync<ProjectReportBody>("/api/reports/projects?include_cancelled=true");
        Assert.Equal(142m, withCancelled!.Summary.SellingTotal);

        var sorted = await director.GetFromJsonAsync<ProjectReportBody>("/api/reports/projects?sort=selling_total&direction=asc");
        Assert.Equal(southProject.Id, sorted!.Items[0].ProjectId);

        var reportsOnly = await UserAsync(director, "reports2", PermissionCatalog.Reports.View);
        var hiddenProjects = await reportsOnly.GetStringAsync("/api/reports/projects");
        Assert.DoesNotContain("\"sellingTotal\"", hiddenProjects);
        Assert.DoesNotContain("\"profit\"", hiddenProjects);
        await AuthApi.AssertErrorAsync(
            await reportsOnly.GetAsync("/api/reports/projects?sort=profit"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var clients = await director.GetFromJsonAsync<ClientReportBody>("/api/reports/clients?sort=client_name&direction=asc");
        Assert.NotNull(clients);
        var northRow = clients.Items.Single(row => row.ClientId == north.Id);
        Assert.Equal(2, northRow.ProjectCount);
        Assert.Equal(3, northRow.OrderCount);
        Assert.Equal(37m, northRow.SellingTotal);
        Assert.Equal(8m, northRow.CostTotal);
        Assert.Equal(29m, northRow.Profit);
        Assert.True(northRow.IsActive);
        Assert.Equal(2, clients.Summary.TotalClients);
        Assert.Equal(3, clients.Summary.TotalProjects);
        Assert.Equal(4, clients.Summary.TotalOrders);

        Assert.Equal(HttpStatusCode.OK, (await director.PostAsync($"/api/clients/{north.Id}/deactivate", null)).StatusCode);
        var inactive = await director.GetFromJsonAsync<ClientReportBody>($"/api/reports/clients?client_id={north.Id}");
        Assert.False(inactive!.Items.Single().IsActive);
        Assert.Equal(2, inactive.Items.Single().ProjectCount);

        var page1 = await director.GetFromJsonAsync<ClientReportBody>("/api/reports/clients?page_size=1&sort=client_name&direction=asc");
        var page2 = await director.GetFromJsonAsync<ClientReportBody>("/api/reports/clients?page=2&page_size=1&sort=client_name&direction=asc");
        Assert.Equal(page1!.Summary, page2!.Summary);
        Assert.NotEqual(page1.Items.Single().ClientId, page2.Items.Single().ClientId);
    }

    [Fact]
    public async Task OrderTypeReport_GroupsByIdentity_IncludingInactiveTypes()
    {
        using var director = await DirectorAsync();
        var graph = await GraphAsync(director);
        var other = await CreateOrderTypeAsync(director, "Historical Type");
        var unused = await CreateOrderTypeAsync(director, "Unused Type");
        var first = await CreateOrderAsync(director, graph.ProjectId, graph.OrderTypeId, "Current");
        var second = await CreateOrderAsync(director, graph.ProjectId, other.Id, "Old");
        var dropped = await CreateOrderAsync(director, graph.ProjectId, other.Id, "Old Cancelled");
        await SetOrderAsync(first.Id, 8m, 2m, "active", priority: "high");
        await SetOrderAsync(second.Id, 9m, 3m, "completed");
        await SetOrderAsync(dropped.Id, 30m, 1m, "cancelled");
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsync($"/api/order-types/{other.Id}/deactivate", null)).StatusCode);

        var report = await director.GetFromJsonAsync<OrderTypeReportBody>("/api/reports/order-types?sort=order_type&direction=asc");
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalItems);
        Assert.DoesNotContain(report.Items, row => row.OrderTypeId == unused.Id);
        var historical = report.Items.Single(row => row.OrderTypeId == other.Id);
        Assert.False(historical.IsActive);
        Assert.Equal("Historical Type", historical.OrderTypeName);
        Assert.Equal(1, historical.OrderCount);
        Assert.Equal(9m, historical.SellingTotal);
        Assert.Equal(6m, historical.Profit);
        Assert.Equal(17m, report.Summary.SellingTotal);
        Assert.Equal(5m, report.Summary.CostTotal);
        Assert.Equal(12m, report.Summary.ProfitTotal);

        var highOnly = await director.GetFromJsonAsync<OrderTypeReportBody>("/api/reports/order-types?priority=high");
        Assert.Equal(graph.OrderTypeId, highOnly!.Items.Single().OrderTypeId);

        var included = await director.GetFromJsonAsync<OrderTypeReportBody>("/api/reports/order-types?include_cancelled=true");
        Assert.Equal(47m, included!.Summary.SellingTotal);

        var reportsOnly = await UserAsync(director, "reports3", PermissionCatalog.Reports.View);
        var hidden = await reportsOnly.GetStringAsync("/api/reports/order-types");
        Assert.DoesNotContain("\"sellingTotal\"", hidden);
        Assert.DoesNotContain("\"profit\"", hidden);
        Assert.Contains("\"orderTypeId\"", hidden);
    }

    [Fact]
    public async Task CostsReport_GroupsByDocumentedDimensions_AndUsesExpenseDate()
    {
        using var director = await DirectorAsync();
        var north = await CreateClientAsync(director, "Cost North");
        var south = await CreateClientAsync(director, "Cost South");
        var northProject = await CreateProjectAsync(director, north.Id, "Cost North Project");
        var southProject = await CreateProjectAsync(director, south.Id, "Cost South Project");
        var brochure = await CreateOrderTypeAsync(director, "Cost Brochure");
        var poster = await CreateOrderTypeAsync(director, "Cost Poster");
        var northOrder = await CreateOrderAsync(director, northProject.Id, brochure.Id, "North Cost Order");
        var southOrder = await CreateOrderAsync(director, southProject.Id, poster.Id, "South Cost Order");
        var cancelledOrder = await CreateOrderAsync(director, northProject.Id, brochure.Id, "Cancelled Cost Order");
        await CreateCostAsync(director, northOrder.Id, "Material", null, "2026-03-15", 10m);
        await CreateCostAsync(director, northOrder.Id, "Material", null, null, 5m);
        await CreateCostAsync(director, southOrder.Id, "Materials", "Paper Co", "2026-04-01", 7m);
        await CreateCostAsync(director, cancelledOrder.Id, "Material", "Paper Co", "2026-03-20", 50m);
        await SetOrderAsync(cancelledOrder.Id, 0m, 50m, "cancelled");

        var category = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?group_by=category&sort=label&direction=asc");
        Assert.NotNull(category);
        Assert.Equal(2, category.Summary.TotalGroups);
        Assert.Equal(3, category.Summary.CostItemCount);
        Assert.Equal(22m, category.Summary.TotalAmount);
        Assert.Equal(15m, category.Items.Single(row => row.Label == "Material").TotalAmount);
        Assert.Equal(2, category.Items.Single(row => row.Label == "Material").CostItemCount);
        Assert.Equal(7m, category.Items.Single(row => row.Label == "Materials").TotalAmount);

        var supplier = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?group_by=supplier&sort=total_amount&direction=desc");
        Assert.Null(supplier!.Items.Single(row => row.TotalAmount == 15m).Label);
        Assert.Equal("Paper Co", supplier.Items.Single(row => row.TotalAmount == 7m).Label);

        var byOrder = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?group_by=order");
        var northRow = byOrder!.Items.Single(row => row.OrderId == northOrder.Id);
        Assert.Equal(15m, northRow.TotalAmount);
        Assert.Equal(northProject.Id, northRow.ProjectId);
        Assert.Equal(north.Id, northRow.ClientId);
        Assert.Equal(2, northRow.CostItemCount);

        var byProject = await director.GetFromJsonAsync<CostReportBody>($"/api/reports/costs?group_by=project&project_id={southProject.Id}");
        Assert.Equal(southProject.Id, byProject!.Items.Single().ProjectId);
        Assert.Equal(south.Id, byProject.Items.Single().ClientId);
        Assert.Equal(7m, byProject.Summary.TotalAmount);

        var byClient = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?group_by=client&sort=client_name&direction=asc");
        Assert.Equal(2, byClient!.TotalItems);
        Assert.Equal(north.Id, byClient.Items[0].ClientId);
        Assert.Equal(15m, byClient.Items[0].TotalAmount);

        var byType = await director.GetFromJsonAsync<CostReportBody>($"/api/reports/costs?group_by=order_type&order_type_id={poster.Id}");
        Assert.Equal(poster.Id, byType!.Items.Single().OrderTypeId);
        Assert.True(byType.Items.Single().OrderTypeIsActive);
        Assert.Equal(7m, byType.Items.Single().TotalAmount);

        var march = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?from_expense_date=2026-03-01&to_expense_date=2026-03-31");
        Assert.Equal(10m, march!.Summary.TotalAmount);
        Assert.Equal(1, march.Summary.CostItemCount);

        var marchIncluded = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?from_expense_date=2026-03-01&to_expense_date=2026-03-31&include_cancelled=true");
        Assert.Equal(60m, marchIncluded!.Summary.TotalAmount);
        Assert.Equal(2, marchIncluded.Summary.CostItemCount);

        var reversed = await director.GetAsync("/api/reports/costs?from_expense_date=2026-05-02&to_expense_date=2026-05-01");
        await AuthApi.AssertErrorAsync(reversed, HttpStatusCode.BadRequest, "REPORT_DATE_RANGE_INVALID");
        var badGroup = await director.GetAsync("/api/reports/costs?group_by=formula");
        await AuthApi.AssertErrorAsync(badGroup, HttpStatusCode.BadRequest, "REPORT_INVALID_FILTER");

        var empty = await director.GetFromJsonAsync<CostReportBody>("/api/reports/costs?category=Does%20Not%20Exist");
        Assert.Empty(empty!.Items);
        Assert.Equal(0, empty.Summary.CostItemCount);
        Assert.Equal(0m, empty.Summary.TotalAmount);

        var reportsOnly = await UserAsync(director, "reports4", PermissionCatalog.Reports.View);
        var costViewer = await UserAsync(director, "costview", PermissionCatalog.Reports.View, PermissionCatalog.Calculator.ViewCosts);
        var aggregateOnly = await UserAsync(director, "aggregate", PermissionCatalog.Reports.View, PermissionCatalog.Orders.ViewCostPrice);
        await AuthApi.AssertErrorAsync(await reportsOnly.GetAsync("/api/reports/costs"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await aggregateOnly.GetAsync("/api/reports/costs"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var allowed = await costViewer.GetFromJsonAsync<CostReportBody>("/api/reports/costs");
        Assert.Equal(22m, allowed!.Summary.TotalAmount);
    }

    [Fact]
    public async Task Reports_RejectAnonymousAndMissingReportsPermission_AndReturnEmptyReports()
    {
        await factory.ResetAsync();
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/reports/orders"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        var ordersOnly = await UserAsync(director, "orders", PermissionCatalog.Orders.View);
        await AuthApi.AssertErrorAsync(await ordersOnly.GetAsync("/api/reports/projects"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await ordersOnly.GetAsync("/api/reports/clients"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await ordersOnly.GetAsync("/api/reports/order-types"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await ordersOnly.GetAsync("/api/reports/costs"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        var emptyOrders = await GetOrdersAsync(director);
        Assert.Empty(emptyOrders.Items);
        Assert.Equal(0, emptyOrders.Summary.TotalOrders);
        Assert.Equal(0m, emptyOrders.Summary.ProfitTotal);
        var emptyProjects = await director.GetFromJsonAsync<ProjectReportBody>("/api/reports/projects");
        Assert.Empty(emptyProjects!.Items);
        Assert.Equal(0, emptyProjects.Summary.TotalProjects);
        Assert.Equal(0m, emptyProjects.Summary.ProfitTotal);
    }

    [Fact]
    public async Task OrdersReport_AggregatesALargeFilteredSetOnTheServer()
    {
        using var director = await DirectorAsync();
        var graph = await GraphAsync(director);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var projectId = graph.ProjectId;
        var orderTypeId = graph.OrderTypeId;
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO orders.orders (
                id, business_id, project_id, order_type_id, name, status, priority, selling_price, cost_price, created_at)
            SELECT
                gen_random_uuid(),
                'RPT-' || lpad(i::text, 6, '0'),
                {projectId},
                {orderTypeId},
                'Bulk ' || i,
                CASE WHEN i <= 10 THEN 'cancelled' ELSE 'active' END,
                'normal',
                CASE WHEN i <= 10 THEN 100.00 ELSE 10.00 END,
                CASE WHEN i <= 10 THEN 1.00 ELSE 4.00 END,
                TIMESTAMPTZ '2026-06-15 12:00:00+00'
            FROM generate_series(1, 1000) AS i
            """);

        var page1 = await GetOrdersAsync(director, "page=1&page_size=50");
        var page2 = await GetOrdersAsync(director, "page=2&page_size=50");
        Assert.Equal(990, page1.TotalItems);
        Assert.Equal(20, page1.TotalPages);
        Assert.Equal(50, page1.Items.Length);
        Assert.Equal(50, page2.Items.Length);
        Assert.Equal(9900m, page1.Summary.SellingTotal);
        Assert.Equal(3960m, page1.Summary.CostTotal);
        Assert.Equal(5940m, page1.Summary.ProfitTotal);
        Assert.Equal(page1.Summary, page2.Summary);
        Assert.Equal(page1.Items.Sum(row => row.SellingPrice), 500m);

        var included = await GetOrdersAsync(director, "include_cancelled=true&page_size=50");
        Assert.Equal(1000, included.TotalItems);
        Assert.Equal(10900m, included.Summary.SellingTotal);
        Assert.Equal(3970m, included.Summary.CostTotal);
        Assert.Equal(6930m, included.Summary.ProfitTotal);
    }

    private async Task<HttpClient> DirectorAsync()
    {
        await factory.ResetAsync();
        var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        await AuthApi.LoginAsync(client, "director", AuthApi.Password);
        return client;
    }

    private async Task<HttpClient> UserAsync(HttpClient director, string username, params string[] permissions)
    {
        var roleId = await CreateRoleAsync(director, username, permissions);
        var created = await director.PostAsJsonAsync("/api/users", new { username, password = AuthApi.OtherPassword, roleIds = new[] { roleId } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var client = factory.CreateClient();
        await AuthApi.LoginAsync(client, username, AuthApi.OtherPassword);
        return client;
    }

    private async Task<Graph> GraphAsync(HttpClient director)
    {
        var client = await CreateClientAsync(director, "Report Client");
        var project = await CreateProjectAsync(director, client.Id, "Report Project");
        var orderType = await CreateOrderTypeAsync(director, "Report Type");
        return new Graph(client.Id, project.Id, orderType.Id);
    }

    private async Task SetOrderAsync(
        Guid orderId,
        decimal selling,
        decimal cost,
        string status,
        string? priority = null,
        DateTimeOffset? createdAt = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var order = await db.Orders.SingleAsync(candidate => candidate.Id == orderId);
        order.SellingPrice = selling;
        order.CostPrice = cost;
        order.Status = status;
        if (priority is not null)
        {
            order.Priority = priority;
        }

        if (createdAt is not null)
        {
            order.CreatedAt = createdAt.Value;
        }

        await db.SaveChangesAsync();
    }

    private static async Task<OrderReportBody> GetOrdersAsync(HttpClient client, string? query = null)
    {
        var response = await client.GetAsync("/api/reports/orders" + (query is null ? string.Empty : "?" + query));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<OrderReportBody>(body, JsonSerializerOptions.Web)!;
    }

    private static async Task<IdBody> CreateClientAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/clients", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task<IdBody> CreateProjectAsync(HttpClient client, Guid clientId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { clientId, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task<IdBody> CreateOrderTypeAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/order-types", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task<IdBody> CreateOrderAsync(HttpClient client, Guid projectId, Guid orderTypeId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { projectId, orderTypeId, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task<IdBody> CreateEmployeeAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/employees", new { fullName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task CreateCostAsync(HttpClient client, Guid orderId, string category, string? supplier, string? expenseDate, decimal amount)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cost-items", new { category, supplier, expenseDate, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, params string[] permissionCodes)
    {
        var created = await client.PostAsJsonAsync("/api/roles", new { name, description = name });
        var role = (await created.Content.ReadFromJsonAsync<RoleBody>())!;
        var permissions = await client.GetFromJsonAsync<PermissionBody[]>("/api/permissions");
        var permissionIds = permissionCodes.Select(code => permissions!.Single(permission => permission.Code == code).Id).ToArray();
        var saved = await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        return role.Id;
    }

    private sealed record Graph(Guid ClientId, Guid ProjectId, Guid OrderTypeId);

    private sealed record IdBody(Guid Id);

    private sealed record RoleBody(Guid Id, string Name, string? Description, bool IsSystem, PermissionBody[] Permissions);

    private sealed record PermissionBody(Guid Id, string Code, string Name, string? Description, string Module);

    private sealed record OrderReportBody(OrderRow[] Items, int Page, int PageSize, int TotalItems, int TotalPages, OrderSummary Summary);

    private sealed record OrderRow(
        Guid OrderId,
        string OrderBusinessId,
        string OrderName,
        Guid ClientId,
        string ClientName,
        Guid ProjectId,
        string ProjectName,
        Guid OrderTypeId,
        decimal? SellingPrice,
        decimal? CostPrice,
        decimal? Profit);

    private sealed record OrderSummary(int TotalOrders, decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record ProjectReportBody(ProjectRow[] Items, int Page, int PageSize, int TotalItems, int TotalPages, ProjectSummary Summary);

    private sealed record ProjectRow(
        Guid ProjectId,
        string ProjectName,
        Guid ClientId,
        string Status,
        Guid? OwnerEmployeeId,
        string? OwnerName,
        Guid? AssigneeEmployeeId,
        string? AssigneeName,
        int OrderCount,
        decimal? SellingTotal,
        decimal? CostTotal,
        decimal? Profit);

    private sealed record ProjectSummary(int TotalProjects, int TotalOrders, decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record ClientReportBody(ClientRow[] Items, int Page, int PageSize, int TotalItems, int TotalPages, ClientSummary Summary);

    private sealed record ClientRow(
        Guid ClientId,
        string ClientName,
        bool IsActive,
        int ProjectCount,
        int OrderCount,
        decimal? SellingTotal,
        decimal? CostTotal,
        decimal? Profit);

    private sealed record ClientSummary(
        int TotalClients,
        int TotalProjects,
        int TotalOrders,
        decimal? SellingTotal,
        decimal? CostTotal,
        decimal? ProfitTotal);

    private sealed record OrderTypeReportBody(OrderTypeRow[] Items, int Page, int PageSize, int TotalItems, int TotalPages, OrderTypeSummary Summary);

    private sealed record OrderTypeRow(
        Guid OrderTypeId,
        string OrderTypeName,
        bool IsActive,
        int OrderCount,
        decimal? SellingTotal,
        decimal? CostTotal,
        decimal? Profit);

    private sealed record OrderTypeSummary(int TotalOrderTypes, int TotalOrders, decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record CostReportBody(CostRow[] Items, int Page, int PageSize, int TotalItems, int TotalPages, CostSummary Summary);

    private sealed record CostRow(
        string GroupKey,
        string? Label,
        Guid? OrderId,
        Guid? ProjectId,
        Guid? ClientId,
        Guid? OrderTypeId,
        bool? OrderTypeIsActive,
        int CostItemCount,
        decimal TotalAmount);

    private sealed record CostSummary(int TotalGroups, int CostItemCount, decimal TotalAmount);
}
