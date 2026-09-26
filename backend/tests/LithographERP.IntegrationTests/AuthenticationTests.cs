using System.Net;
using System.Net.Http.Json;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

internal static class AuthApi
{
    public const string Password = "Director-pass-1";
    public const string OtherPassword = "Planner-pass-1";

    public static async Task SetupAsync(HttpClient client, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/setup", new { password });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<CurrentUserBody> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CurrentUserBody>())!;
    }

    public static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, body);
        Assert.Contains($"\"code\":\"{code}\"", body.Replace(" ", string.Empty));
    }
}

internal sealed record CurrentUserBody(Guid Id, string Username, string[] Roles, string[] Permissions, Guid? LinkedEmployee);

internal sealed record UserBody(Guid Id, string Username, bool IsActive, DateTimeOffset? LastLoginAt, RoleSummaryBody[] Roles);

internal sealed record RoleSummaryBody(Guid Id, string Name, bool IsSystem);

internal sealed record RoleBody(Guid Id, string Name, string? Description, bool IsSystem, PermissionBody[] Permissions);

internal sealed record PermissionBody(Guid Id, string Code, string Name, string? Description, string Module);

internal sealed record SetupStatusBody(bool RequiresSetup);

[Collection(DatabaseCollection.Name)]
public class AuthenticationTests(LithographApiFactory factory)
{
    [Fact]
    public async Task FreshDatabase_ReportsSetupRequired_AndContainsNoUsers()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<SetupStatusBody>("/api/auth/setup-status");
        Assert.NotNull(status);
        Assert.True(status.RequiresSetup);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal(0, await db.Users.CountAsync());
        Assert.Equal(PermissionCatalog.All.Count, await db.Permissions.CountAsync());
        Assert.Equal(1, await db.Roles.CountAsync(role => role.NormalizedName == DirectorRole.NormalizedName && role.IsSystem));
        Assert.Equal(PermissionCatalog.All.Count, await db.RolePermissions.CountAsync());
    }

    [Fact]
    public async Task Setup_IsIdempotentForCatalog_AndCannotRunTwice()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var user = await db.Users.Include(candidate => candidate.UserRoles).ThenInclude(assignment => assignment.Role).SingleAsync();
        Assert.Equal("director", user.Username);
        Assert.Equal("DIRECTOR", user.NormalizedUsername);
        Assert.Null(user.CreatedBy);
        Assert.NotEqual(AuthApi.Password, user.PasswordHash);
        Assert.DoesNotContain(AuthApi.Password, user.PasswordHash);
        Assert.Equal(DirectorRole.NormalizedName, user.UserRoles.Single().Role.NormalizedName);

        await scope.ServiceProvider.GetRequiredService<IAuthenticationBootstrap>().SynchronizeAsync();
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(PermissionCatalog.All.Count, await db.Permissions.CountAsync());

        var second = await client.PostAsJsonAsync("/api/auth/setup", new { password = "Another-pass-1" });
        await AuthApi.AssertErrorAsync(second, HttpStatusCode.Conflict, "SETUP_ALREADY_COMPLETED");
    }

    [Fact]
    public async Task Setup_ConcurrentRequests_CreateOneDirector()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();

        var first = client.PostAsJsonAsync("/api/auth/setup", new { password = AuthApi.Password });
        var second = client.PostAsJsonAsync("/api/auth/setup", new { password = AuthApi.Password });
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(response => response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, results.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Login_SucceedsForNormalizedUsername_AndRejectsInvalidCredentials()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);

        var current = await AuthApi.LoginAsync(client, "DIRECTOR", AuthApi.Password);
        Assert.Equal("director", current.Username);
        Assert.Contains("Director", current.Roles);
        Assert.Equal(PermissionCatalog.All.Count, current.Permissions.Length);
        Assert.Contains(PermissionCatalog.Users.View, current.Permissions);
        Assert.Null(current.LinkedEmployee);

        using var anonymous = factory.CreateClient();
        var wrongPassword = await anonymous.PostAsJsonAsync("/api/auth/login", new { username = "director", password = "wrong-password" });
        await AuthApi.AssertErrorAsync(wrongPassword, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");

        var unknown = await anonymous.PostAsJsonAsync("/api/auth/login", new { username = "nobody", password = AuthApi.Password });
        await AuthApi.AssertErrorAsync(unknown, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Session_AuthorizesMe_LogoutAndExpiryAndRevocationRejectIt()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        await AuthApi.LoginAsync(client, "director", AuthApi.Password);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            Assert.Equal(1, await db.Sessions.CountAsync(session => session.IsActive));
            var session = await db.Sessions.SingleAsync();
            session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var expired = await client.GetAsync("/api/auth/me");
        await AuthApi.AssertErrorAsync(expired, HttpStatusCode.Unauthorized, "SESSION_EXPIRED");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var session = await db.Sessions.SingleAsync();
            session.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            session.IsActive = false;
            await db.SaveChangesAsync();
        }

        var revoked = await client.GetAsync("/api/auth/me");
        await AuthApi.AssertErrorAsync(revoked, HttpStatusCode.Unauthorized, "SESSION_INVALID");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var session = await db.Sessions.SingleAsync();
            session.IsActive = true;
            await db.SaveChangesAsync();
        }

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var afterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);

        await using var check = factory.Services.CreateAsyncScope();
        var sessions = check.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.False(await sessions.Sessions.AnyAsync(session => session.IsActive));
    }

    [Fact]
    public async Task Authorization_Returns401And403_AndUnionsRolePermissions()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);

        using var anonymous = factory.CreateClient();
        var unauthenticated = await anonymous.GetAsync("/api/users");
        await AuthApi.AssertErrorAsync(unauthenticated, HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        var viewRole = await CreateRoleAsync(director, "Viewer", PermissionCatalog.Users.View);
        var roleRole = await CreateRoleAsync(director, "Role Reader", PermissionCatalog.Roles.View);
        var created = await director.PostAsJsonAsync("/api/users", new
        {
            username = "planner",
            password = AuthApi.OtherPassword,
            roleIds = new[] { viewRole, roleRole },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var planner = factory.CreateClient();
        var current = await AuthApi.LoginAsync(planner, "planner", AuthApi.OtherPassword);
        Assert.Contains(PermissionCatalog.Users.View, current.Permissions);
        Assert.Contains(PermissionCatalog.Roles.View, current.Permissions);
        Assert.DoesNotContain(PermissionCatalog.Users.Create, current.Permissions);

        var allowed = await planner.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var denied = await planner.PostAsJsonAsync("/api/users", new { username = "other", password = AuthApi.OtherPassword });
        await AuthApi.AssertErrorAsync(denied, HttpStatusCode.Forbidden, "PERMISSION_DENIED");
    }

    [Fact]
    public async Task UserAdministration_CoversCreateUniquenessEditActivationAndPasswordReset()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);

        var created = await director.PostAsJsonAsync("/api/users", new { username = "Aram", password = AuthApi.OtherPassword });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<UserBody>())!;
        Assert.Equal("Aram", user.Username);
        Assert.DoesNotContain("passwordHash", await created.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AuthApi.OtherPassword, await created.Content.ReadAsStringAsync());

        var duplicate = await director.PostAsJsonAsync("/api/users", new { username = "aram", password = AuthApi.OtherPassword });
        await AuthApi.AssertErrorAsync(duplicate, HttpStatusCode.Conflict, "USERNAME_ALREADY_EXISTS");

        var renamed = await director.PutAsJsonAsync($"/api/users/{user.Id}", new { username = "aram.k" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("aram.k", (await renamed.Content.ReadFromJsonAsync<UserBody>())!.Username);

        using var secondSession = factory.CreateClient();
        await AuthApi.LoginAsync(secondSession, "aram.k", AuthApi.OtherPassword);

        var reset = await director.PostAsJsonAsync($"/api/users/{user.Id}/reset-password", new { newPassword = "Reset-pass-1" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        var oldSession = await secondSession.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        using var relogin = factory.CreateClient();
        var oldPassword = await relogin.PostAsJsonAsync("/api/auth/login", new { username = "aram.k", password = AuthApi.OtherPassword });
        await AuthApi.AssertErrorAsync(oldPassword, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        await AuthApi.LoginAsync(relogin, "aram.k", "Reset-pass-1");

        var deactivated = await director.PostAsync($"/api/users/{user.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.False((await deactivated.Content.ReadFromJsonAsync<UserBody>())!.IsActive);
        var inactiveLogin = await relogin.PostAsJsonAsync("/api/auth/login", new { username = "aram.k", password = "Reset-pass-1" });
        await AuthApi.AssertErrorAsync(inactiveLogin, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        var revoked = await relogin.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);

        var activated = await director.PostAsync($"/api/users/{user.Id}/activate", null);
        Assert.True((await activated.Content.ReadFromJsonAsync<UserBody>())!.IsActive);
    }

    [Fact]
    public async Task Deactivation_RevokesTheInactiveUsersSession()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        var created = await director.PostAsJsonAsync("/api/users", new { username = "operator", password = AuthApi.OtherPassword });
        var user = (await created.Content.ReadFromJsonAsync<UserBody>())!;

        using var operatorClient = factory.CreateClient();
        await AuthApi.LoginAsync(operatorClient, "operator", AuthApi.OtherPassword);
        await director.PostAsync($"/api/users/{user.Id}/deactivate", null);

        var me = await operatorClient.GetAsync("/api/auth/me");
        await AuthApi.AssertErrorAsync(me, HttpStatusCode.Unauthorized, "SESSION_INVALID");
    }

    [Fact]
    public async Task Roles_CanBeCreatedAssignedAndChangeEffectivePermissions()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        await AuthApi.LoginAsync(director, "director", AuthApi.Password);

        var roleId = await CreateRoleAsync(director, "Operators", PermissionCatalog.Users.View);
        var created = await director.PostAsJsonAsync("/api/users", new
        {
            username = "operator",
            password = AuthApi.OtherPassword,
            roleIds = new[] { roleId },
        });
        var user = (await created.Content.ReadFromJsonAsync<UserBody>())!;
        Assert.Contains(user.Roles, role => role.Id == roleId);

        var removed = await director.DeleteAsync($"/api/users/{user.Id}/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Empty((await removed.Content.ReadFromJsonAsync<UserBody>())!.Roles);

        using var operatorClient = factory.CreateClient();
        var before = await AuthApi.LoginAsync(operatorClient, "operator", AuthApi.OtherPassword);
        Assert.Empty(before.Permissions);

        await director.PostAsync($"/api/users/{user.Id}/roles/{roleId}", null);
        var after = await operatorClient.GetFromJsonAsync<CurrentUserBody>("/api/auth/me");
        Assert.Contains(PermissionCatalog.Users.View, after!.Permissions);
    }

    [Fact]
    public async Task FinalDirector_CannotBeRemoved_UntilAnotherDirectorExists()
    {
        await factory.ResetAsync();
        using var director = factory.CreateClient();
        await AuthApi.SetupAsync(director);
        var current = await AuthApi.LoginAsync(director, "director", AuthApi.Password);
        var directorRoleId = (await director.GetFromJsonAsync<RoleBody[]>("/api/roles"))!
            .Single(role => role.IsSystem).Id;

        var deactivate = await director.PostAsync($"/api/users/{current.Id}/deactivate", null);
        await AuthApi.AssertErrorAsync(deactivate, HttpStatusCode.Conflict, "FINAL_DIRECTOR_REQUIRED");
        var removeRole = await director.DeleteAsync($"/api/users/{current.Id}/roles/{directorRoleId}");
        await AuthApi.AssertErrorAsync(removeRole, HttpStatusCode.Conflict, "FINAL_DIRECTOR_REQUIRED");

        var rename = await director.PutAsJsonAsync($"/api/roles/{directorRoleId}", new { name = "Owner", description = "nope" });
        await AuthApi.AssertErrorAsync(rename, HttpStatusCode.Conflict, "SYSTEM_ROLE_PROTECTED");
        var permissions = await director.PutAsJsonAsync($"/api/roles/{directorRoleId}/permissions", new { permissionIds = Array.Empty<Guid>() });
        await AuthApi.AssertErrorAsync(permissions, HttpStatusCode.Conflict, "SYSTEM_ROLE_PROTECTED");

        var second = await director.PostAsJsonAsync("/api/users", new
        {
            username = "director2",
            password = AuthApi.OtherPassword,
            roleIds = new[] { directorRoleId },
        });
        var secondUser = (await second.Content.ReadFromJsonAsync<UserBody>())!;

        var deactivated = await director.PostAsync($"/api/users/{secondUser.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        await director.PostAsync($"/api/users/{secondUser.Id}/activate", null);
        var removed = await director.DeleteAsync($"/api/users/{secondUser.Id}/roles/{directorRoleId}");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.DoesNotContain((await removed.Content.ReadFromJsonAsync<UserBody>())!.Roles, role => role.IsSystem);
    }

    [Fact]
    public async Task FinalDirector_ConcurrentDeactivation_LeavesOneActiveDirector()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        await AuthApi.SetupAsync(client);
        var first = await AuthApi.LoginAsync(client, "director", AuthApi.Password);
        var directorRoleId = (await client.GetFromJsonAsync<RoleBody[]>("/api/roles"))!.Single(role => role.IsSystem).Id;
        var created = await client.PostAsJsonAsync("/api/users", new
        {
            username = "director2",
            password = AuthApi.OtherPassword,
            roleIds = new[] { directorRoleId },
        });
        var second = (await created.Content.ReadFromJsonAsync<UserBody>())!;

        var results = await Task.WhenAll(
            client.PostAsync($"/api/users/{first.Id}/deactivate", null),
            client.PostAsync($"/api/users/{second.Id}/deactivate", null));

        Assert.Equal(1, results.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var activeDirectors = await db.Users.CountAsync(user =>
            user.IsActive && user.UserRoles.Any(assignment => assignment.Role.NormalizedName == DirectorRole.NormalizedName));
        Assert.Equal(1, activeDirectors);
    }

    [Fact]
    public async Task ChangePassword_KeepsCurrentSession_AndRejectsOldPassword()
    {
        await factory.ResetAsync();
        using var current = factory.CreateClient();
        using var other = factory.CreateClient();
        await AuthApi.SetupAsync(current);
        await AuthApi.LoginAsync(current, "director", AuthApi.Password);
        await AuthApi.LoginAsync(other, "director", AuthApi.Password);

        var changed = await current.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = AuthApi.Password,
            newPassword = "New-director-1",
        });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await current.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/auth/me")).StatusCode);

        using var relogin = factory.CreateClient();
        var oldPassword = await relogin.PostAsJsonAsync("/api/auth/login", new { username = "director", password = AuthApi.Password });
        await AuthApi.AssertErrorAsync(oldPassword, HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS");
        await AuthApi.LoginAsync(relogin, "director", "New-director-1");
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient client, string name, string permissionCode)
    {
        var created = await client.PostAsJsonAsync("/api/roles", new { name, description = name });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = (await created.Content.ReadFromJsonAsync<RoleBody>())!;
        var permissions = await client.GetFromJsonAsync<PermissionBody[]>("/api/permissions");
        var permissionId = permissions!.Single(permission => permission.Code == permissionCode).Id;
        var updated = await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds = new[] { permissionId } });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        return role.Id;
    }
}
