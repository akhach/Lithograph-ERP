using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class DashboardTests(LithographApiFactory factory)
{
    private static readonly DateTimeOffset TodayAtNoon = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 26);

    [Fact]
    public async Task Dashboard_ClassifiesAttention_AndOrdersRecentWork()
    {
        using var director = await DirectorAsync();
        factory.SetUtcNow(TodayAtNoon);
        var client = await CreateClientAsync(director, "North Press");
        var project = await CreateProjectAsync(director, client.Id, "Catalog");
        var olderProject = await CreateProjectAsync(director, client.Id, "Older Project");
        var newerProject = await CreateProjectAsync(director, client.Id, "Newer Project");
        var owner = await CreateEmployeeAsync(director, "Mara Owner");
        Assert.Equal(HttpStatusCode.OK, (await director.PutAsJsonAsync($"/api/projects/{newerProject.Id}/owner", new { employeeId = owner.Id })).StatusCode);
        var orderType = await CreateOrderTypeAsync(director, "Brochure");

        var draft = await CreateOrderAsync(director, project.Id, orderType.Id, "Draft");
        var draftUrgent = await CreateOrderAsync(director, project.Id, orderType.Id, "Draft Urgent");
        var dueLower = await CreateOrderAsync(director, project.Id, orderType.Id, "Due Lower");
        var dueUpper = await CreateOrderAsync(director, project.Id, orderType.Id, "Due Upper");
        var dueOutside = await CreateOrderAsync(director, project.Id, orderType.Id, "Due Outside");
        var activeOverdue = await CreateOrderAsync(director, project.Id, orderType.Id, "Active Overdue");
        var activeUrgent = await CreateOrderAsync(director, project.Id, orderType.Id, "Active Urgent");
        var holdUrgent = await CreateOrderAsync(director, project.Id, orderType.Id, "Hold Urgent");
        var holdOverdue = await CreateOrderAsync(director, project.Id, orderType.Id, "Hold Overdue");
        var holdOpen = await CreateOrderAsync(director, project.Id, orderType.Id, "Hold Open");
        var completedUrgent = await CreateOrderAsync(director, project.Id, orderType.Id, "Completed Urgent");
        var completedPast = await CreateOrderAsync(director, project.Id, orderType.Id, "Completed Past");
        var completedToday = await CreateOrderAsync(director, project.Id, orderType.Id, "Completed Today");
        var completedPlain = await CreateOrderAsync(director, project.Id, orderType.Id, "Completed Plain");
        var cancelledUrgent = await CreateOrderAsync(director, project.Id, orderType.Id, "Cancelled Urgent");

        await SetOrderAsync(draft.Id, "draft", "normal", null, selling: 80m, createdAt: At(1));
        await SetOrderAsync(draftUrgent.Id, "draft", "urgent", null, createdAt: At(2));
        await SetOrderAsync(dueLower.Id, "active", "normal", Today, createdAt: At(3));
        await SetOrderAsync(dueUpper.Id, "active", "normal", Today.AddDays(7), createdAt: At(4));
        await SetOrderAsync(dueOutside.Id, "active", "normal", Today.AddDays(8), createdAt: At(5));
        await SetOrderAsync(activeOverdue.Id, "active", "normal", Today.AddDays(-2), createdAt: At(6));
        await SetOrderAsync(activeUrgent.Id, "active", "urgent", Today.AddDays(-1), createdAt: At(7));
        await SetOrderAsync(holdUrgent.Id, "on_hold", "urgent", Today.AddDays(-3), createdAt: At(8));
        await SetOrderAsync(holdOverdue.Id, "on_hold", "normal", Today.AddDays(-4), createdAt: At(9));
        await SetOrderAsync(holdOpen.Id, "on_hold", "normal", null, createdAt: At(10));
        await SetOrderAsync(completedUrgent.Id, "completed", "urgent", Today.AddDays(-1), createdAt: At(11));
        await SetOrderAsync(completedPast.Id, "completed", "normal", Today.AddDays(-4), createdAt: At(12));
        await SetOrderAsync(completedToday.Id, "completed", "normal", Today, createdAt: At(13));
        await SetOrderAsync(completedPlain.Id, "completed", "normal", null, createdAt: At(14));
        await SetOrderAsync(cancelledUrgent.Id, "cancelled", "urgent", Today.AddDays(-1), selling: 500m, cost: 20m, createdAt: At(15));
        await SetProjectCreatedAsync(olderProject.Id, At(1));
        await SetProjectCreatedAsync(project.Id, At(2));
        await SetProjectCreatedAsync(newerProject.Id, At(3));

        var body = await director.GetStringAsync("/api/dashboard");
        Assert.DoesNotContain("sellingPrice", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("costPrice", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"profit\"", body);
        var dashboard = JsonSerializer.Deserialize<DashboardBody>(body, JsonSerializerOptions.Web)!;
        Assert.NotNull(dashboard.Summary);
        Assert.NotNull(dashboard.ActiveOrders);
        Assert.NotNull(dashboard.UrgentOrders);
        Assert.NotNull(dashboard.DueSoonOrders);
        Assert.NotNull(dashboard.OverdueOrders);
        Assert.NotNull(dashboard.RecentOrders);
        Assert.NotNull(dashboard.RecentProjects);

        Assert.Equal(5, dashboard.Summary.ActiveOrders);
        Assert.Equal(3, dashboard.Summary.UrgentOrders);
        Assert.Equal(2, dashboard.Summary.DueSoonOrders);
        Assert.Equal(4, dashboard.Summary.OverdueOrders);

        Assert.Equal(
            [activeUrgent.Id, activeOverdue.Id, dueLower.Id, dueUpper.Id, dueOutside.Id],
            dashboard.ActiveOrders.Select(row => row.Id).ToArray());
        Assert.Equal([holdUrgent.Id, activeUrgent.Id, draftUrgent.Id], dashboard.UrgentOrders.Select(row => row.Id).ToArray());
        Assert.Equal([dueLower.Id, dueUpper.Id], dashboard.DueSoonOrders.Select(row => row.Id).ToArray());
        Assert.Equal(
            [holdOverdue.Id, holdUrgent.Id, activeOverdue.Id, activeUrgent.Id],
            dashboard.OverdueOrders.Select(row => row.Id).ToArray());
        Assert.DoesNotContain(dashboard.DueSoonOrders, row => row.Id == completedToday.Id || row.Id == cancelledUrgent.Id || row.Id == dueOutside.Id);
        Assert.DoesNotContain(dashboard.OverdueOrders, row => row.Id == completedPast.Id || row.Id == completedUrgent.Id || row.Id == cancelledUrgent.Id || row.Id == draft.Id || row.Id == holdOpen.Id);
        Assert.DoesNotContain(dashboard.UrgentOrders, row => row.Id == completedUrgent.Id || row.Id == cancelledUrgent.Id);
        Assert.Equal(10, dashboard.RecentOrders.Length);
        Assert.Equal(completedPlain.Id, dashboard.RecentOrders[0].Id);
        Assert.DoesNotContain(dashboard.RecentOrders, row => row.Id == cancelledUrgent.Id);
        Assert.Equal("Catalog", dashboard.ActiveOrders[0].ProjectName);
        Assert.Equal("North Press", dashboard.ActiveOrders[0].ClientName);

        Assert.Equal([newerProject.Id, project.Id, olderProject.Id], dashboard.RecentProjects.Select(row => row.Id).ToArray());
        Assert.Equal("Mara Owner", dashboard.RecentProjects[0].OwnerName);
        Assert.Equal(newerProject.Id, dashboard.RecentProjects[0].Id);
        Assert.Equal("North Press", dashboard.RecentProjects[0].ClientName);
    }

    [Fact]
    public async Task Dashboard_LimitsListsWithoutChangingCounts()
    {
        using var director = await DirectorAsync();
        factory.SetUtcNow(TodayAtNoon);
        var client = await CreateClientAsync(director, "Limit Client");
        var project = await CreateProjectAsync(director, client.Id, "Limit Project");
        var orderType = await CreateOrderTypeAsync(director, "Limit Type");
        for (var index = 0; index < 12; index++)
        {
            var order = await CreateOrderAsync(director, project.Id, orderType.Id, $"Active {index:00}");
            await SetOrderAsync(order.Id, "active", "normal", Today, createdAt: At(index + 1));
        }

        var dashboard = await GetAsync(director);
        Assert.NotNull(dashboard.Summary);
        Assert.NotNull(dashboard.ActiveOrders);
        Assert.Equal(12, dashboard.Summary.ActiveOrders);
        Assert.Equal(10, dashboard.ActiveOrders.Length);
        Assert.Equal("Active 11", dashboard.ActiveOrders[0].Name);
    }

    [Fact]
    public async Task Dashboard_OmitsSectionsWithoutPermission_AndRejectsAnonymous()
    {
        await factory.ResetAsync();
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/dashboard"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        var ordersOnly = await UserAsync(director, "orders", PermissionCatalog.Orders.View);
        var projectsOnly = await UserAsync(director, "projects", PermissionCatalog.Projects.View);
        var neither = await UserAsync(director, "clients", PermissionCatalog.Clients.View);
        var client = await CreateClientAsync(director, "Visible Client");
        var project = await CreateProjectAsync(director, client.Id, "Visible Project");
        var orderType = await CreateOrderTypeAsync(director, "Visible Type");
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, "Visible Order");
        await SetOrderAsync(order.Id, "active", "urgent", Today, selling: 40m, cost: 10m);

        var ordersBody = await ordersOnly.GetStringAsync("/api/dashboard");
        Assert.Contains("\"activeOrders\"", ordersBody);
        Assert.Contains("\"summary\"", ordersBody);
        Assert.DoesNotContain("\"recentProjects\"", ordersBody);
        Assert.DoesNotContain("sellingPrice", ordersBody, StringComparison.OrdinalIgnoreCase);
        var ordersDashboard = JsonSerializer.Deserialize<DashboardBody>(ordersBody, JsonSerializerOptions.Web)!;
        Assert.NotNull(ordersDashboard.Summary);
        Assert.NotNull(ordersDashboard.ActiveOrders);
        Assert.Equal(order.Id, ordersDashboard.ActiveOrders.Single().Id);
        Assert.Equal(1, ordersDashboard.Summary.UrgentOrders);

        var projectsBody = await projectsOnly.GetStringAsync("/api/dashboard");
        Assert.Contains("\"recentProjects\"", projectsBody);
        Assert.DoesNotContain("\"activeOrders\"", projectsBody);
        Assert.DoesNotContain("\"urgentOrders\"", projectsBody);
        Assert.DoesNotContain("\"dueSoonOrders\"", projectsBody);
        Assert.DoesNotContain("\"overdueOrders\"", projectsBody);
        Assert.DoesNotContain("\"recentOrders\"", projectsBody);
        Assert.DoesNotContain("\"summary\"", projectsBody);
        var projectsDashboard = JsonSerializer.Deserialize<DashboardBody>(projectsBody, JsonSerializerOptions.Web)!;
        Assert.NotNull(projectsDashboard.RecentProjects);
        Assert.Equal(project.Id, projectsDashboard.RecentProjects.Single().Id);

        var emptyAccess = await neither.GetStringAsync("/api/dashboard");
        Assert.Equal("{}", emptyAccess.Replace(" ", string.Empty));

        var empty = await GetAsync(await FreshDirectorAsync());
        Assert.NotNull(empty.Summary);
        Assert.NotNull(empty.ActiveOrders);
        Assert.NotNull(empty.UrgentOrders);
        Assert.NotNull(empty.DueSoonOrders);
        Assert.NotNull(empty.OverdueOrders);
        Assert.NotNull(empty.RecentOrders);
        Assert.NotNull(empty.RecentProjects);
        Assert.Equal(0, empty.Summary.ActiveOrders);
        Assert.Equal(0, empty.Summary.UrgentOrders);
        Assert.Equal(0, empty.Summary.DueSoonOrders);
        Assert.Equal(0, empty.Summary.OverdueOrders);
        Assert.Empty(empty.ActiveOrders);
        Assert.Empty(empty.UrgentOrders);
        Assert.Empty(empty.DueSoonOrders);
        Assert.Empty(empty.OverdueOrders);
        Assert.Empty(empty.RecentOrders);
        Assert.Empty(empty.RecentProjects);
    }

    private async Task<HttpClient> FreshDirectorAsync()
    {
        await factory.ResetAsync();
        var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        await AuthApi.LoginAsync(client, "director", AuthApi.Password);
        return client;
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

    private async Task SetOrderAsync(
        Guid orderId,
        string status,
        string priority,
        DateOnly? deadline,
        decimal selling = 0m,
        decimal cost = 0m,
        DateTimeOffset? createdAt = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var order = await db.Orders.SingleAsync(candidate => candidate.Id == orderId);
        order.Status = status;
        order.Priority = priority;
        order.Deadline = deadline;
        order.SellingPrice = selling;
        order.CostPrice = cost;
        if (createdAt is not null)
        {
            order.CreatedAt = createdAt.Value;
        }

        await db.SaveChangesAsync();
    }

    private async Task SetProjectCreatedAsync(Guid projectId, DateTimeOffset createdAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var project = await db.Projects.SingleAsync(candidate => candidate.Id == projectId);
        project.CreatedAt = createdAt;
        await db.SaveChangesAsync();
    }

    private static DateTimeOffset At(int hour) => new(2026, 9, 1, hour, 0, 0, TimeSpan.Zero);

    private static async Task<DashboardBody> GetAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/dashboard");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<DashboardBody>(body, JsonSerializerOptions.Web)!;
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

    private sealed record IdBody(Guid Id);

    private sealed record RoleBody(Guid Id, string Name, string? Description, bool IsSystem, PermissionBody[] Permissions);

    private sealed record PermissionBody(Guid Id, string Code, string Name, string? Description, string Module);

    private sealed record DashboardBody(
        DashboardSummaryBody? Summary,
        DashboardOrderBody[]? ActiveOrders,
        DashboardOrderBody[]? UrgentOrders,
        DashboardOrderBody[]? DueSoonOrders,
        DashboardOrderBody[]? OverdueOrders,
        DashboardOrderBody[]? RecentOrders,
        DashboardProjectBody[]? RecentProjects);

    private sealed record DashboardSummaryBody(int ActiveOrders, int UrgentOrders, int DueSoonOrders, int OverdueOrders);

    private sealed record DashboardOrderBody(
        Guid Id,
        string BusinessId,
        string Name,
        string ClientName,
        string ProjectName,
        string Status,
        string Priority,
        DateOnly? Deadline);

    private sealed record DashboardProjectBody(Guid Id, string BusinessId, string Name, string ClientName, string Status, string? OwnerName);
}
