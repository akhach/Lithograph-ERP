using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class OrderTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Order_CreationRequiresAnOpenProjectAndActiveTypeAndDerivesClientAndTeam()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Samsung Armenia");
        var project = await CreateProjectAsync(director, client.Id, "New Store Opening");
        var owner = await CreateEmployeeAsync(director, "Owner Person");
        var assignee = await CreateEmployeeAsync(director, "Assignee Person");
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = owner.Id });
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = assignee.Id });
        var orderType = await CreateOrderTypeAsync(director, "UV Printing");

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = orderType.Id, name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = Guid.NewGuid(), orderTypeId = orderType.Id, name = "Missing project" }),
            HttpStatusCode.NotFound,
            "PROJECT_NOT_FOUND");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = Guid.NewGuid(), name = "Missing type" }),
            HttpStatusCode.NotFound,
            "ORDER_TYPE_NOT_FOUND");

        var completed = await CreateProjectAsync(director, client.Id, "Finished Work");
        await director.PostAsJsonAsync($"/api/projects/{completed.Id}/status", new { status = "completed" });
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = completed.Id, orderTypeId = orderType.Id, name = "Too late" }),
            HttpStatusCode.Conflict,
            "PROJECT_CLOSED");
        var cancelled = await CreateProjectAsync(director, client.Id, "Dropped Work");
        await director.PostAsJsonAsync($"/api/projects/{cancelled.Id}/status", new { status = "cancelled" });
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = cancelled.Id, orderTypeId = orderType.Id, name = "Dropped" }),
            HttpStatusCode.Conflict,
            "PROJECT_CLOSED");

        await director.PostAsync($"/api/order-types/{orderType.Id}/deactivate", null);
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Inactive type" }),
            HttpStatusCode.BadRequest,
            "ORDER_TYPE_INACTIVE");
        await director.PostAsync($"/api/order-types/{orderType.Id}/activate", null);

        var created = await CreateOrderAsync(director, project.Id, orderType.Id, "  Entrance UV Panels  ", " Print panels ", "high", "2026-11-15");
        Assert.Equal("ORD-2026-000001", created.BusinessId);
        Assert.Equal("Entrance UV Panels", created.Name);
        Assert.Equal("Print panels", created.Description);
        Assert.Equal("draft", created.Status);
        Assert.Equal("high", created.Priority);
        Assert.Equal(0m, created.SellingPrice);
        Assert.Equal(0m, created.CostPrice);
        Assert.Equal(0m, created.Profit);
        Assert.False(created.CalculatorConfigured);
        Assert.Equal(client.Id, created.Client.Id);
        Assert.Equal(client.BusinessId, created.Client.BusinessId);
        Assert.Equal(project.Id, created.Project.Id);
        Assert.Equal(owner.Id, created.Team.Owner!.Id);
        Assert.Equal(assignee.Id, created.Team.Assignee!.Id);
        Assert.Empty(created.ChecklistItems);
        Assert.Equal(0, created.ChecklistProgress.Total);
        Assert.Null(created.UpdatedAt);

        var otherOwner = await CreateEmployeeAsync(director, "Replacement Owner");
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = otherOwner.Id });
        var inherited = await director.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{created.Id}");
        Assert.Equal(otherOwner.Id, inherited!.Team.Owner!.Id);
        Assert.Equal("ORD-2026-000001", inherited.BusinessId);

        var kept = await director.PatchAsJsonAsync($"/api/orders/{created.Id}", new
        {
            projectId = project.Id,
            orderTypeId = orderType.Id,
            name = "Entrance UV Panels",
            businessId = "ORD-2026-999999",
            sellingPrice = 50,
            costPrice = 20,
            status = "active",
        });
        var edited = (await kept.Content.ReadFromJsonAsync<OrderDetailBody>())!;
        Assert.Equal("ORD-2026-000001", edited.BusinessId);
        Assert.Equal("draft", edited.Status);
        Assert.Equal(0m, edited.SellingPrice);
        Assert.Equal(0m, edited.CostPrice);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Orders.SingleAsync(order => order.Id == created.Id);
        Assert.NotNull(stored.CreatedBy);
        Assert.Equal(stored.CreatedBy, stored.UpdatedBy);
        Assert.Equal(0m, stored.SellingPrice);
        Assert.Equal(0m, stored.CostPrice);
        var orderColumns = await db.Database.SqlQueryRaw<string>(
            """
            SELECT column_name AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'orders' AND table_name = 'orders'
            """).ToListAsync();
        Assert.DoesNotContain("client_id", orderColumns);
        Assert.DoesNotContain("profit", orderColumns);
        Assert.DoesNotContain("owner_employee_id", orderColumns);
        Assert.False(await db.Database.SqlQueryRaw<string>(
            """
            SELECT table_name AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'orders' AND table_name = 'order_members'
            """).AnyAsync());
    }

    [Fact]
    public async Task Order_BusinessIdsResetEachYearStayUniqueAndAllowGaps()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Yearly Client");
        var project = await CreateProjectAsync(director, client.Id, "Yearly Project");
        var orderType = await CreateOrderTypeAsync(director, "Laser Cutting");
        var clients = Enumerable.Range(0, 8).Select(_ => factory.CreateClient()).ToArray();
        try
        {
            await Task.WhenAll(clients.Select(http => AuthApi.LoginAsync(http, "director", AuthApi.Password)));
            var responses = await Task.WhenAll(clients.Select(http =>
                http.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Concurrent" })));
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var created = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<OrderDetailBody>()));
            var numbers = created.Select(order => int.Parse(order!.BusinessId[^6..])).Order().ToArray();
            Assert.Equal(Enumerable.Range(1, 8), numbers);
            Assert.Equal(numbers.Distinct().Count(), numbers.Length);
            Assert.All(created, order => Assert.StartsWith("ORD-2026-", order!.BusinessId, StringComparison.Ordinal));
        }
        finally
        {
            foreach (var http in clients)
            {
                http.Dispose();
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        _ = await db.Database.SqlQueryRaw<long>("SELECT orders.next_order_business_id(2026) AS \"Value\"").ToListAsync();
        var afterGap = await CreateOrderAsync(director, project.Id, orderType.Id, "After Gap");
        Assert.Equal("ORD-2026-000010", afterGap.BusinessId);

        factory.SetUtcNow(new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero));
        var endOfYear = await CreateOrderAsync(director, project.Id, orderType.Id, "End of 2026");
        Assert.StartsWith("ORD-2026-", endOfYear.BusinessId, StringComparison.Ordinal);

        factory.SetUtcNow(new DateTimeOffset(2027, 1, 1, 0, 30, 0, TimeSpan.Zero));
        var nextYear = await CreateOrderAsync(director, project.Id, orderType.Id, "Start of 2027");
        var nextYearAgain = await CreateOrderAsync(director, project.Id, orderType.Id, "Second of 2027");
        Assert.Equal("ORD-2027-000001", nextYear.BusinessId);
        Assert.Equal("ORD-2027-000002", nextYearAgain.BusinessId);
    }

    [Fact]
    public async Task Order_ProjectChangesOnlyWhileDraftAndStatusPriorityFollowTheSimpleRules()
    {
        var director = await DirectorClientAsync();
        var firstClient = await CreateClientAsync(director, "First Client");
        var secondClient = await CreateClientAsync(director, "Second Client");
        var firstProject = await CreateProjectAsync(director, firstClient.Id, "First Project");
        var secondProject = await CreateProjectAsync(director, secondClient.Id, "Second Project");
        var closedProject = await CreateProjectAsync(director, secondClient.Id, "Closed Project");
        await director.PostAsJsonAsync($"/api/projects/{closedProject.Id}/status", new { status = "completed" });
        var orderType = await CreateOrderTypeAsync(director, "Installation");
        var order = await CreateOrderAsync(director, firstProject.Id, orderType.Id, "Window Work");
        Assert.Equal("normal", order.Priority);

        var moved = await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
        {
            projectId = secondProject.Id,
            orderTypeId = orderType.Id,
            name = "Window Work",
            priority = "urgent",
        });
        var movedBody = (await moved.Content.ReadFromJsonAsync<OrderDetailBody>())!;
        Assert.Equal(secondProject.Id, movedBody.Project.Id);
        Assert.Equal(secondClient.Id, movedBody.Client.Id);
        Assert.Equal("urgent", movedBody.Priority);
        Assert.Equal("ORD-2026-000001", movedBody.BusinessId);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
            {
                projectId = secondProject.Id,
                orderTypeId = orderType.Id,
                name = "Window Work",
                priority = "critical",
            }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
            {
                projectId = closedProject.Id,
                orderTypeId = orderType.Id,
                name = "Window Work",
                priority = "urgent",
            }),
            HttpStatusCode.Conflict,
            "PROJECT_CLOSED");

        foreach (var status in new[] { "active", "on_hold", "completed", "cancelled", "draft", "active" })
        {
            var response = await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(status, (await response.Content.ReadFromJsonAsync<OrderDetailBody>())!.Status);
        }

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "archived" }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");

        var item = await AddChecklistAsync(director, order.Id, "Receive artwork");
        var completed = await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "completed" });
        var completedBody = (await completed.Content.ReadFromJsonAsync<OrderDetailBody>())!;
        Assert.Equal("completed", completedBody.Status);
        Assert.Equal(1, completedBody.ChecklistProgress.Total);
        Assert.Equal(0, completedBody.ChecklistProgress.Completed);
        Assert.Contains(completedBody.ChecklistItems, candidate => candidate.Id == item.Id);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
            {
                projectId = firstProject.Id,
                orderTypeId = orderType.Id,
                name = "Window Work",
            }),
            HttpStatusCode.Conflict,
            "ORDER_PROJECT_CHANGE_NOT_ALLOWED");

        var cancelled = await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "cancelled" });
        var cancelledBody = (await cancelled.Content.ReadFromJsonAsync<OrderDetailBody>())!;
        Assert.Equal("cancelled", cancelledBody.Status);
        Assert.Single(cancelledBody.ChecklistItems);
        var reopened = await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "active" });
        Assert.Equal("active", (await reopened.Content.ReadFromJsonAsync<OrderDetailBody>())!.Status);
    }

    [Fact]
    public async Task OrderType_NamesAreUniqueIgnoringCaseAndInactiveTypesStayOnExistingOrders()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Type Client");
        var project = await CreateProjectAsync(director, client.Id, "Type Project");
        var created = await CreateOrderTypeAsync(director, "  UV Printing  ", "Flatbed work");
        Assert.Equal("UV Printing", created.Name);
        Assert.True(created.IsActive);
        Assert.Equal("Flatbed work", created.Description);

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/order-types", new { name = "uv printing" }),
            HttpStatusCode.Conflict,
            "ORDER_TYPE_NAME_ALREADY_EXISTS");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/order-types", new { name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");

        var renamed = await director.PatchAsJsonAsync($"/api/order-types/{created.Id}", new { name = "UV Printing", description = "Updated" });
        Assert.Equal("Updated", (await renamed.Content.ReadFromJsonAsync<OrderTypeBody>())!.Description);

        var order = await CreateOrderAsync(director, project.Id, created.Id, "Historical");
        await director.PostAsync($"/api/order-types/{created.Id}/deactivate", null);
        var historicalType = await director.GetFromJsonAsync<OrderTypeBody>($"/api/order-types/{created.Id}");
        Assert.False(historicalType!.IsActive);
        var historicalOrder = await director.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{order.Id}");
        Assert.Equal(created.Id, historicalOrder!.OrderType.Id);
        Assert.False(historicalOrder.OrderType.IsActive);
        var edited = await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
        {
            projectId = project.Id,
            orderTypeId = created.Id,
            name = "Historical renamed",
        });
        Assert.Equal("Historical renamed", (await edited.Content.ReadFromJsonAsync<OrderDetailBody>())!.Name);
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = created.Id, name = "New" }),
            HttpStatusCode.BadRequest,
            "ORDER_TYPE_INACTIVE");

        var activeTypes = await director.GetFromJsonAsync<OrderTypeBody[]>("/api/order-types?view=selector");
        Assert.DoesNotContain(activeTypes!, type => type.Id == created.Id);
        var allTypes = await director.GetFromJsonAsync<OrderTypeBody[]>("/api/order-types");
        Assert.Contains(allTypes!, type => type.Id == created.Id && !type.IsActive);

        await director.PostAsync($"/api/order-types/{created.Id}/activate", null);
        var again = await CreateOrderAsync(director, project.Id, created.Id, "After reactivation");
        Assert.True(again.OrderType.IsActive);
    }

    [Fact]
    public async Task Checklist_AndFolderLinks_SupportSimpleEditingWithoutFilesystemChanges()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Checklist Client");
        var project = await CreateProjectAsync(director, client.Id, "Checklist Project");
        var orderType = await CreateOrderTypeAsync(director, "Graphic Design");
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, "Artwork");
        var other = await CreateOrderAsync(director, project.Id, orderType.Id, "Other");

        var empty = await director.GetFromJsonAsync<ChecklistBody[]>($"/api/orders/{order.Id}/checklist-items");
        Assert.Empty(empty!);

        var first = await AddChecklistAsync(director, order.Id, "  Receive artwork  ");
        var second = await AddChecklistAsync(director, order.Id, "Customer approval");
        var third = await AddChecklistAsync(director, order.Id, "Production");
        Assert.Equal("Receive artwork", first.Text);
        Assert.False(first.IsCompleted);
        Assert.True(first.SortOrder < second.SortOrder);
        Assert.True(second.SortOrder < third.SortOrder);

        var edited = await director.PatchAsJsonAsync($"/api/orders/{order.Id}/checklist-items/{first.Id}", new { text = "Receive final artwork" });
        Assert.Equal("Receive final artwork", (await edited.Content.ReadFromJsonAsync<ChecklistBody>())!.Text);
        var completed = await director.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/checklist-items/{first.Id}",
            new { isCompleted = true });
        Assert.True((await completed.Content.ReadFromJsonAsync<ChecklistBody>())!.IsCompleted);
        var cleared = await director.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/checklist-items/{first.Id}",
            new { isCompleted = false });
        Assert.False((await cleared.Content.ReadFromJsonAsync<ChecklistBody>())!.IsCompleted);

        var reordered = await director.PostAsJsonAsync($"/api/orders/{order.Id}/checklist-items/reorder", new
        {
            ids = new[] { third.Id, first.Id, second.Id },
        });
        var orderIds = (await reordered.Content.ReadFromJsonAsync<ChecklistBody[]>())!.Select(item => item.Id).ToArray();
        Assert.Equal([third.Id, first.Id, second.Id], orderIds);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/orders/{other.Id}/checklist-items/{first.Id}", new { text = "Stolen" }),
            HttpStatusCode.NotFound,
            "CHECKLIST_ITEM_NOT_FOUND");

        var deleted = await director.DeleteAsync($"/api/orders/{order.Id}/checklist-items/{second.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var remaining = await director.GetFromJsonAsync<ChecklistBody[]>($"/api/orders/{order.Id}/checklist-items");
        Assert.Equal(2, remaining!.Length);
        var detail = await director.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{order.Id}");
        Assert.Equal(2, detail!.ChecklistProgress.Total);

        var folder = Path.Combine(Path.GetTempPath(), "lithograph-order-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var artwork = await AddFolderAsync(director, order.Id, "Artwork", folder);
            var production = await AddFolderAsync(director, order.Id, null, folder + "\\production");
            Assert.Equal("Artwork", artwork.Name);
            Assert.Equal(folder, artwork.Path);
            var folderEdit = await director.PatchAsJsonAsync($"/api/orders/{order.Id}/folder-links/{artwork.Id}", new
            {
                name = "Customer files",
                path = folder + "\\artwork",
            });
            Assert.Equal("Customer files", (await folderEdit.Content.ReadFromJsonAsync<FolderBody>())!.Name);
            var folderOrder = await director.PostAsJsonAsync($"/api/orders/{order.Id}/folder-links/reorder", new
            {
                ids = new[] { production.Id, artwork.Id },
            });
            Assert.Equal(production.Id, (await folderOrder.Content.ReadFromJsonAsync<FolderBody[]>())![0].Id);
            await AuthApi.AssertErrorAsync(
                await director.DeleteAsync($"/api/orders/{other.Id}/folder-links/{artwork.Id}"),
                HttpStatusCode.NotFound,
                "FOLDER_LINK_NOT_FOUND");
            var removed = await director.DeleteAsync($"/api/orders/{order.Id}/folder-links/{artwork.Id}");
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.True(Directory.Exists(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var checklistColumns = await db.Database.SqlQueryRaw<string>(
            """
            SELECT column_name AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'orders' AND table_name = 'checklist_items'
            """).ToListAsync();
        Assert.Equal(
            ["id", "is_completed", "order_id", "sort_order", "text"],
            checklistColumns.Order(StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(checklistColumns, column => column is "employee_id" or "deadline" or "priority" or "filename" or "notes");
    }

    [Fact]
    public async Task Order_ListSupportsSearchFiltersPaginationAndFinancialPermissions()
    {
        var director = await DirectorClientAsync();
        var samsung = await CreateClientAsync(director, "Samsung Armenia");
        var otherClient = await CreateClientAsync(director, "Other Client");
        var store = await CreateProjectAsync(director, samsung.Id, "Store Opening", deadline: "2026-11-30");
        var otherProject = await CreateProjectAsync(director, otherClient.Id, "Side Job");
        var owner = await CreateEmployeeAsync(director, "Filter Owner");
        var assignee = await CreateEmployeeAsync(director, "Filter Assignee");
        await director.PutAsJsonAsync($"/api/projects/{store.Id}/owner", new { employeeId = owner.Id });
        await director.PutAsJsonAsync($"/api/projects/{store.Id}/assignee", new { employeeId = assignee.Id });
        var uv = await CreateOrderTypeAsync(director, "UV Printing");
        var laser = await CreateOrderTypeAsync(director, "Laser Cutting");
        var first = await CreateOrderAsync(director, store.Id, uv.Id, "Entrance Panels", priority: "high", deadline: "2026-11-15");
        await director.PostAsJsonAsync($"/api/orders/{first.Id}/status", new { status = "active" });
        var second = await CreateOrderAsync(director, otherProject.Id, laser.Id, "Acrylic Logo", priority: "low", deadline: "2026-12-01");
        await director.PostAsJsonAsync($"/api/orders/{second.Id}/status", new { status = "completed" });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var stored = await db.Orders.SingleAsync(order => order.Id == first.Id);
            stored.SellingPrice = 120.50m;
            stored.CostPrice = 40.25m;
            await db.SaveChangesAsync();
        }

        var byBusinessId = await director.GetFromJsonAsync<OrderPageBody>($"/api/orders?search={Uri.EscapeDataString(first.BusinessId)}");
        Assert.Equal(first.Id, Assert.Single(byBusinessId!.Items).Id);
        var byClient = await director.GetFromJsonAsync<OrderPageBody>("/api/orders?search=Samsung");
        Assert.Contains(byClient!.Items, item => item.Id == first.Id);

        var clientAndStatus = await director.GetFromJsonAsync<OrderPageBody>(
            $"/api/orders?client_id={samsung.Id}&status=active");
        Assert.Equal(first.Id, Assert.Single(clientAndStatus!.Items).Id);
        var projectAndPriority = await director.GetFromJsonAsync<OrderPageBody>(
            $"/api/orders?project_id={store.Id}&priority=high");
        Assert.Equal(first.Id, Assert.Single(projectAndPriority!.Items).Id);
        var typeAndStatus = await director.GetFromJsonAsync<OrderPageBody>(
            $"/api/orders?order_type_id={laser.Id}&status=completed");
        Assert.Equal(second.Id, Assert.Single(typeAndStatus!.Items).Id);
        var ownerAndDeadline = await director.GetFromJsonAsync<OrderPageBody>(
            $"/api/orders?owner_employee_id={owner.Id}&deadline_from=2026-11-01&deadline_to=2026-11-20");
        Assert.Equal(first.Id, Assert.Single(ownerAndDeadline!.Items).Id);
        var openOnly = await director.GetFromJsonAsync<OrderPageBody>("/api/orders?status=open");
        Assert.Contains(openOnly!.Items, item => item.Id == first.Id);
        Assert.DoesNotContain(openOnly.Items, item => item.Id == second.Id);

        var page = await director.GetFromJsonAsync<OrderPageBody>("/api/orders?page=1&page_size=1&sort=business_id&direction=asc");
        Assert.Single(page!.Items);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(120.50m, page.Items[0].SellingPrice);
        Assert.Equal(40.25m, page.Items[0].CostPrice);
        Assert.Equal(80.25m, page.Items[0].Profit);

        var viewRole = await CreateRoleAsync(director, "Order Viewer", PermissionCatalog.Orders.View);
        var sellingRole = await CreateRoleAsync(director, "Selling Viewer", PermissionCatalog.Orders.View, PermissionCatalog.Orders.ViewSellingPrice);
        var costRole = await CreateRoleAsync(director, "Cost Viewer", PermissionCatalog.Orders.View, PermissionCatalog.Orders.ViewCostPrice);
        var bothRole = await CreateRoleAsync(
            director,
            "Finance Viewer",
            PermissionCatalog.Orders.View,
            PermissionCatalog.Orders.ViewSellingPrice,
            PermissionCatalog.Orders.ViewCostPrice);
        await CreateUserAsync(director, "orderviewer", viewRole);
        await CreateUserAsync(director, "orderseller", sellingRole);
        await CreateUserAsync(director, "ordercost", costRole);
        await CreateUserAsync(director, "orderboth", bothRole);

        var viewer = await LoginAsync("orderviewer");
        var viewerJson = await viewer.GetStringAsync($"/api/orders/{first.Id}");
        using (var document = JsonDocument.Parse(viewerJson))
        {
            Assert.False(document.RootElement.TryGetProperty("sellingPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
            Assert.Equal(samsung.Id, document.RootElement.GetProperty("client").GetProperty("id").GetGuid());
        }

        var viewerList = await viewer.GetStringAsync($"/api/orders?search={Uri.EscapeDataString(first.BusinessId)}");
        using (var document = JsonDocument.Parse(viewerList))
        {
            var item = document.RootElement.GetProperty("items")[0];
            Assert.False(item.TryGetProperty("sellingPrice", out _));
            Assert.False(item.TryGetProperty("costPrice", out _));
            Assert.False(item.TryGetProperty("profit", out _));
        }

        await AuthApi.AssertErrorAsync(
            await viewer.GetAsync("/api/orders?sort=selling_price"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewer.GetAsync("/api/orders?sort=cost_price"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var seller = await LoginAsync("orderseller");
        var sellerBody = await seller.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{first.Id}");
        Assert.Equal(120.50m, sellerBody!.SellingPrice);
        Assert.Null(sellerBody.CostPrice);
        Assert.Null(sellerBody.Profit);
        var sellerJson = await seller.GetStringAsync($"/api/orders/{first.Id}");
        using (var document = JsonDocument.Parse(sellerJson))
        {
            Assert.True(document.RootElement.TryGetProperty("sellingPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        var costUser = await LoginAsync("ordercost");
        var costBody = await costUser.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{first.Id}");
        Assert.Null(costBody!.SellingPrice);
        Assert.Equal(40.25m, costBody.CostPrice);
        Assert.Null(costBody.Profit);

        var both = await LoginAsync("orderboth");
        var bothBody = await both.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{first.Id}");
        Assert.Equal(120.50m, bothBody!.SellingPrice);
        Assert.Equal(40.25m, bothBody.CostPrice);
        Assert.Equal(80.25m, bothBody.Profit);
    }

    [Fact]
    public async Task Order_EndpointsRequireAuthenticationAndTheMatchingPermission()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Protected Client");
        var project = await CreateProjectAsync(director, client.Id, "Protected Project");
        var orderType = await CreateOrderTypeAsync(director, "Outsourced Work");
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, "Protected Order");
        var item = await AddChecklistAsync(director, order.Id, "Pack");
        var link = await AddFolderAsync(director, order.Id, "Files", "\\\\server\\orders\\protected");

        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/orders"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/orders/{order.Id}"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.PostAsJsonAsync("/api/orders", new { name = "No" }), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/order-types"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/orders/{order.Id}/checklist-items"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/orders/{order.Id}/folder-links"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        await CreateUserAsync(director, "viewonly", await CreateRoleAsync(director, "View Only", PermissionCatalog.Orders.View));
        await CreateUserAsync(director, "createonly", await CreateRoleAsync(director, "Create Only", PermissionCatalog.Orders.Create));
        await CreateUserAsync(director, "editonly", await CreateRoleAsync(director, "Edit Only", PermissionCatalog.Orders.Edit));
        await CreateUserAsync(director, "statusonly", await CreateRoleAsync(director, "Status Only", PermissionCatalog.Orders.ChangeStatus));
        await CreateUserAsync(director, "checkonly", await CreateRoleAsync(director, "Checklist Only", PermissionCatalog.Orders.ManageChecklist));
        await CreateUserAsync(director, "folderonly", await CreateRoleAsync(director, "Folder Only", PermissionCatalog.Orders.ManageFolderLinks));
        await CreateUserAsync(director, "typeonly", await CreateRoleAsync(director, "Type Only", PermissionCatalog.Orders.ManageTypes));

        var viewOnly = await LoginAsync("viewonly");
        Assert.Equal(HttpStatusCode.OK, (await viewOnly.GetAsync($"/api/orders/{order.Id}")).StatusCode);
        await AuthApi.AssertErrorAsync(
            await viewOnly.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewOnly.PatchAsJsonAsync($"/api/orders/{order.Id}", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewOnly.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "active" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewOnly.PostAsJsonAsync($"/api/orders/{order.Id}/checklist-items", new { text = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewOnly.PostAsJsonAsync($"/api/orders/{order.Id}/folder-links", new { path = "\\\\server\\nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewOnly.PostAsJsonAsync("/api/order-types", new { name = "Nope Type" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var createOnly = await LoginAsync("createonly");
        await AuthApi.AssertErrorAsync(await createOnly.GetAsync("/api/orders"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var created = await createOnly.PostAsJsonAsync("/api/orders", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Created without view" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var editOnly = await LoginAsync("editonly");
        await AuthApi.AssertErrorAsync(
            await editOnly.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "active" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        var statusOnly = await LoginAsync("statusonly");
        await AuthApi.AssertErrorAsync(
            await statusOnly.PatchAsJsonAsync($"/api/orders/{order.Id}", new { projectId = project.Id, orderTypeId = orderType.Id, name = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        var checklistOnly = await LoginAsync("checkonly");
        await AuthApi.AssertErrorAsync(
            await checklistOnly.DeleteAsync($"/api/orders/{order.Id}/folder-links/{link.Id}"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        var folderOnly = await LoginAsync("folderonly");
        await AuthApi.AssertErrorAsync(
            await folderOnly.DeleteAsync($"/api/orders/{order.Id}/checklist-items/{item.Id}"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        var typeOnly = await LoginAsync("typeonly");
        await AuthApi.AssertErrorAsync(await typeOnly.GetAsync("/api/orders"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var typeCreated = await typeOnly.PostAsJsonAsync("/api/order-types", new { name = "Managed Type" });
        Assert.Equal(HttpStatusCode.Created, typeCreated.StatusCode);
    }

    [Fact]
    public async Task Order_DatabaseRejectsDuplicateBusinessIdsDuplicateTypeNamesNegativePricesAndBrokenForeignKeys()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Constraint Client");
        var project = await CreateProjectAsync(director, client.Id, "Constraint Project");
        var orderType = await CreateOrderTypeAsync(director, "CNC Routing");
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, "Constraint Order");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = order.BusinessId,
            ProjectId = project.Id,
            OrderTypeId = orderType.Id,
            Name = "Duplicate ID",
            Status = OrderStatuses.Draft,
            Priority = OrderPriorities.Normal,
            SellingPrice = 0,
            CostPrice = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.OrderTypes.Add(new OrderType
        {
            Id = Guid.NewGuid(),
            Name = "cnc routing",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var stored = await db.Orders.SingleAsync(candidate => candidate.Id == order.Id);
        stored.SellingPrice = -1;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            BusinessId = "ORD-2026-000099",
            ProjectId = Guid.NewGuid(),
            OrderTypeId = orderType.Id,
            Name = "Missing project",
            Status = OrderStatuses.Draft,
            Priority = OrderPriorities.Normal,
            SellingPrice = 0,
            CostPrice = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.ChecklistItems.Add(new ChecklistItem
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Text = "Orphan",
            IsCompleted = false,
            SortOrder = 10,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var projectRow = await db.Projects.SingleAsync(candidate => candidate.Id == project.Id);
        db.Projects.Remove(projectRow);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private async Task<HttpClient> DirectorClientAsync()
    {
        await factory.ResetAsync();
        var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        await AuthApi.LoginAsync(client, "director", AuthApi.Password);
        return client;
    }

    private async Task<HttpClient> LoginAsync(string username)
    {
        var client = factory.CreateClient();
        await AuthApi.LoginAsync(client, username, AuthApi.OtherPassword);
        return client;
    }

    private static async Task CreateUserAsync(HttpClient client, string username, Guid roleId)
    {
        var response = await client.PostAsJsonAsync("/api/users", new
        {
            username,
            password = AuthApi.OtherPassword,
            roleIds = new[] { roleId },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, params string[] permissionCodes)
    {
        var created = await client.PostAsJsonAsync("/api/roles", new { name, description = name });
        var role = (await created.Content.ReadFromJsonAsync<RoleBody>())!;
        var permissions = await client.GetFromJsonAsync<PermissionBody[]>("/api/permissions");
        var permissionIds = permissionCodes.Select(code => permissions!.Single(permission => permission.Code == code).Id).ToArray();
        await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds });
        return role.Id;
    }

    private static async Task<ClientDetailBody> CreateClientAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/clients", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClientDetailBody>())!;
    }

    private static async Task<EmployeeBody> CreateEmployeeAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/employees", new { fullName, position = fullName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmployeeBody>())!;
    }

    private static async Task<ProjectDetailBody> CreateProjectAsync(HttpClient client, Guid clientId, string name, string? deadline = null)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { clientId, name, deadline });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectDetailBody>())!;
    }

    private static async Task<OrderTypeBody> CreateOrderTypeAsync(HttpClient client, string name, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/order-types", new { name, description });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderTypeBody>())!;
    }

    private static async Task<OrderDetailBody> CreateOrderAsync(
        HttpClient client,
        Guid projectId,
        Guid orderTypeId,
        string name,
        string? description = null,
        string? priority = null,
        string? deadline = null)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { projectId, orderTypeId, name, description, priority, deadline });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDetailBody>())!;
    }

    private static async Task<ChecklistBody> AddChecklistAsync(HttpClient client, Guid orderId, string text)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/checklist-items", new { text });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ChecklistBody>())!;
    }

    private static async Task<FolderBody> AddFolderAsync(HttpClient client, Guid orderId, string? name, string path)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/folder-links", new { name, path });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FolderBody>())!;
    }

    private sealed record ClientDetailBody(Guid Id, string BusinessId, string Name);

    private sealed record EmployeeBody(Guid Id, string FullName);

    private sealed record ProjectDetailBody(Guid Id, string BusinessId, string Name);

    private sealed record OrderTypeBody(Guid Id, string Name, string? Description, bool IsActive);

    private sealed record TypeSummaryBody(Guid Id, string Name, bool IsActive);

    private sealed record ClientBody(Guid Id, string BusinessId, string Name);

    private sealed record ProjectContextBody(Guid Id, string BusinessId, string Name, string Status, DateOnly? Deadline);

    private sealed record PersonBody(Guid Id, string FullName, string? Position, bool IsActive);

    private sealed record TeamBody(PersonBody? Owner, PersonBody? Assignee, PersonBody[] Participants, PersonBody[] Observers);

    private sealed record ProgressBody(int Completed, int Total);

    private sealed record ChecklistBody(Guid Id, string Text, bool IsCompleted, int SortOrder);

    private sealed record FolderBody(Guid Id, string? Name, string Path, int SortOrder);

    private sealed record OrderDetailBody(
        Guid Id,
        string BusinessId,
        ProjectContextBody Project,
        ClientBody Client,
        TypeSummaryBody OrderType,
        string Name,
        string? Description,
        string Status,
        string Priority,
        DateOnly? Deadline,
        string? PreviewImagePath,
        TeamBody Team,
        bool CalculatorConfigured,
        ProgressBody ChecklistProgress,
        ChecklistBody[] ChecklistItems,
        FolderBody[] FolderLinks,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        decimal? SellingPrice,
        decimal? CostPrice,
        decimal? Profit);

    private sealed record OrderListBody(
        Guid Id,
        string BusinessId,
        string Name,
        decimal? SellingPrice,
        decimal? CostPrice,
        decimal? Profit);

    private sealed record OrderPageBody(OrderListBody[] Items, int Page, int PageSize, int TotalItems, int TotalPages);
}
