using System.Net;
using System.Net.Http.Json;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class CalculatorTemplateTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Template_CreateStartsAtDraftVersionOne_AndNamesAreUniqueIgnoringCase()
    {
        var director = await DirectorClientAsync();
        var created = await CreateTemplateAsync(director, "  UV Printing Calculator  ", " Flatbed ");
        Assert.Equal("UV Printing Calculator", created.Name);
        Assert.Equal("Flatbed", created.Description);
        Assert.True(created.IsActive);
        Assert.Null(created.LatestPublishedVersion);
        Assert.Equal(1, created.DraftVersion);
        Assert.NotNull(created.DraftVersionId);

        var draft = await director.GetFromJsonAsync<VersionDetail>(
            $"/api/calculator-templates/{created.Id}/versions/{created.DraftVersionId}");
        Assert.Equal("draft", draft!.Status);
        Assert.Equal(1, draft.VersionNumber);
        Assert.Equal(1, draft.Definition.SchemaVersion);
        Assert.Empty(draft.Definition.Elements);
        Assert.Null(draft.PublishedAt);
        Assert.Null(draft.PublishedBy);

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/calculator-templates", new { name = "uv printing calculator" }),
            HttpStatusCode.Conflict,
            "CALCULATOR_TEMPLATE_NAME_ALREADY_EXISTS");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/calculator-templates", new { name = " " }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/calculator-templates/{created.Id}/versions", null),
            HttpStatusCode.Conflict,
            "TEMPLATE_DRAFT_ALREADY_EXISTS");

        var listed = await director.GetFromJsonAsync<TemplateListItem[]>("/api/calculator-templates");
        Assert.Contains(listed!, item => item.Id == created.Id && item.DraftVersion == 1 && item.LatestPublishedVersion is null);
    }

    [Fact]
    public async Task Template_PublicationRetiresPreviousVersionAndKeepsItImmutable()
    {
        var director = await DirectorClientAsync();
        var me = (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!;
        var template = await CreateTemplateAsync(director, "Laser Calculator");
        var versionId = template.DraftVersionId!.Value;

        var invalid = await director.PostAsync($"/api/calculator-templates/{template.Id}/versions/{versionId}/validate", null);
        var invalidBody = (await invalid.Content.ReadFromJsonAsync<ValidationBody>())!;
        Assert.False(invalidBody.IsValid);
        Assert.Contains(invalidBody.Errors, error => error.Code == "SELLING_PRICE_FIELD_MISSING");
        await AuthApi.AssertErrorAsync(
            await director.PostAsync($"/api/calculator-templates/{template.Id}/versions/{versionId}/publish", null),
            HttpStatusCode.BadRequest,
            "TEMPLATE_VALIDATION_FAILED");
        Assert.Equal("draft", (await GetVersionAsync(director, template.Id, versionId)).Status);

        var saved = await director.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{versionId}",
            new { definition = ValidDefinition("quantity * 2") });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var validated = (await (await director.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{versionId}/validate", null)).Content.ReadFromJsonAsync<ValidationBody>())!;
        Assert.True(validated.IsValid);
        Assert.Empty(validated.Errors);
        Assert.Equal("draft", (await GetVersionAsync(director, template.Id, versionId)).Status);

        var published = (await (await director.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{versionId}/publish", null)).Content.ReadFromJsonAsync<VersionDetail>())!;
        Assert.Equal("published", published.Status);
        Assert.Equal(me.Id, published.PublishedBy);
        Assert.NotNull(published.PublishedAt);
        Assert.Equal("quantity * 2", published.Definition.Elements.Single(element => element.Key == "selling_price").Formula);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync(
                $"/api/calculator-templates/{template.Id}/versions/{versionId}",
                new { definition = ValidDefinition("quantity * 9") }),
            HttpStatusCode.Conflict,
            "TEMPLATE_VERSION_IMMUTABLE");

        var second = (await (await director.PostAsync($"/api/calculator-templates/{template.Id}/versions", null))
            .Content.ReadFromJsonAsync<VersionDetail>())!;
        Assert.Equal(2, second.VersionNumber);
        Assert.Equal("draft", second.Status);
        Assert.Equal("quantity * 2", second.Definition.Elements.Single(element => element.Key == "selling_price").Formula);

        await director.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{second.Id}",
            new { definition = ValidDefinition("quantity * 4") });
        var publishedSecond = (await (await director.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{second.Id}/publish", null)).Content.ReadFromJsonAsync<VersionDetail>())!;
        Assert.Equal("published", publishedSecond.Status);
        Assert.Equal("quantity * 4", publishedSecond.Definition.Elements.Single(element => element.Key == "selling_price").Formula);

        var historical = await GetVersionAsync(director, template.Id, versionId);
        Assert.Equal("retired", historical.Status);
        Assert.Equal("quantity * 2", historical.Definition.Elements.Single(element => element.Key == "selling_price").Formula);
        Assert.Equal(me.Id, historical.PublishedBy);
        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync(
                $"/api/calculator-templates/{template.Id}/versions/{versionId}",
                new { definition = ValidDefinition("quantity * 9") }),
            HttpStatusCode.Conflict,
            "TEMPLATE_VERSION_IMMUTABLE");

        var detail = (await director.GetFromJsonAsync<TemplateDetail>($"/api/calculator-templates/{template.Id}"))!;
        Assert.Equal(2, detail.LatestPublishedVersion);
        Assert.Null(detail.DraftVersion);
        Assert.Equal(new[] { "retired", "published" }, detail.Versions.OrderBy(version => version.VersionNumber).Select(version => version.Status));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var stored = await db.CalculatorTemplateVersions.SingleAsync(version => version.Id == versionId);
        Assert.Equal("quantity * 2", stored.Definition.Elements.Single(element => element.Key == "selling_price").Formula);
        Assert.Equal(1, stored.Definition.SchemaVersion);
    }

    [Fact]
    public async Task Template_VersionCreationIsConcurrencySafe()
    {
        var director = await DirectorClientAsync();
        var template = await CreateTemplateAsync(director, "Concurrent Calculator");
        await director.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}",
            new { definition = ValidDefinition("1") });
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null)).StatusCode);

        var clients = Enumerable.Range(0, 8).Select(_ => factory.CreateClient()).ToArray();
        try
        {
            await Task.WhenAll(clients.Select(client => AuthApi.LoginAsync(client, "director", AuthApi.Password)));
            var responses = await Task.WhenAll(clients.Select(client =>
                client.PostAsync($"/api/calculator-templates/{template.Id}/versions", null)));
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            foreach (var conflict in responses.Where(response => response.StatusCode == HttpStatusCode.Conflict))
            {
                await AuthApi.AssertErrorAsync(conflict, HttpStatusCode.Conflict, "TEMPLATE_DRAFT_ALREADY_EXISTS");
            }

            var created = (await responses.Single(response => response.StatusCode == HttpStatusCode.Created)
                .Content.ReadFromJsonAsync<VersionDetail>())!;
            Assert.Equal(2, created.VersionNumber);
            Assert.Equal("draft", created.Status);
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
        var versions = await db.CalculatorTemplateVersions.Where(version => version.TemplateId == template.Id).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Single(versions, version => version.Status == "draft");
        Assert.Equal(new[] { 1, 2 }, versions.Select(version => version.VersionNumber).Order());
    }

    [Fact]
    public async Task Template_EndpointsEnforceAuthenticationAndSeparatePermissions()
    {
        var director = await DirectorClientAsync();
        var template = await CreateTemplateAsync(director, "Protected Calculator");
        await director.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}",
            new { definition = ValidDefinition("1") });

        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync("/api/calculator-templates"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/calculator-templates/{template.Id}"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PostAsJsonAsync("/api/calculator-templates", new { name = "Nope" }),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PostAsync($"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");

        await CreateUserAsync(director, "calcedit", await CreateRoleAsync(director, "Calc Edit", PermissionCatalog.Calculator.Edit));
        await CreateUserAsync(director, "calcview", await CreateRoleAsync(director, "Calc View", PermissionCatalog.Calculator.View));
        await CreateUserAsync(director, "calcmanage", await CreateRoleAsync(director, "Calc Manage", PermissionCatalog.Calculator.ManageTemplates));
        await CreateUserAsync(director, "calcpublish", await CreateRoleAsync(director, "Calc Publish", PermissionCatalog.Calculator.PublishTemplates));

        var editor = await LoginAsync("calcedit");
        await AuthApi.AssertErrorAsync(await editor.GetAsync("/api/calculator-templates"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await editor.PostAsJsonAsync("/api/calculator-templates", new { name = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var viewer = await LoginAsync("calcview");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/calculator-templates/{template.Id}")).StatusCode);
        await AuthApi.AssertErrorAsync(
            await viewer.PostAsJsonAsync("/api/calculator-templates", new { name = "Nope" }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var manager = await LoginAsync("calcmanage");
        Assert.Equal(HttpStatusCode.Created, (await manager.PostAsJsonAsync("/api/calculator-templates", new { name = "Manager Template" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/validate", null)).StatusCode);
        await AuthApi.AssertErrorAsync(
            await manager.PostAsync($"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var publisher = await LoginAsync("calcpublish");
        await AuthApi.AssertErrorAsync(
            await publisher.PatchAsJsonAsync(
                $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}",
                new { definition = ValidDefinition("2") }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        Assert.Equal(HttpStatusCode.OK, (await publisher.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null)).StatusCode);
    }

    [Fact]
    public async Task Template_DatabaseRejectsDuplicateDraftsVersionsAndPublishedMutation()
    {
        var director = await DirectorClientAsync();
        var template = await CreateTemplateAsync(director, "Constraint Calculator");
        await director.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}",
            new { definition = ValidDefinition("3") });
        Assert.Equal(HttpStatusCode.OK, (await director.PostAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null)).StatusCode);
        var draft = (await (await director.PostAsync($"/api/calculator-templates/{template.Id}/versions", null))
            .Content.ReadFromJsonAsync<VersionDetail>())!;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var columnType = await db.Database.SqlQueryRaw<string>(
            """
            SELECT udt_name AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'calculator' AND table_name = 'template_versions' AND column_name = 'definition'
            """).SingleAsync();
        Assert.Equal("jsonb", columnType);
        Assert.False(await db.Database.SqlQueryRaw<string>(
            """
            SELECT table_name AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'calculator' AND table_name IN ('order_calculators', 'cost_items')
            """).AnyAsync());
        var nullable = await db.Database.SqlQueryRaw<string>(
            """
            SELECT is_nullable AS "Value"
            FROM information_schema.columns
            WHERE table_schema = 'orders' AND table_name = 'order_types' AND column_name = 'calculator_template_id'
            """).SingleAsync();
        Assert.Equal("YES", nullable);

        db.CalculatorTemplateVersions.Add(new CalculatorTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 9,
            Status = "draft",
            Definition = CalculatorTemplateDefinition.Empty(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.CalculatorTemplateVersions.Add(new CalculatorTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 1,
            Status = "published",
            Definition = CalculatorTemplateDefinition.Empty(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var mutation = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(
            """
            UPDATE calculator.template_versions
            SET definition = {0}::jsonb
            WHERE id = {1}
            """,
            """{"schemaVersion":1,"elements":[]}""",
            template.DraftVersionId!.Value));
        Assert.Contains("template version immutable", mutation.ToString(), StringComparison.OrdinalIgnoreCase);

        var original = await db.CalculatorTemplateVersions.AsNoTracking().SingleAsync(version => version.Id == template.DraftVersionId);
        Assert.Equal("3", original.Definition.Elements.Single(element => element.Key == "selling_price").Formula);
        Assert.Equal("published", original.Status);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync(
                $"/api/calculator-templates/{template.Id}/versions/{draft.Id}",
                new { definition = new CalculatorTemplateDefinition { SchemaVersion = 2, Elements = [] } }),
            HttpStatusCode.Conflict,
            "TEMPLATE_SCHEMA_VERSION_UNSUPPORTED");
    }

    [Fact]
    public async Task OrderType_AssignsActiveTemplatesWithoutChangingHistoricalOrdersOrSellingPrice()
    {
        var director = await DirectorClientAsync();
        var template = await CreateTemplateAsync(director, "Shared Calculator");
        var other = await CreateTemplateAsync(director, "Other Calculator");
        await director.PostAsync($"/api/calculator-templates/{other.Id}/deactivate", null);

        var created = await director.PostAsJsonAsync("/api/order-types", new
        {
            name = "UV Printing",
            calculatorTemplateId = template.Id,
        });
        var uv = (await created.Content.ReadFromJsonAsync<OrderTypeBody>())!;
        Assert.Equal(template.Id, uv.CalculatorTemplateId);
        Assert.Equal("Shared Calculator", uv.CalculatorTemplateName);

        var shared = await director.PostAsJsonAsync("/api/order-types", new
        {
            name = "UV Board Printing",
            calculatorTemplateId = template.Id,
        });
        Assert.Equal(template.Id, (await shared.Content.ReadFromJsonAsync<OrderTypeBody>())!.CalculatorTemplateId);

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync("/api/order-types", new { name = "Inactive Assignment", calculatorTemplateId = other.Id }),
            HttpStatusCode.BadRequest,
            "CALCULATOR_TEMPLATE_INACTIVE");
        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/order-types/{uv.Id}", new
            {
                name = uv.Name,
                calculatorTemplateId = Guid.NewGuid(),
            }),
            HttpStatusCode.NotFound,
            "CALCULATOR_TEMPLATE_NOT_FOUND");

        await director.PostAsync($"/api/calculator-templates/{template.Id}/deactivate", null);
        var kept = (await director.GetFromJsonAsync<OrderTypeBody>($"/api/order-types/{uv.Id}"))!;
        Assert.Equal(template.Id, kept.CalculatorTemplateId);
        Assert.Equal("Shared Calculator", kept.CalculatorTemplateName);
        var renamed = await director.PatchAsJsonAsync($"/api/order-types/{uv.Id}", new
        {
            name = "UV Printing",
            description = "Still linked",
            calculatorTemplateId = template.Id,
        });
        var renamedBody = (await renamed.Content.ReadFromJsonAsync<OrderTypeBody>())!;
        Assert.Equal("Still linked", renamedBody.Description);
        Assert.Equal(template.Id, renamedBody.CalculatorTemplateId);

        var cleared = await director.PatchAsJsonAsync($"/api/order-types/{uv.Id}", new { name = "UV Printing", calculatorTemplateId = (Guid?)null });
        var clearedBody = (await cleared.Content.ReadFromJsonAsync<OrderTypeBody>())!;
        Assert.Null(clearedBody.CalculatorTemplateId);
        Assert.Null(clearedBody.CalculatorTemplateName);

        await director.PostAsync($"/api/calculator-templates/{template.Id}/activate", null);
        await director.PatchAsJsonAsync($"/api/order-types/{uv.Id}", new { name = "UV Printing", calculatorTemplateId = template.Id });

        var client = await director.PostAsJsonAsync("/api/clients", new { name = "Calculator Client" });
        var clientBody = (await client.Content.ReadFromJsonAsync<ClientBody>())!;
        var project = await director.PostAsJsonAsync("/api/projects", new { clientId = clientBody.Id, name = "Calculator Project" });
        var projectBody = (await project.Content.ReadFromJsonAsync<ProjectBody>())!;
        var order = await director.PostAsJsonAsync("/api/orders", new { projectId = projectBody.Id, orderTypeId = uv.Id, name = "Panel" });
        var orderBody = (await order.Content.ReadFromJsonAsync<OrderBody>())!;
        Assert.Equal(0m, orderBody.SellingPrice);
        Assert.False(orderBody.CalculatorConfigured);

        var options = await director.GetFromJsonAsync<TemplateOption[]>("/api/calculator-templates?view=selector");
        Assert.Contains(options!, option => option.Id == template.Id);
        Assert.DoesNotContain(options!, option => option.Id == other.Id);
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

    private static async Task<TemplateDetail> CreateTemplateAsync(HttpClient client, string name, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/calculator-templates", new { name, description });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TemplateDetail>())!;
    }

    private static async Task<VersionDetail> GetVersionAsync(HttpClient client, Guid templateId, Guid versionId) =>
        (await client.GetFromJsonAsync<VersionDetail>($"/api/calculator-templates/{templateId}/versions/{versionId}"))!;

    private static CalculatorTemplateDefinition ValidDefinition(string sellingFormula) => new()
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
                Formula = sellingFormula,
            },
        ],
    };

    private sealed record TemplateDetail(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        int? LatestPublishedVersion,
        int? DraftVersion,
        Guid? DraftVersionId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        VersionSummary[] Versions);

    private sealed record VersionSummary(
        Guid Id,
        int VersionNumber,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? PublishedAt,
        Guid? PublishedBy);

    private sealed record VersionDetail(
        Guid Id,
        Guid TemplateId,
        int VersionNumber,
        string Status,
        CalculatorTemplateDefinition Definition,
        DateTimeOffset CreatedAt,
        Guid? CreatedBy,
        DateTimeOffset? PublishedAt,
        Guid? PublishedBy);

    private sealed record TemplateListItem(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        int? LatestPublishedVersion,
        int? DraftVersion);

    private sealed record TemplateOption(Guid Id, string Name);

    private sealed record ValidationBody(bool IsValid, CalculatorValidationIssue[] Errors);

    private sealed record OrderTypeBody(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        Guid? CalculatorTemplateId,
        string? CalculatorTemplateName);

    private sealed record ClientBody(Guid Id, string BusinessId, string Name);

    private sealed record ProjectBody(Guid Id, string BusinessId, string Name);

    private sealed record OrderBody(Guid Id, decimal? SellingPrice, bool CalculatorConfigured);
}
