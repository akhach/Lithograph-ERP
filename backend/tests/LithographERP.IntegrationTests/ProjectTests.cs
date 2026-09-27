using System.Net;
using System.Net.Http.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ProjectTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Project_CreationRequiresAnActiveClientAndAssignsAYearlyBusinessId()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Samsung Armenia");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/projects", new { clientId = client.Id, name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/projects", new
            {
                clientId = client.Id,
                name = "Bad dates",
                startDate = "2026-06-02",
                deadline = "2026-06-01",
            }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/projects", new { clientId = Guid.NewGuid(), name = "Missing" }),
            HttpStatusCode.NotFound,
            "CLIENT_NOT_FOUND");

        var created = await CreateProjectAsync(director, client.Id, "  Mall Signage  ", "2026-05-01", "2026-06-01", " Entrance signs ");
        Assert.Equal("PRJ-2026-000001", created.BusinessId);
        Assert.Equal("Mall Signage", created.Name);
        Assert.Equal("Entrance signs", created.Description);
        Assert.Equal("draft", created.Status);
        Assert.Equal(client.Id, created.Client.Id);
        Assert.Null(created.UpdatedAt);
        Assert.Null(created.Team.Owner);

        var duplicateName = await CreateProjectAsync(director, client.Id, "Mall Signage");
        Assert.Equal("PRJ-2026-000002", duplicateName.BusinessId);
        Assert.NotEqual(created.Id, duplicateName.Id);

        var before = created.UpdatedAt;
        var detail = await director.GetFromJsonAsync<ProjectDetailBody>($"/api/projects/{created.Id}");
        Assert.Equal(before, detail!.UpdatedAt);
        Assert.Equal("PRJ-2026-000001", detail.BusinessId);

        await director.PostAsync($"/api/clients/{client.Id}/deactivate", null);
        var historical = await director.GetFromJsonAsync<ProjectDetailBody>($"/api/projects/{created.Id}");
        Assert.False(historical!.Client.IsActive);
        Assert.Equal("PRJ-2026-000001", historical.BusinessId);
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/projects", new { clientId = client.Id, name = "Should fail" }),
            HttpStatusCode.BadRequest,
            "CLIENT_INACTIVE");
        var kept = await director.PatchAsJsonAsync($"/api/projects/{created.Id}", new
        {
            clientId = client.Id,
            name = "Mall Signage Updated",
            businessId = "PRJ-2026-999999",
        });
        var edited = (await kept.Content.ReadFromJsonAsync<ProjectDetailBody>())!;
        Assert.Equal("PRJ-2026-000001", edited.BusinessId);
        Assert.Equal("Mall Signage Updated", edited.Name);
        Assert.NotNull(edited.UpdatedAt);

        var activeClient = await CreateClientAsync(director, "ABC LLC");
        var moved = await director.PatchAsJsonAsync($"/api/projects/{created.Id}", new
        {
            clientId = activeClient.Id,
            name = "Mall Signage Updated",
        });
        Assert.Equal(activeClient.Id, (await moved.Content.ReadFromJsonAsync<ProjectDetailBody>())!.Client.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Projects.SingleAsync(project => project.Id == created.Id);
        Assert.NotNull(stored.CreatedBy);
        Assert.Equal(stored.CreatedBy, stored.UpdatedBy);
    }

    [Fact]
    public async Task Project_BusinessIdsResetEachYearStayUniqueAndAllowGaps()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Yearly Client");
        var clients = Enumerable.Range(0, 8).Select(_ => factory.CreateClient()).ToArray();
        try
        {
            await Task.WhenAll(clients.Select(http => AuthApi.LoginAsync(http, "director", AuthApi.Password)));
            var responses = await Task.WhenAll(clients.Select(http =>
                http.PostAsJsonAsync("/api/projects", new { clientId = client.Id, name = "Concurrent" })));
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var created = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<ProjectDetailBody>()));
            var numbers = created.Select(project => int.Parse(project!.BusinessId[^6..])).Order().ToArray();
            Assert.Equal(Enumerable.Range(1, 8), numbers);
            Assert.All(created, project => Assert.StartsWith("PRJ-2026-", project!.BusinessId, StringComparison.Ordinal));
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
        _ = await db.Database.SqlQueryRaw<long>("SELECT projects.next_project_business_id(2026) AS \"Value\"").ToListAsync();
        var afterGap = await CreateProjectAsync(director, client.Id, "After Gap");
        Assert.Equal("PRJ-2026-000010", afterGap.BusinessId);

        factory.SetUtcNow(new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero));
        var endOfYear = await CreateProjectAsync(director, client.Id, "End of 2026");
        Assert.StartsWith("PRJ-2026-", endOfYear.BusinessId, StringComparison.Ordinal);

        factory.SetUtcNow(new DateTimeOffset(2027, 1, 1, 0, 30, 0, TimeSpan.Zero));
        var nextYear = await CreateProjectAsync(director, client.Id, "Start of 2027");
        var nextYearAgain = await CreateProjectAsync(director, client.Id, "Second of 2027");
        Assert.Equal("PRJ-2027-000001", nextYear.BusinessId);
        Assert.Equal("PRJ-2027-000002", nextYearAgain.BusinessId);
    }

    [Fact]
    public async Task Project_ListSupportsSearchFiltersAndOpenSelector()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "ABC Construction");
        var other = await CreateClientAsync(director, "Other Client");
        var owner = await CreateEmployeeAsync(director, "Owner Person");
        var assignee = await CreateEmployeeAsync(director, "Assignee Person");
        var project = await CreateProjectAsync(director, client.Id, "Exhibition 2027", "2026-04-01", "2026-08-15");
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = owner.Id });
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = assignee.Id });
        var closed = await CreateProjectAsync(director, other.Id, "Closed Work");
        await director.PostAsJsonAsync($"/api/projects/{closed.Id}/status", new { status = "completed" });

        Assert.Contains((await SearchAsync(director, project.BusinessId)).Items, item => item.Id == project.Id);
        Assert.Contains((await SearchAsync(director, "exhibition")).Items, item => item.Id == project.Id);
        Assert.Contains((await SearchAsync(director, client.BusinessId)).Items, item => item.Id == project.Id);
        Assert.Contains((await SearchAsync(director, "construction")).Items, item => item.Id == project.Id);
        Assert.DoesNotContain((await SearchAsync(director, "does-not-match")).Items, item => item.Id == project.Id);

        var byOwner = await director.GetFromJsonAsync<ProjectPageBody>($"/api/projects?owner_employee_id={owner.Id}");
        Assert.Contains(byOwner!.Items, item => item.Id == project.Id && item.Owner!.Id == owner.Id);
        var byAssignee = await director.GetFromJsonAsync<ProjectPageBody>($"/api/projects?assignee_employee_id={assignee.Id}");
        Assert.Contains(byAssignee!.Items, item => item.Id == project.Id);
        var byClient = await director.GetFromJsonAsync<ProjectPageBody>($"/api/projects?client_id={client.Id}&status=draft");
        Assert.Contains(byClient!.Items, item => item.Id == project.Id);
        Assert.DoesNotContain(byClient.Items, item => item.Id == closed.Id);
        var byDeadline = await director.GetFromJsonAsync<ProjectPageBody>("/api/projects?deadline_from=2026-08-01&deadline_to=2026-08-31");
        Assert.Contains(byDeadline!.Items, item => item.Id == project.Id);
        Assert.DoesNotContain(byDeadline.Items, item => item.Id == closed.Id);

        var selector = await director.GetStringAsync("/api/projects?view=selector");
        Assert.Contains("Exhibition 2027", selector);
        Assert.DoesNotContain("Closed Work", selector);
        Assert.DoesNotContain("description", selector, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ABC Construction", selector);
    }

    [Fact]
    public async Task Project_StatusAndTeamFollowCardinalityAndHistoricalEmployeeRules()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, "Team Client");
        var project = await CreateProjectAsync(director, client.Id, "Store Opening");
        var owner = await CreateEmployeeAsync(director, "Aram");
        var replacement = await CreateEmployeeAsync(director, "Karen");
        var participant = await CreateEmployeeAsync(director, "Anna");
        var observer = await CreateEmployeeAsync(director, "Lilit");
        var inactive = await CreateEmployeeAsync(director, "Inactive Hire");
        await director.PostAsync($"/api/employees/{inactive.Id}/deactivate", null);
        await AuthApi.AssertErrorAsync(
            await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = inactive.Id }),
            HttpStatusCode.BadRequest,
            "EMPLOYEE_INACTIVE");
        await AuthApi.AssertErrorAsync(
            await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = Guid.NewGuid() }),
            HttpStatusCode.NotFound,
            "EMPLOYEE_NOT_FOUND");

        foreach (var status in new[] { "active", "on_hold", "completed", "cancelled", "draft" })
        {
            var changed = await director.PostAsJsonAsync($"/api/projects/{project.Id}/status", new { status });
            Assert.Equal(status, (await changed.Content.ReadFromJsonAsync<ProjectDetailBody>())!.Status);
        }

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/projects/{project.Id}/status", new { status = "archived" }),
            HttpStatusCode.BadRequest,
            "PROJECT_INVALID_STATUS");

        var assigned = await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = owner.Id });
        Assert.Equal(owner.Id, (await assigned.Content.ReadFromJsonAsync<ProjectDetailBody>())!.Team.Owner!.Id);
        var replaced = await director.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = replacement.Id });
        var afterOwner = (await replaced.Content.ReadFromJsonAsync<ProjectDetailBody>())!;
        Assert.Equal(replacement.Id, afterOwner.Team.Owner!.Id);
        Assert.Equal("PRJ-2026-000001", afterOwner.BusinessId);

        await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = owner.Id });
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = participant.Id });
        var clearedAssignee = await director.DeleteAsync($"/api/projects/{project.Id}/assignee");
        Assert.Null((await clearedAssignee.Content.ReadFromJsonAsync<ProjectDetailBody>())!.Team.Assignee);
        await director.PutAsJsonAsync($"/api/projects/{project.Id}/assignee", new { employeeId = owner.Id });

        await director.PostAsync($"/api/projects/{project.Id}/participants/{owner.Id}", null);
        await director.PostAsync($"/api/projects/{project.Id}/participants/{participant.Id}", null);
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/projects/{project.Id}/participants/{participant.Id}", null),
            HttpStatusCode.Conflict,
            "PROJECT_MEMBER_ALREADY_EXISTS");
        await director.PostAsync($"/api/projects/{project.Id}/observers/{observer.Id}", null);
        var withTeam = (await (await director.DeleteAsync($"/api/projects/{project.Id}/observers/{observer.Id}")).Content.ReadFromJsonAsync<ProjectDetailBody>())!;
        Assert.Empty(withTeam.Team.Observers);
        await director.PostAsync($"/api/projects/{project.Id}/observers/{observer.Id}", null);
        var team = (await director.GetFromJsonAsync<ProjectDetailBody>($"/api/projects/{project.Id}"))!;
        Assert.Equal(replacement.Id, team.Team.Owner!.Id);
        Assert.Equal(owner.Id, team.Team.Assignee!.Id);
        Assert.Contains(team.Team.Participants, person => person.Id == owner.Id);
        Assert.Contains(team.Team.Participants, person => person.Id == participant.Id);
        Assert.Contains(team.Team.Observers, person => person.Id == observer.Id);

        await director.PostAsync($"/api/employees/{owner.Id}/deactivate", null);
        var historical = (await director.GetFromJsonAsync<ProjectDetailBody>($"/api/projects/{project.Id}"))!;
        Assert.False(historical.Team.Assignee!.IsActive);
        Assert.Contains(historical.Team.Participants, person => person.Id == owner.Id && !person.IsActive);
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/projects/{project.Id}/observers/{owner.Id}", null),
            HttpStatusCode.BadRequest,
            "EMPLOYEE_INACTIVE");

        var clearedOwner = await director.DeleteAsync($"/api/projects/{project.Id}/owner");
        Assert.Null((await clearedOwner.Content.ReadFromJsonAsync<ProjectDetailBody>())!.Team.Owner);
        await AuthApi.AssertErrorAsync(
            await director.DeleteAsync($"/api/projects/{project.Id}/owner"),
            HttpStatusCode.NotFound,
            "PROJECT_MEMBER_NOT_FOUND");
        await director.DeleteAsync($"/api/projects/{project.Id}/participants/{participant.Id}");
    }

    [Fact]
    public async Task Project_PermissionsAndDatabaseConstraintsRejectInvalidAccessAndDuplicates()
    {
        var director = await DirectorClientAsync();
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/projects"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        var client = await CreateClientAsync(director, "Protected Client");
        var project = await CreateProjectAsync(director, client.Id, "Protected Project");
        var employee = await CreateEmployeeAsync(director, "Protected Employee");
        await CreateUserAsync(director, "viewer", await CreateRoleAsync(director, "Project Viewer", PermissionCatalog.Projects.View));
        await CreateUserAsync(director, "creator", await CreateRoleAsync(director, "Project Creator", PermissionCatalog.Projects.Create));
        await CreateUserAsync(director, "editor", await CreateRoleAsync(director, "Project Editor", PermissionCatalog.Projects.Edit));
        await CreateUserAsync(director, "team", await CreateRoleAsync(director, "Project Team", PermissionCatalog.Projects.ManageTeam));
        await CreateUserAsync(director, "status", await CreateRoleAsync(director, "Project Status", PermissionCatalog.Projects.ChangeStatus));

        using var viewer = await LoginAsync("viewer");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/projects/{project.Id}")).StatusCode);
        await AuthApi.AssertErrorAsync(await viewer.PostAsJsonAsync("/api/projects", new { clientId = client.Id, name = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PatchAsJsonAsync($"/api/projects/{project.Id}", new { clientId = client.Id, name = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PostAsJsonAsync($"/api/projects/{project.Id}/status", new { status = "active" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = employee.Id }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var creator = await LoginAsync("creator");
        Assert.Equal(HttpStatusCode.Created, (await creator.PostAsJsonAsync("/api/projects", new { clientId = client.Id, name = "Created" })).StatusCode);
        await AuthApi.AssertErrorAsync(await creator.GetAsync("/api/projects"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var editor = await LoginAsync("editor");
        Assert.Equal(HttpStatusCode.OK, (await editor.PatchAsJsonAsync($"/api/projects/{project.Id}", new { clientId = client.Id, name = "Edited" })).StatusCode);
        await AuthApi.AssertErrorAsync(await editor.PostAsJsonAsync($"/api/projects/{project.Id}/status", new { status = "active" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var team = await LoginAsync("team");
        Assert.Equal(HttpStatusCode.OK, (await team.PutAsJsonAsync($"/api/projects/{project.Id}/owner", new { employeeId = employee.Id })).StatusCode);
        await AuthApi.AssertErrorAsync(await team.PatchAsJsonAsync($"/api/projects/{project.Id}", new { clientId = client.Id, name = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var status = await LoginAsync("status");
        Assert.Equal(HttpStatusCode.OK, (await status.PostAsJsonAsync($"/api/projects/{project.Id}/status", new { status = "active" })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await director.DeleteAsync($"/api/projects/{project.Id}")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Projects.SingleAsync(candidate => candidate.Id == project.Id);
        Assert.Equal("Edited", stored.Name);
        Assert.Equal("active", stored.Status);
        Assert.Equal(project.BusinessId, stored.BusinessId);
        db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            EmployeeId = employee.Id,
            ProjectRole = ProjectRoles.Owner,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        var other = await CreateEmployeeAsync(director, "Second Owner");
        db.ChangeTracker.Clear();
        db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            EmployeeId = other.Id,
            ProjectRole = ProjectRoles.Owner,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        db.ChangeTracker.Clear();
        var clientRow = await db.Clients.SingleAsync(candidate => candidate.Id == client.Id);
        db.Clients.Remove(clientRow);
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

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, string permissionCode)
    {
        var created = await client.PostAsJsonAsync("/api/roles", new { name, description = name });
        var role = (await created.Content.ReadFromJsonAsync<RoleBody>())!;
        var permissions = await client.GetFromJsonAsync<PermissionBody[]>("/api/permissions");
        var permissionId = permissions!.Single(permission => permission.Code == permissionCode).Id;
        await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds = new[] { permissionId } });
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

    private static async Task<ProjectDetailBody> CreateProjectAsync(
        HttpClient client,
        Guid clientId,
        string name,
        string? startDate = null,
        string? deadline = null,
        string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { clientId, name, description, startDate, deadline });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectDetailBody>())!;
    }

    private static async Task<ProjectPageBody> SearchAsync(HttpClient client, string search) =>
        (await client.GetFromJsonAsync<ProjectPageBody>($"/api/projects?search={Uri.EscapeDataString(search)}"))!;

    private sealed record ClientDetailBody(Guid Id, string BusinessId, string Name, bool IsActive);

    private sealed record EmployeeBody(Guid Id, string FullName);

    private sealed record PersonBody(Guid Id, string FullName, string? Position, bool IsActive);

    private sealed record TeamBody(PersonBody? Owner, PersonBody? Assignee, PersonBody[] Participants, PersonBody[] Observers);

    private sealed record ProjectClientBody(Guid Id, string BusinessId, string Name, bool IsActive);

    private sealed record ProjectDetailBody(
        Guid Id,
        string BusinessId,
        ProjectClientBody Client,
        string Name,
        string? Description,
        string Status,
        DateOnly? StartDate,
        DateOnly? Deadline,
        TeamBody Team,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    private sealed record ProjectListItemBody(
        Guid Id,
        string BusinessId,
        string Name,
        ProjectClientBody Client,
        string Status,
        DateOnly? StartDate,
        DateOnly? Deadline,
        PersonBody? Owner,
        PersonBody? Assignee);

    private sealed record ProjectPageBody(ProjectListItemBody[] Items, int Page, int PageSize, int TotalItems, int TotalPages);
}
