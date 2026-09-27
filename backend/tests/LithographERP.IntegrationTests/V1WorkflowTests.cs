using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class V1WorkflowTests(LithographApiFactory factory)
{
    private static readonly string[] ExpectedTables =
    [
        "auth.users",
        "auth.roles",
        "auth.permissions",
        "auth.user_roles",
        "auth.role_permissions",
        "auth.sessions",
        "employees.employees",
        "clients.clients",
        "projects.projects",
        "projects.project_members",
        "orders.order_types",
        "orders.orders",
        "orders.checklist_items",
        "orders.folder_links",
        "calculator.templates",
        "calculator.template_versions",
        "calculator.order_calculators",
        "calculator.cost_items",
    ];

    [Fact]
    public async Task ReleaseChain_PreservesIdentityHistoryAndFinancialInvariants()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();

        var setup = await director.GetFromJsonAsync<SetupStatusBody>("/api/auth/setup-status");
        Assert.True(setup!.RequiresSetup);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            Assert.Equal(0, await db.Users.CountAsync());
            Assert.Equal(40, PermissionCatalog.All.Count);
            Assert.Equal(40, PermissionCatalog.Codes.Count);
            Assert.Equal(40, await db.Permissions.CountAsync());
            Assert.Equal(1, await db.Roles.CountAsync(role => role.NormalizedName == DirectorRole.NormalizedName && role.IsSystem));
        }

        await AuthApi.SetupAsync(director);
        var loggedIn = await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        var me = await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me");
        Assert.Equal(loggedIn.Id, me!.Id);
        Assert.Equal("director", me.Username);
        Assert.Contains(DirectorRole.Name, me.Roles);
        Assert.Equal(40, me.Permissions.Length);
        var meBody = await director.GetStringAsync("/api/auth/me");
        Assert.DoesNotContain("passwordHash", meBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"password\"", meBody);
        Assert.DoesNotContain("tokenHash", meBody, StringComparison.OrdinalIgnoreCase);

        var employee = await CreateEmployeeAsync(director, "Aline Operator");
        Assert.Null(employee.LinkedUser);
        var account = await CreateUserAsync(director, "aline");
        var linked = await director.PostAsJsonAsync($"/api/employees/{employee.Id}/link-user", new { userId = account.Id });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal(account.Id, (await linked.Content.ReadFromJsonAsync<EmployeeDetailBody>())!.LinkedUser!.Id);

        var otherEmployee = await CreateEmployeeAsync(director, "Second Person");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/employees/{otherEmployee.Id}/link-user", new { userId = account.Id }),
            HttpStatusCode.Conflict,
            "USER_ALREADY_LINKED_TO_EMPLOYEE");

        using (var aline = factory.CreateClient())
        {
            await AuthApi.LoginAsync(aline, "aline", AuthApi.OtherPassword);
            var deactivatedEmployee = await director.PostAsync($"/api/employees/{employee.Id}/deactivate", null);
            Assert.False((await deactivatedEmployee.Content.ReadFromJsonAsync<EmployeeDetailBody>())!.IsActive);
            Assert.Equal(HttpStatusCode.OK, (await aline.GetAsync("/api/auth/me")).StatusCode);
            Assert.True((await director.GetFromJsonAsync<UserBody>($"/api/users/{account.Id}"))!.IsActive);

            await director.PostAsync($"/api/employees/{employee.Id}/activate", null);
            var deactivatedUser = await director.PostAsync($"/api/users/{account.Id}/deactivate", null);
            Assert.False((await deactivatedUser.Content.ReadFromJsonAsync<UserBody>())!.IsActive);
            Assert.True((await director.GetFromJsonAsync<EmployeeDetailBody>($"/api/employees/{employee.Id}"))!.IsActive);
            await AuthApi.AssertErrorAsync(await aline.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized, "SESSION_INVALID");
        }

        await director.PostAsync($"/api/users/{account.Id}/activate", null);

        var client = await CreateClientAsync(director, "North Press");
        Assert.Equal("CL-000001", client.BusinessId);
        var renamed = await director.PatchAsJsonAsync($"/api/clients/{client.Id}", new { name = "North Press Studio", phone = "010-000", contactPerson = "Mara" });
        var renamedClient = (await renamed.Content.ReadFromJsonAsync<ClientBody>())!;
        Assert.Equal("CL-000001", renamedClient.BusinessId);
        Assert.Equal("North Press Studio", renamedClient.Name);

        var owner = await CreateEmployeeAsync(director, "Mara Owner");
        var assignee = await CreateEmployeeAsync(director, "Narek Assignee");
        var project = await CreateProjectAsync(director, client.Id, "Catalog");
        Assert.Equal("PRJ-2026-000001", project.BusinessId);
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = owner.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = assignee.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsync($"/api/projects/{project.Id}/participants/{owner.Id}", null)).StatusCode);

        var template = await CreateTemplateAsync(director, "UV Printing Calculator");
        var draftId = template.DraftVersionId!.Value;
        await SaveDraftAsync(director, template.Id, draftId, Price("System.Diagnostics.Process.Start(\"cmd\")"));
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/calculator-templates/{template.Id}/versions/{draftId}/publish", null),
            HttpStatusCode.BadRequest,
            "TEMPLATE_VALIDATION_FAILED");
        await SaveDraftAsync(director, template.Id, draftId, Cycle());
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/calculator-templates/{template.Id}/versions/{draftId}/publish", null),
            HttpStatusCode.BadRequest,
            "TEMPLATE_VALIDATION_FAILED");
        await SaveDraftAsync(director, template.Id, draftId, Price("quantity * 50000"));
        var published = await PublishAsync(director, template.Id, draftId);
        Assert.Equal(1, published.VersionNumber);
        Assert.Equal("published", published.Status);
        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync(
                $"/api/calculator-templates/{template.Id}/versions/{published.Id}",
                new { definition = Price("quantity * 1") }),
            HttpStatusCode.Conflict,
            "TEMPLATE_VERSION_IMMUTABLE");

        var orderType = await CreateOrderTypeAsync(director, "UV Printing", template.Id);
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, "Entrance Panels");
        Assert.Equal("ORD-2026-000001", order.BusinessId);
        Assert.Equal(client.Id, order.Client.Id);
        Assert.Equal(owner.Id, order.Team.Owner!.Id);
        Assert.Equal(assignee.Id, order.Team.Assignee!.Id);
        Assert.Contains(order.Team.Participants, person => person.Id == owner.Id);

        var replacement = await CreateEmployeeAsync(director, "Replacement Owner");
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = replacement.Id })).StatusCode);
        var afterTeamChange = await GetOrderAsync(director, order.Id);
        Assert.Equal(replacement.Id, afterTeamChange.Team.Owner!.Id);
        Assert.Equal(assignee.Id, afterTeamChange.Team.Assignee!.Id);

        var urgent = await director.PatchAsJsonAsync($"/api/orders/{order.Id}", new
        {
            projectId = project.Id,
            orderTypeId = orderType.Id,
            name = "Entrance Panels",
            priority = "urgent",
            deadline = "2026-09-26",
        });
        Assert.Equal(HttpStatusCode.OK, urgent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "active" })).StatusCode);

        var opened = await OpenAsync(director, order.Id);
        Assert.Equal(published.Id, opened.TemplateVersion.Id);
        Assert.Equal(1, opened.TemplateVersion.VersionNumber);
        var saved = await SaveAsync(director, order.Id, new { quantity = 10 }, opened.UpdatedAt);
        Assert.Equal(500_000m, saved.SellingPrice);
        Assert.Equal(500_000m, (await GetOrderAsync(director, order.Id)).SellingPrice);

        var manipulated = await director.PatchAsJsonAsync($"/api/orders/{order.Id}/calculator", new
        {
            fieldValues = new { quantity = 10 },
            sellingPrice = 1,
            updatedAt = saved.UpdatedAt,
        });
        Assert.Equal(HttpStatusCode.OK, manipulated.StatusCode);
        Assert.Equal(500_000m, (await manipulated.Content.ReadFromJsonAsync<CalculatorBody>())!.SellingPrice);

        var incomplete = await SaveAsync(director, order.Id, new { }, saved.UpdatedAt);
        Assert.False(incomplete.CalculationComplete);
        Assert.Equal(0m, incomplete.SellingPrice);
        Assert.Equal(0m, (await GetOrderAsync(director, order.Id)).SellingPrice);
        var restored = await SaveAsync(director, order.Id, new { quantity = 10 }, incomplete.UpdatedAt);
        Assert.Equal(500_000m, restored.SellingPrice);

        var secondDraft = await (await director.PostAsync($"/api/calculator-templates/{template.Id}/versions", null)).Content.ReadFromJsonAsync<VersionBody>();
        await SaveDraftAsync(director, template.Id, secondDraft!.Id, Price("quantity * 50000 + 1"));
        var publishedSecond = await PublishAsync(director, template.Id, secondDraft.Id);
        Assert.Equal(2, publishedSecond.VersionNumber);
        var reopened = await OpenAsync(director, order.Id);
        Assert.Equal(published.Id, reopened.TemplateVersion.Id);
        Assert.Equal(500_000m, reopened.SellingPrice);

        var orderB = await CreateOrderAsync(director, project.Id, orderType.Id, "Follow-up Panel");
        var calculatorB = await OpenAsync(director, orderB.Id);
        Assert.Equal(publishedSecond.Id, calculatorB.TemplateVersion.Id);
        Assert.Equal(2, calculatorB.TemplateVersion.VersionNumber);

        var material = await CreateCostAsync(director, order.Id, "Material", 200_000m);
        Assert.Equal(200_000m, (await GetOrderAsync(director, order.Id)).CostPrice);
        var transport = await CreateCostAsync(director, order.Id, "Transport", 100_000m);
        Assert.Equal(300_000m, (await GetOrderAsync(director, order.Id)).CostPrice);
        var editedCost = await director.PatchAsJsonAsync($"/api/orders/{order.Id}/cost-items/{material.Id}", new { category = "Material", amount = 250_000m });
        Assert.Equal(HttpStatusCode.OK, editedCost.StatusCode);
        Assert.Equal(350_000m, (await GetOrderAsync(director, order.Id)).CostPrice);
        Assert.Equal(HttpStatusCode.NoContent, (await director.DeleteAsync($"/api/orders/{order.Id}/cost-items/{transport.Id}")).StatusCode);
        Assert.Equal(250_000m, (await GetOrderAsync(director, order.Id)).CostPrice);
        await CreateCostAsync(director, order.Id, "Transport", 50_000m);
        Assert.Equal(300_000m, (await GetOrderAsync(director, order.Id)).CostPrice);

        var reset = await director.PostAsJsonAsync($"/api/orders/{order.Id}/calculator/reset", new { updatedAt = reopened.UpdatedAt });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var resetBody = (await reset.Content.ReadFromJsonAsync<CalculatorBody>())!;
        Assert.Equal(0m, resetBody.SellingPrice);
        Assert.Equal(published.Id, resetBody.TemplateVersion.Id);
        var afterReset = await GetOrderAsync(director, order.Id);
        Assert.Equal(0m, afterReset.SellingPrice);
        Assert.Equal(300_000m, afterReset.CostPrice);
        Assert.Equal(2, (await director.GetFromJsonAsync<CostListBody>($"/api/orders/{order.Id}/cost-items"))!.Items.Length);
        var pricedAgain = await SaveAsync(director, order.Id, new { quantity = 10 }, resetBody.UpdatedAt);
        Assert.Equal(500_000m, pricedAgain.SellingPrice);
        Assert.Equal(300_000m, (await GetOrderAsync(director, order.Id)).CostPrice);
        Assert.Equal(200_000m, (await GetOrderAsync(director, order.Id)).Profit);

        var checklist = await director.PostAsJsonAsync($"/api/orders/{order.Id}/checklist-items", new { text = "Receive artwork" });
        var checklistItem = (await checklist.Content.ReadFromJsonAsync<ChecklistBody>())!;
        var completed = await director.PatchAsJsonAsync($"/api/orders/{order.Id}/checklist-items/{checklistItem.Id}", new { isCompleted = true });
        Assert.True((await completed.Content.ReadFromJsonAsync<ChecklistBody>())!.IsCompleted);
        var folder = await director.PostAsJsonAsync($"/api/orders/{order.Id}/folder-links", new { name = "Artwork", path = @"\\server\orders\2026\ORD-2026-000001" });
        Assert.Equal(@"\\server\orders\2026\ORD-2026-000001", (await folder.Content.ReadFromJsonAsync<FolderBody>())!.Path);

        var dashboardBody = await director.GetStringAsync("/api/dashboard");
        Assert.DoesNotContain("sellingPrice", dashboardBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("costPrice", dashboardBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"profit\"", dashboardBody);
        var dashboard = JsonSerializer.Deserialize<DashboardBody>(dashboardBody, JsonSerializerOptions.Web)!;
        Assert.Contains(dashboard.ActiveOrders!, row => row.Id == order.Id);
        Assert.Contains(dashboard.UrgentOrders!, row => row.Id == order.Id);
        Assert.Contains(dashboard.OverdueOrders!, row => row.Id == order.Id);
        Assert.DoesNotContain(dashboard.DueSoonOrders!, row => row.Id == order.Id);
        Assert.DoesNotContain(dashboard.OverdueOrders!, row => row.Id == orderB.Id);

        Assert.Equal(HttpStatusCode.OK, (await director.PostAsJsonAsync($"/api/orders/{order.Id}/status", new { status = "completed" })).StatusCode);
        var completedOrder = await GetOrderAsync(director, order.Id);
        Assert.Equal("completed", completedOrder.Status);
        Assert.Equal(500_000m, completedOrder.SellingPrice);
        Assert.Equal(300_000m, completedOrder.CostPrice);
        Assert.Single(completedOrder.ChecklistItems);
        Assert.Single(completedOrder.FolderLinks);
        Assert.True(completedOrder.CalculatorConfigured);

        var ordersReport = await GetOrdersAsync(director, "search=Entrance&page=1&page_size=1");
        var ordersReportPage2 = await GetOrdersAsync(director, "search=Entrance&page=2&page_size=1");
        Assert.Equal(order.Id, ordersReport.Items.Single().OrderId);
        Assert.Equal(500_000m, ordersReport.Summary.SellingTotal);
        Assert.Equal(300_000m, ordersReport.Summary.CostTotal);
        Assert.Equal(200_000m, ordersReport.Summary.ProfitTotal);
        Assert.Equal(ordersReport.Summary.SellingTotal, ordersReportPage2.Summary.SellingTotal);
        Assert.Equal(ordersReport.Summary.CostTotal, ordersReportPage2.Summary.CostTotal);
        Assert.Equal(ordersReport.Summary.ProfitTotal, ordersReportPage2.Summary.ProfitTotal);
        Assert.Empty(ordersReportPage2.Items);

        var projectsReport = await director.GetFromJsonAsync<ProjectReportBody>($"/api/reports/projects?client_id={client.Id}");
        Assert.Equal(500_000m, projectsReport!.Summary.SellingTotal);
        Assert.Equal(300_000m, projectsReport.Summary.CostTotal);
        Assert.Equal(200_000m, projectsReport.Summary.ProfitTotal);
        var clientsReport = await director.GetFromJsonAsync<ClientReportBody>($"/api/reports/clients?client_id={client.Id}");
        Assert.Equal(500_000m, clientsReport!.Items.Single().SellingTotal);
        Assert.Equal(200_000m, clientsReport.Summary.ProfitTotal);
        var typesReport = await director.GetFromJsonAsync<OrderTypeReportBody>($"/api/reports/order-types?order_type_id={orderType.Id}");
        Assert.Equal(500_000m, typesReport!.Items.Single().SellingTotal);
        var costsReport = await director.GetFromJsonAsync<CostReportBody>($"/api/reports/costs?order_id={order.Id}&group_by=category");
        Assert.Equal(300_000m, costsReport!.Summary.TotalAmount);

        var missing = await director.GetAsync($"/api/orders/{Guid.NewGuid()}");
        var missingBody = await missing.Content.ReadAsStringAsync();
        await AuthApi.AssertErrorAsync(missing, HttpStatusCode.NotFound, "ORDER_NOT_FOUND");
        Assert.Contains("\"message\"", missingBody);
        Assert.Contains("\"errors\"", missingBody);
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/clients", new { name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");

        var restrictedRole = await CreateRoleAsync(
            director,
            "Restricted",
            PermissionCatalog.Orders.View,
            PermissionCatalog.Reports.View,
            PermissionCatalog.Projects.View);
        await director.PostAsJsonAsync("/api/users", new { username = "restricted", password = AuthApi.OtherPassword, roleIds = new[] { restrictedRole } });
        using var restricted = factory.CreateClient();
        await AuthApi.LoginAsync(restricted, "restricted", AuthApi.OtherPassword);
        var hidden = await restricted.GetStringAsync($"/api/orders/{order.Id}");
        Assert.DoesNotContain("sellingPrice", hidden);
        Assert.DoesNotContain("costPrice", hidden);
        Assert.DoesNotContain("\"profit\"", hidden);
        await AuthApi.AssertErrorAsync(await restricted.GetAsync($"/api/orders/{order.Id}/cost-items"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var hiddenReport = await restricted.GetStringAsync("/api/reports/orders?search=Entrance");
        Assert.DoesNotContain("sellingTotal", hiddenReport);
        Assert.DoesNotContain("costTotal", hiddenReport);
        Assert.DoesNotContain("profitTotal", hiddenReport);
        await AuthApi.AssertErrorAsync(await restricted.GetAsync($"/api/reports/costs?order_id={order.Id}"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var stored = await db.Orders.AsNoTracking().SingleAsync(candidate => candidate.Id == order.Id);
            Assert.Equal(500_000m, stored.SellingPrice);
            Assert.Equal(300_000m, stored.CostPrice);
            Assert.Equal(published.Id, (await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == order.Id)).TemplateVersionId);
            Assert.Equal(publishedSecond.Id, (await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == orderB.Id)).TemplateVersionId);
            Assert.Equal(stored.CostPrice, await db.CostItems.Where(item => item.OrderId == order.Id).SumAsync(item => item.Amount));
            var tables = await QueryAsync(db, """
                SELECT table_schema || '.' || table_name
                FROM information_schema.tables
                WHERE table_schema IN ('auth', 'employees', 'clients', 'projects', 'orders', 'calculator')
                  AND table_type = 'BASE TABLE'
                """);
            Assert.Equal(ExpectedTables.OrderBy(name => name, StringComparer.Ordinal), tables.OrderBy(name => name, StringComparer.Ordinal));
            Assert.Equal(0, await ScalarAsync(db, """
                SELECT COUNT(*)::int
                FROM information_schema.columns
                WHERE table_schema = 'orders' AND table_name = 'orders' AND column_name = 'profit'
                """));
            Assert.Equal(1, await ScalarAsync(db, """
                SELECT COUNT(*)::int
                FROM pg_trigger
                WHERE tgname = 'template_versions_immutable' AND NOT tgisinternal
                """));
            Assert.Equal(1, await ScalarAsync(db, """
                SELECT COUNT(*)::int
                FROM pg_proc
                JOIN pg_namespace ON pg_proc.pronamespace = pg_namespace.oid
                WHERE nspname = 'calculator' AND proname = 'prevent_template_version_mutation'
                """));
            var permissionCodes = await QueryAsync(db, "SELECT code FROM auth.permissions");
            Assert.Equal(40, permissionCodes.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(PermissionCatalog.Codes.OrderBy(code => code, StringComparer.Ordinal), permissionCodes.OrderBy(code => code, StringComparer.Ordinal));
        }

        Assert.Equal(HttpStatusCode.NoContent, (await director.PostAsync("/api/auth/logout", null)).StatusCode);
        await AuthApi.AssertErrorAsync(await director.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        Assert.Equal("director", (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!.Username);
    }

    private static async Task<EmployeeDetailBody> CreateEmployeeAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/employees", new { fullName, position = fullName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmployeeDetailBody>())!;
    }

    private static async Task<UserBody> CreateUserAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { username, password = AuthApi.OtherPassword });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserBody>())!;
    }

    private static async Task<ClientBody> CreateClientAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/clients", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClientBody>())!;
    }

    private static async Task<ProjectBody> CreateProjectAsync(HttpClient client, Guid clientId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { clientId, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectBody>())!;
    }

    private static async Task<TemplateBody> CreateTemplateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/calculator-templates", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TemplateBody>())!;
    }

    private static async Task SaveDraftAsync(HttpClient client, Guid templateId, Guid versionId, CalculatorTemplateDefinition definition)
    {
        var response = await client.PatchAsJsonAsync($"/api/calculator-templates/{templateId}/versions/{versionId}", new { definition });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<VersionBody> PublishAsync(HttpClient client, Guid templateId, Guid versionId)
    {
        var response = await client.PostAsync($"/api/calculator-templates/{templateId}/versions/{versionId}/publish", null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<VersionBody>(body, JsonSerializerOptions.Web)!;
    }

    private static async Task<OrderTypeBody> CreateOrderTypeAsync(HttpClient client, string name, Guid templateId)
    {
        var response = await client.PostAsJsonAsync("/api/order-types", new { name, calculatorTemplateId = templateId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderTypeBody>())!;
    }

    private static async Task<OrderDetailBody> CreateOrderAsync(HttpClient client, Guid projectId, Guid orderTypeId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { projectId, orderTypeId, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDetailBody>())!;
    }

    private static async Task<OrderDetailBody> GetOrderAsync(HttpClient client, Guid orderId) =>
        (await client.GetFromJsonAsync<OrderDetailBody>($"/api/orders/{orderId}"))!;

    private static async Task<CalculatorBody> OpenAsync(HttpClient client, Guid orderId) =>
        (await client.GetFromJsonAsync<CalculatorBody>($"/api/orders/{orderId}/calculator"))!;

    private static async Task<CalculatorBody> SaveAsync(HttpClient client, Guid orderId, object fieldValues, DateTimeOffset updatedAt)
    {
        var response = await client.PatchAsJsonAsync($"/api/orders/{orderId}/calculator", new { fieldValues, updatedAt });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<CalculatorBody>(body, JsonSerializerOptions.Web)!;
    }

    private static async Task<CostItemBody> CreateCostAsync(HttpClient client, Guid orderId, string category, decimal amount)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cost-items", new { category, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CostItemBody>())!;
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, params string[] permissionCodes)
    {
        var created = await client.PostAsJsonAsync("/api/roles", new { name, description = name });
        var role = (await created.Content.ReadFromJsonAsync<RoleBody>())!;
        var permissions = await client.GetFromJsonAsync<PermissionBody[]>("/api/permissions");
        var ids = permissionCodes.Select(code => permissions!.Single(permission => permission.Code == code).Id).ToArray();
        var assigned = await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds = ids });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        return role.Id;
    }

    private static async Task<OrderReportBody> GetOrdersAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync("/api/reports/orders?" + query);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<OrderReportBody>(body, JsonSerializerOptions.Web)!;
    }

    private static async Task<List<string>> QueryAsync(LithographDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<int> ScalarAsync(LithographDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static CalculatorTemplateDefinition Price(string formula) => new()
    {
        SchemaVersion = 1,
        SellingPriceFieldKey = "selling_price",
        Elements =
        [
            new CalculatorElementDefinition
            {
                Id = "quantity",
                Type = "number_input",
                Key = "quantity",
                Label = "Quantity",
                Visibility = "general",
            },
            new CalculatorElementDefinition
            {
                Id = "selling",
                Type = "calculated_field",
                Key = "selling_price",
                Label = "Selling Price",
                Visibility = "selling",
                Formula = formula,
            },
        ],
    };

    private static CalculatorTemplateDefinition Cycle() => new()
    {
        SchemaVersion = 1,
        SellingPriceFieldKey = "selling_price",
        Elements =
        [
            new CalculatorElementDefinition
            {
                Id = "selling",
                Type = "calculated_field",
                Key = "selling_price",
                Label = "Selling Price",
                Visibility = "selling",
                Formula = "other + 1",
            },
            new CalculatorElementDefinition
            {
                Id = "other",
                Type = "calculated_field",
                Key = "other",
                Label = "Other",
                Visibility = "selling",
                Formula = "selling_price + 1",
            },
        ],
    };

    private sealed record EmployeeDetailBody(Guid Id, bool IsActive, LinkedUserBody? LinkedUser);

    private sealed record LinkedUserBody(Guid Id, string Username, bool IsActive);

    private sealed record ClientBody(Guid Id, string BusinessId, string Name);

    private sealed record ProjectBody(Guid Id, string BusinessId, string Name);

    private sealed record TemplateBody(Guid Id, Guid? DraftVersionId);

    private sealed record VersionBody(Guid Id, int VersionNumber, string Status);

    private sealed record OrderTypeBody(Guid Id, string Name);

    private sealed record OrderDetailBody(
        Guid Id,
        string BusinessId,
        string Status,
        ClientRef Client,
        TeamBody Team,
        decimal? SellingPrice,
        decimal? CostPrice,
        decimal? Profit,
        bool CalculatorConfigured,
        ChecklistBody[] ChecklistItems,
        FolderBody[] FolderLinks);

    private sealed record ClientRef(Guid Id, string BusinessId, string Name);

    private sealed record TeamBody(PersonBody? Owner, PersonBody? Assignee, PersonBody[] Participants, PersonBody[] Observers);

    private sealed record PersonBody(Guid Id, string FullName, bool IsActive);

    private sealed record CalculatorBody(
        Guid CalculatorId,
        VersionRef TemplateVersion,
        decimal? SellingPrice,
        bool CalculationComplete,
        DateTimeOffset UpdatedAt);

    private sealed record VersionRef(Guid Id, int VersionNumber);

    private sealed record ChecklistBody(Guid Id, string Text, bool IsCompleted, int SortOrder);

    private sealed record FolderBody(Guid Id, string? Name, string Path);

    private sealed record CostItemBody(Guid Id, string Category, decimal Amount);

    private sealed record CostListBody(CostItemBody[] Items, decimal TotalCost);

    private sealed record DashboardBody(
        DashboardOrderRow[]? ActiveOrders,
        DashboardOrderRow[]? UrgentOrders,
        DashboardOrderRow[]? DueSoonOrders,
        DashboardOrderRow[]? OverdueOrders);

    private sealed record DashboardOrderRow(Guid Id, string Name);

    private sealed record OrderReportBody(OrderReportRow[] Items, int TotalItems, OrderSummary Summary);

    private sealed record OrderReportRow(Guid OrderId, decimal? SellingPrice, decimal? CostPrice, decimal? Profit);

    private sealed record OrderSummary(int TotalOrders, decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record ProjectReportBody(ProjectReportRow[] Items, ProjectSummary Summary);

    private sealed record ProjectReportRow(Guid ProjectId, decimal? SellingTotal, decimal? CostTotal, decimal? Profit);

    private sealed record ProjectSummary(int TotalProjects, decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record ClientReportBody(ClientReportRow[] Items, ClientSummary Summary);

    private sealed record ClientReportRow(Guid ClientId, decimal? SellingTotal, decimal? CostTotal, decimal? Profit);

    private sealed record ClientSummary(decimal? SellingTotal, decimal? CostTotal, decimal? ProfitTotal);

    private sealed record OrderTypeReportBody(OrderTypeReportRow[] Items, OrderTypeSummary Summary);

    private sealed record OrderTypeReportRow(Guid OrderTypeId, decimal? SellingTotal);

    private sealed record OrderTypeSummary(decimal? SellingTotal, decimal? ProfitTotal);

    private sealed record CostReportBody(CostReportRow[] Items, CostSummary Summary);

    private sealed record CostReportRow(string? Category, decimal Amount);

    private sealed record CostSummary(decimal TotalAmount);

    private sealed record RoleBody(Guid Id, string Name);

    private sealed record PermissionBody(Guid Id, string Code);
}
