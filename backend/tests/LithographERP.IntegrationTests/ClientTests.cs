using System.Net;
using System.Net.Http.Json;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Clients;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ClientTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Client_CreationAssignsSequentialBusinessIdsAndAudit()
    {
        var director = await DirectorClientAsync();
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/clients", new { name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/clients", new { name = "Acme", email = "not-an-email" }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");

        var first = await CreateClientAsync(director, new
        {
            name = "  Samsung Armenia  ",
            contactPerson = " Anna ",
            phone = " +374 91 123456 ",
            email = "anna@example.com",
            address = " Yerevan ",
            notes = " Prefers afternoon delivery ",
        });
        Assert.Equal("CL-000001", first.BusinessId);
        Assert.Equal("Samsung Armenia", first.Name);
        Assert.Equal("Anna", first.ContactPerson);
        Assert.Equal("+374 91 123456", first.Phone);
        Assert.Equal("anna@example.com", first.Email);
        Assert.Equal("Yerevan", first.Address);
        Assert.Equal("Prefers afternoon delivery", first.Notes);
        Assert.True(first.IsActive);
        Assert.Null(first.UpdatedAt);

        var duplicateName = await CreateClientAsync(director, new { name = "Samsung Armenia" });
        Assert.Equal("CL-000002", duplicateName.BusinessId);
        Assert.NotEqual(first.Id, duplicateName.Id);

        var listed = await director.GetFromJsonAsync<ClientPageBody>("/api/clients?sort=business_id");
        Assert.Equal(2, listed!.TotalItems);
        Assert.Contains(listed.Items, item => item.BusinessId == "CL-000001");

        var before = first.UpdatedAt;
        var detail = await director.GetFromJsonAsync<ClientDetailBody>($"/api/clients/{first.Id}");
        Assert.Equal("CL-000001", detail!.BusinessId);
        Assert.Equal(before, detail.UpdatedAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Clients.SingleAsync(client => client.Id == first.Id);
        Assert.NotNull(stored.CreatedBy);
        Assert.Equal("CL-000001", stored.BusinessId);
        Assert.Null(stored.UpdatedAt);
    }

    [Fact]
    public async Task Client_BusinessIdsStayUniqueUnderConcurrencyAndAllowGaps()
    {
        var director = await DirectorClientAsync();
        var clients = Enumerable.Range(0, 8).Select(_ => factory.CreateClient()).ToArray();
        try
        {
            await Task.WhenAll(clients.Select(client => AuthApi.LoginAsync(client, "director", AuthApi.Password)));
            var responses = await Task.WhenAll(clients.Select(client =>
                client.PostAsJsonAsync("/api/clients", new { name = "Concurrent Client" })));
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var created = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<ClientDetailBody>()));
            var businessIds = created.Select(client => client!.BusinessId).ToArray();
            Assert.Equal(8, businessIds.Distinct(StringComparer.Ordinal).Count());
            Assert.All(businessIds, businessId => Assert.Matches("^CL-\\d{6}$", businessId));
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        _ = await db.Database.SqlQueryRaw<long>("SELECT nextval('clients.client_business_id_seq') AS \"Value\"").ToListAsync();
        var afterGap = await CreateClientAsync(director, new { name = "After Gap" });
        Assert.NotEqual("CL-000009", afterGap.BusinessId);
        Assert.Matches("^CL-\\d{6}$", afterGap.BusinessId);

        var sample = await db.Clients.AsNoTracking().FirstAsync();
        db.Clients.Add(new Client
        {
            Id = Guid.NewGuid(),
            BusinessId = sample.BusinessId,
            Name = "Duplicate ID",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Client_CanBeSearchedUpdatedAndRemainsReadableWhenInactive()
    {
        var director = await DirectorClientAsync();
        var client = await CreateClientAsync(director, new
        {
            name = "ABC Construction",
            contactPerson = "Anna Martirosyan",
            phone = "(010) 123456",
            email = "anna.m@example.com",
            address = "1 Main Street",
            notes = "Call before delivery",
        });
        await CreateClientAsync(director, new { name = "Other Client" });

        Assert.Contains((await SearchAsync(director, client.BusinessId)).Items, item => item.Id == client.Id);
        Assert.Contains((await SearchAsync(director, "construction")).Items, item => item.Id == client.Id);
        Assert.Contains((await SearchAsync(director, "martirosyan")).Items, item => item.Id == client.Id);
        Assert.Contains((await SearchAsync(director, "123456")).Items, item => item.Id == client.Id);
        Assert.Contains((await SearchAsync(director, "anna.m@example.com")).Items, item => item.Id == client.Id);
        Assert.DoesNotContain((await SearchAsync(director, "does-not-match")).Items, item => item.Id == client.Id);

        var updated = await director.PatchAsJsonAsync($"/api/clients/{client.Id}", new
        {
            name = "ABC Construction LLC",
            contactPerson = "Karen",
            phone = "+374 10 000000",
            email = "office@example.com",
            address = "2 New Street",
            notes = "Updated note",
            businessId = "CL-999999",
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var edited = (await updated.Content.ReadFromJsonAsync<ClientDetailBody>())!;
        Assert.Equal(client.BusinessId, edited.BusinessId);
        Assert.Equal("ABC Construction LLC", edited.Name);
        Assert.Equal("Karen", edited.ContactPerson);
        Assert.Equal("2 New Street", edited.Address);
        Assert.Equal("Updated note", edited.Notes);
        Assert.NotNull(edited.UpdatedAt);

        var deactivated = await director.PostAsync($"/api/clients/{client.Id}/deactivate", null);
        var inactive = (await deactivated.Content.ReadFromJsonAsync<ClientDetailBody>())!;
        Assert.False(inactive.IsActive);
        Assert.Equal(client.BusinessId, inactive.BusinessId);
        var readable = await director.GetFromJsonAsync<ClientDetailBody>($"/api/clients/{client.Id}");
        Assert.False(readable!.IsActive);
        Assert.DoesNotContain((await director.GetFromJsonAsync<ClientPageBody>("/api/clients?is_active=true"))!.Items, item => item.Id == client.Id);

        var selector = await director.GetStringAsync("/api/clients?is_active=true&view=selector&search=Other");
        Assert.Contains("Other Client", selector);
        Assert.DoesNotContain("contactPerson", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("address", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notes", selector, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ABC Construction", selector);

        var reactivated = await director.PostAsync($"/api/clients/{client.Id}/activate", null);
        Assert.True((await reactivated.Content.ReadFromJsonAsync<ClientDetailBody>())!.IsActive);
        await AuthApi.AssertErrorAsync(
            await director.GetAsync($"/api/clients/{Guid.NewGuid()}"),
            HttpStatusCode.NotFound,
            "CLIENT_NOT_FOUND");
    }

    [Fact]
    public async Task Client_PermissionsRejectAnonymousAndInsufficientAccessAndThereIsNoDelete()
    {
        var director = await DirectorClientAsync();
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/clients"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.PostAsJsonAsync("/api/clients", new { name = "Nope" }), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");

        var viewRole = await CreateRoleAsync(director, "Client Viewer", PermissionCatalog.Clients.View);
        var createRole = await CreateRoleAsync(director, "Client Creator", PermissionCatalog.Clients.Create);
        var editRole = await CreateRoleAsync(director, "Client Editor", PermissionCatalog.Clients.Edit);
        var activateRole = await CreateRoleAsync(director, "Client Activator", PermissionCatalog.Clients.Activate);
        await CreateUserAsync(director, "viewer", viewRole);
        await CreateUserAsync(director, "creator", createRole);
        await CreateUserAsync(director, "editor", editRole);
        await CreateUserAsync(director, "activator", activateRole);
        var client = await CreateClientAsync(director, new { name = "Protected Client" });

        using var viewer = await LoginAsync("viewer");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/clients/{client.Id}")).StatusCode);
        await AuthApi.AssertErrorAsync(await viewer.PostAsJsonAsync("/api/clients", new { name = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PatchAsJsonAsync($"/api/clients/{client.Id}", new { name = "Nope" }), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await viewer.PostAsync($"/api/clients/{client.Id}/deactivate", null), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var creator = await LoginAsync("creator");
        Assert.Equal(HttpStatusCode.Created, (await creator.PostAsJsonAsync("/api/clients", new { name = "Created" })).StatusCode);
        await AuthApi.AssertErrorAsync(await creator.GetAsync("/api/clients"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var editor = await LoginAsync("editor");
        Assert.Equal(HttpStatusCode.OK, (await editor.PatchAsJsonAsync($"/api/clients/{client.Id}", new { name = "Edited" })).StatusCode);
        await AuthApi.AssertErrorAsync(await editor.PostAsync($"/api/clients/{client.Id}/deactivate", null), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        using var activator = await LoginAsync("activator");
        Assert.Equal(HttpStatusCode.OK, (await activator.PostAsync($"/api/clients/{client.Id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await director.DeleteAsync($"/api/clients/{client.Id}")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.Clients.SingleAsync(candidate => candidate.Id == client.Id);
        Assert.False(stored.IsActive);
        Assert.Equal("Edited", stored.Name);
        Assert.Equal(client.BusinessId, stored.BusinessId);
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

    private static async Task<ClientDetailBody> CreateClientAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/clients", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClientDetailBody>())!;
    }

    private static async Task<ClientPageBody> SearchAsync(HttpClient client, string search) =>
        (await client.GetFromJsonAsync<ClientPageBody>($"/api/clients?search={Uri.EscapeDataString(search)}"))!;

    private sealed record ClientDetailBody(
        Guid Id,
        string BusinessId,
        string Name,
        string? ContactPerson,
        string? Phone,
        string? Email,
        string? Address,
        string? Notes,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    private sealed record ClientListItemBody(
        Guid Id,
        string BusinessId,
        string Name,
        string? ContactPerson,
        string? Phone,
        string? Email,
        bool IsActive);

    private sealed record ClientPageBody(
        ClientListItemBody[] Items,
        int Page,
        int PageSize,
        int TotalItems,
        int TotalPages);
}
