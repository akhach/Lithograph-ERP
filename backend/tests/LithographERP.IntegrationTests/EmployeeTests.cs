using System.Net;
using System.Net.Http.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Employees;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class EmployeeTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Employee_CanBeCreatedListedUpdatedAndReadWithoutChangingAudit()
    {
        var director = await DirectorClientAsync();
        var missingName = await director.PostAsJsonAsync("/api/employees", new { fullName = " " });
        await AuthApi.AssertErrorAsync(missingName, HttpStatusCode.BadRequest, "VALIDATION_FAILED");

        var created = await director.PostAsJsonAsync("/api/employees", new
        {
            fullName = "  Aram Khachatryan  ",
            position = " Director ",
            phone = "+374 00 000000",
            email = "aram@example.com",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", createdBody, StringComparison.OrdinalIgnoreCase);
        var employee = (await created.Content.ReadFromJsonAsync<EmployeeDetailBody>())!;
        Assert.Equal("Aram Khachatryan", employee.FullName);
        Assert.Equal("Director", employee.Position);
        Assert.Null(employee.LinkedUser);
        Assert.True(employee.IsActive);
        Assert.Null(employee.UpdatedAt);

        var listed = await director.GetFromJsonAsync<EmployeePageBody>(
            "/api/employees?search=aram&is_active=true&has_user=false&sort=full_name");
        Assert.Contains(listed!.Items, item => item.Id == employee.Id);

        var unrelated = await director.GetFromJsonAsync<EmployeePageBody>("/api/employees?search=does-not-match");
        Assert.Empty(unrelated!.Items);

        var detail = await director.GetFromJsonAsync<EmployeeDetailBody>($"/api/employees/{employee.Id}");
        Assert.Equal(employee.CreatedAt, detail!.UpdatedAt ?? employee.CreatedAt);
        Assert.Null(detail.UpdatedAt);

        var updated = await director.PatchAsJsonAsync($"/api/employees/{employee.Id}", new
        {
            fullName = "Aram K",
            position = "Production Manager",
            phone = "",
            email = "aram.k@example.com",
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var edited = (await updated.Content.ReadFromJsonAsync<EmployeeDetailBody>())!;
        Assert.Equal("Aram K", edited.FullName);
        Assert.Null(edited.Phone);
        Assert.NotNull(edited.UpdatedAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Employees.SingleAsync(candidate => candidate.Id == employee.Id);
        Assert.NotNull(stored.CreatedBy);
        Assert.Equal(stored.CreatedBy, stored.UpdatedBy);
    }

    [Fact]
    public async Task Employee_ActivationIsIndependentFromTheLinkedUser()
    {
        var director = await DirectorClientAsync();
        var user = await CreateUserAsync(director, "operator");
        var employee = await CreateEmployeeAsync(director, "Operator", user.Id);

        var deactivated = await director.PostAsync($"/api/employees/{employee.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        var inactive = (await deactivated.Content.ReadFromJsonAsync<EmployeeDetailBody>())!;
        Assert.False(inactive.IsActive);
        Assert.True(inactive.LinkedUser!.IsActive);

        var readable = await director.GetFromJsonAsync<EmployeeDetailBody>($"/api/employees/{employee.Id}");
        Assert.False(readable!.IsActive);
        var hidden = await director.GetFromJsonAsync<EmployeePageBody>("/api/employees?is_active=true");
        Assert.DoesNotContain(hidden!.Items, item => item.Id == employee.Id);

        var userAfter = await director.GetFromJsonAsync<UserBody>($"/api/users/{user.Id}");
        Assert.True(userAfter!.IsActive);
        using var operatorClient = factory.CreateClient();
        await AuthApi.LoginAsync(operatorClient, "operator", AuthApi.OtherPassword);

        var reactivated = await director.PostAsync($"/api/employees/{employee.Id}/activate", null);
        Assert.True((await reactivated.Content.ReadFromJsonAsync<EmployeeDetailBody>())!.IsActive);
        var selectorJson = await director.GetStringAsync("/api/employees?is_active=true&view=selector");
        Assert.Contains("Operator", selectorJson);
        Assert.DoesNotContain("phone", selectorJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", selectorJson, StringComparison.OrdinalIgnoreCase);

        await director.PostAsync($"/api/users/{user.Id}/deactivate", null);
        var employeeAfterUser = await director.GetFromJsonAsync<EmployeeDetailBody>($"/api/employees/{employee.Id}");
        Assert.True(employeeAfterUser!.IsActive);
        Assert.False(employeeAfterUser.LinkedUser!.IsActive);
    }

    [Fact]
    public async Task Employee_UserLinkIsOptionalUniqueAndDoesNotChangeTheUser()
    {
        var director = await DirectorClientAsync();
        var firstUser = await CreateUserAsync(director, "designer");
        var secondUser = await CreateUserAsync(director, "installer");
        var alone = await CreateEmployeeAsync(director, "Installation Worker", null);
        Assert.Null(alone.LinkedUser);

        var linked = await director.PostAsJsonAsync($"/api/employees/{alone.Id}/link-user", new { userId = firstUser.Id });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal(firstUser.Id, (await linked.Content.ReadFromJsonAsync<EmployeeDetailBody>())!.LinkedUser!.Id);
        Assert.DoesNotContain("password", await linked.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var second = await CreateEmployeeAsync(director, "Second Person", null);
        var duplicate = await director.PostAsJsonAsync($"/api/employees/{second.Id}/link-user", new { userId = firstUser.Id });
        await AuthApi.AssertErrorAsync(duplicate, HttpStatusCode.Conflict, "USER_ALREADY_LINKED_TO_EMPLOYEE");

        var replace = await director.PostAsJsonAsync($"/api/employees/{alone.Id}/link-user", new { userId = secondUser.Id });
        await AuthApi.AssertErrorAsync(replace, HttpStatusCode.Conflict, "EMPLOYEE_ALREADY_LINKED_TO_USER");

        var missing = await director.PostAsJsonAsync($"/api/employees/{second.Id}/link-user", new { userId = Guid.NewGuid() });
        await AuthApi.AssertErrorAsync(missing, HttpStatusCode.NotFound, "USER_NOT_FOUND");

        var unlinked = await director.DeleteAsync($"/api/employees/{alone.Id}/user-link");
        Assert.Equal(HttpStatusCode.OK, unlinked.StatusCode);
        Assert.Null((await unlinked.Content.ReadFromJsonAsync<EmployeeDetailBody>())!.LinkedUser);
        var userStillThere = await director.GetFromJsonAsync<UserBody>($"/api/users/{firstUser.Id}");
        Assert.True(userStillThere!.IsActive);
        var again = await director.DeleteAsync($"/api/employees/{alone.Id}/user-link");
        await AuthApi.AssertErrorAsync(again, HttpStatusCode.NotFound, "EMPLOYEE_USER_LINK_NOT_FOUND");

        var createdLinked = await CreateEmployeeAsync(director, "Linked At Creation", secondUser.Id);
        Assert.Equal(secondUser.Id, createdLinked.LinkedUser!.Id);
        var takenAgain = await director.PostAsJsonAsync("/api/employees", new { fullName = "Other", userId = secondUser.Id });
        await AuthApi.AssertErrorAsync(takenAgain, HttpStatusCode.Conflict, "USER_ALREADY_LINKED_TO_EMPLOYEE");
    }

    [Fact]
    public async Task Employee_PermissionsRejectAnonymousAndInsufficientAccess()
    {
        var director = await DirectorClientAsync();
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/employees"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        var viewRole = await CreateRoleAsync(director, "Employee Viewer", PermissionCatalog.Employees.View);
        var createRole = await CreateRoleAsync(director, "Employee Creator", PermissionCatalog.Employees.Create);
        await director.PostAsJsonAsync("/api/users", new
        {
            username = "viewer",
            password = AuthApi.OtherPassword,
            roleIds = new[] { viewRole },
        });
        await director.PostAsJsonAsync("/api/users", new
        {
            username = "creator",
            password = AuthApi.OtherPassword,
            roleIds = new[] { createRole },
        });
        var employee = await CreateEmployeeAsync(director, "Protected", null);
        var user = await CreateUserAsync(director, "spare");

        using var viewer = factory.CreateClient();
        await AuthApi.LoginAsync(viewer, "viewer", AuthApi.OtherPassword);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/employees/{employee.Id}")).StatusCode);
        await AuthApi.AssertErrorAsync(await viewer.PostAsJsonAsync("/api/employees", new { fullName = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PatchAsJsonAsync($"/api/employees/{employee.Id}", new { fullName = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PostAsync($"/api/employees/{employee.Id}/deactivate", null), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PostAsJsonAsync($"/api/employees/{employee.Id}/link-user", new { userId = user.Id }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var creator = factory.CreateClient();
        await AuthApi.LoginAsync(creator, "creator", AuthApi.OtherPassword);
        var withoutLink = await creator.PostAsJsonAsync("/api/employees", new { fullName = "No Login" });
        Assert.Equal(HttpStatusCode.Created, withoutLink.StatusCode);
        var withLink = await creator.PostAsJsonAsync("/api/employees", new { fullName = "Should Fail", userId = user.Id });
        await AuthApi.AssertErrorAsync(withLink, HttpStatusCode.Forbidden, "PERMISSION_DENIED");
    }

    [Fact]
    public async Task Employee_DatabaseRejectsDuplicateUserLinksAndClearsTheLinkWhenTheUserIsRemoved()
    {
        var director = await DirectorClientAsync();
        var user = await CreateUserAsync(director, "temporary");
        var employee = await CreateEmployeeAsync(director, "Temporary Person", user.Id);
        var other = await CreateEmployeeAsync(director, "Another Person", null);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        db.Employees.Update(await db.Employees.SingleAsync(candidate => candidate.Id == other.Id));
        var tracked = await db.Employees.SingleAsync(candidate => candidate.Id == other.Id);
        tracked.UserId = user.Id;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM auth.sessions WHERE user_id = {user.Id};
            DELETE FROM auth.user_roles WHERE user_id = {user.Id};
            DELETE FROM auth.users WHERE id = {user.Id};
            """);
        var preserved = await db.Employees.AsNoTracking().SingleAsync(candidate => candidate.Id == employee.Id);
        Assert.Null(preserved.UserId);
        Assert.Equal("Temporary Person", preserved.FullName);
    }

    private async Task<HttpClient> DirectorClientAsync()
    {
        await factory.ResetAsync();
        var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        await AuthApi.LoginAsync(client, "director", AuthApi.Password);
        return client;
    }

    private static async Task<UserBody> CreateUserAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { username, password = AuthApi.OtherPassword });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserBody>())!;
    }

    private static async Task<EmployeeDetailBody> CreateEmployeeAsync(HttpClient client, string fullName, Guid? userId)
    {
        var response = await client.PostAsJsonAsync("/api/employees", new { fullName, position = fullName, userId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmployeeDetailBody>())!;
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

    private sealed record EmployeeDetailBody(
        Guid Id,
        string FullName,
        string? Position,
        string? Phone,
        string? Email,
        bool IsActive,
        LinkedUserBody? LinkedUser,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    private sealed record EmployeeListItemBody(
        Guid Id,
        string FullName,
        string? Position,
        string? Phone,
        string? Email,
        bool IsActive,
        LinkedUserBody? LinkedUser);

    private sealed record EmployeePageBody(EmployeeListItemBody[] Items, int Page, int PageSize, int TotalItems, int TotalPages);

    private sealed record LinkedUserBody(Guid Id, string Username, bool IsActive);
}
