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
public class OrderCalculatorTests(LithographApiFactory factory)
{
    [Fact]
    public async Task Calculator_BindsTheCurrentPublishedVersion_AndStaysOnePerOrder()
    {
        var shop = await StartAsync("Bind", Price("quantity * 2"));
        var before = await GetOrderAsync(shop.Director, shop.OrderId);
        Assert.False(before.CalculatorConfigured);
        Assert.Equal(0m, before.SellingPrice);

        var opened = await OpenAsync(shop.Director, shop.OrderId);
        Assert.Equal(shop.OrderId, opened.OrderId);
        Assert.Equal(shop.VersionId, opened.TemplateVersion.Id);
        Assert.Equal(1, opened.TemplateVersion.VersionNumber);
        Assert.Equal("Bind", opened.Template.Name);
        Assert.False(opened.CalculationComplete);
        Assert.Null(opened.LastCalculatedAt);
        Assert.Equal(0m, opened.SellingPrice);
        Assert.Contains(opened.FieldErrors, error => error.FieldKey == "quantity" && error.Code == "MISSING_REQUIRED_VALUE");

        var again = await OpenAsync(shop.Director, shop.OrderId);
        Assert.Equal(opened.CalculatorId, again.CalculatorId);
        var after = await GetOrderAsync(shop.Director, shop.OrderId);
        Assert.True(after.CalculatorConfigured);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var row = await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == shop.OrderId);
        Assert.Equal(shop.VersionId, row.TemplateVersionId);
        Assert.Equal("{}", row.FieldValues);
        Assert.Equal(shop.UserId, row.CreatedBy);
        Assert.Equal(shop.UserId, row.UpdatedBy);
        Assert.Null(row.LastCalculatedAt);
        Assert.Equal(1, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == shop.OrderId));
    }

    [Fact]
    public async Task Calculator_IsNotCreatedWhenTheOrderTypeHasNoTemplateOrPublishedVersion()
    {
        var director = await DirectorClientAsync();
        var me = (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!;
        var draftOnly = await CreateTemplateAsync(director, "Draft Only");
        var inactive = await PublishAsync(director, "Inactive Template", Price("quantity * 2"));
        var plainType = await CreateOrderTypeAsync(director, "Plain Type", null);
        var draftType = await CreateOrderTypeAsync(director, "Draft Type", draftOnly.Id);
        var inactiveType = await CreateOrderTypeAsync(director, "Inactive Type", inactive.TemplateId);
        await director.PostAsync($"/api/calculator-templates/{inactive.TemplateId}/deactivate", null);
        var project = await CreateProjectAsync(director, (await CreateClientAsync(director, "Config Client")).Id, "Config Project");
        var plain = await CreateOrderAsync(director, project.Id, plainType.Id, "Plain");
        var draft = await CreateOrderAsync(director, project.Id, draftType.Id, "Draft");
        var retiredTemplate = await CreateOrderAsync(director, project.Id, inactiveType.Id, "Inactive");

        await AuthApi.AssertErrorAsync(await director.GetAsync($"/api/orders/{plain.Id}/calculator"), HttpStatusCode.Conflict, "CALCULATOR_NOT_CONFIGURED");
        await AuthApi.AssertErrorAsync(await director.GetAsync($"/api/orders/{draft.Id}/calculator"), HttpStatusCode.Conflict, "CALCULATOR_NO_PUBLISHED_VERSION");
        await AuthApi.AssertErrorAsync(await director.GetAsync($"/api/orders/{retiredTemplate.Id}/calculator"), HttpStatusCode.Conflict, "CALCULATOR_TEMPLATE_INACTIVE");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal(0, await db.OrderCalculators.CountAsync());
        Assert.NotEqual(Guid.Empty, me.Id);
    }

    [Fact]
    public async Task HistoricalOrdersStayOnTheirTemplateVersionAfterPublicationAndRetirement()
    {
        var director = await DirectorClientAsync();
        var template = await CreateTemplateAsync(director, "UV Printing Calculator");
        await SaveDraftAsync(director, template.Id, template.DraftVersionId!.Value, Price("quantity * 2"));
        var published = await PublishAsync(director, template.Id, template.DraftVersionId!.Value);
        Assert.Equal(1, published.VersionNumber);
        var orderType = await CreateOrderTypeAsync(director, "UV Printing", template.Id);
        var project = await CreateProjectAsync(director, (await CreateClientAsync(director, "History Client")).Id, "History Project");
        var orderA = await CreateOrderAsync(director, project.Id, orderType.Id, "Order A");

        var calculatorA = await OpenAsync(director, orderA.Id);
        Assert.Equal(published.Id, calculatorA.TemplateVersion.Id);
        Assert.Equal(1, calculatorA.TemplateVersion.VersionNumber);

        var second = await (await director.PostAsync($"/api/calculator-templates/{template.Id}/versions", null)).Content.ReadFromJsonAsync<VersionBody>();
        await SaveDraftAsync(director, template.Id, second!.Id, Price("quantity * 4"));
        var publishedSecond = await PublishAsync(director, template.Id, second.Id);
        Assert.Equal(2, publishedSecond.VersionNumber);

        var reopenedA = await OpenAsync(director, orderA.Id);
        Assert.Equal(published.Id, reopenedA.TemplateVersion.Id);
        Assert.Equal(1, reopenedA.TemplateVersion.VersionNumber);

        var orderB = await CreateOrderAsync(director, project.Id, orderType.Id, "Order B");
        var calculatorB = await OpenAsync(director, orderB.Id);
        Assert.Equal(publishedSecond.Id, calculatorB.TemplateVersion.Id);
        Assert.Equal(2, calculatorB.TemplateVersion.VersionNumber);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var savedA = await SaveOkAsync(director, orderA.Id, new { quantity = 5 }, reopenedA.UpdatedAt);
        Assert.Equal(10m, savedA.SellingPrice);
        Assert.Equal(10m, savedA.CalculatedValues["selling_price"].GetDecimal());
        Assert.True(savedA.CalculationComplete);
        Assert.NotNull(savedA.LastCalculatedAt);
        Assert.DoesNotContain("quantity * 2", await director.GetStringAsync($"/api/orders/{orderA.Id}/calculator"));

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var savedB = await SaveOkAsync(director, orderB.Id, new { quantity = 5 }, calculatorB.UpdatedAt);
        Assert.Equal(20m, savedB.SellingPrice);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal("retired", (await db.CalculatorTemplateVersions.SingleAsync(version => version.Id == published.Id)).Status);
        Assert.Equal(published.Id, (await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == orderA.Id)).TemplateVersionId);
        Assert.Equal(publishedSecond.Id, (await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == orderB.Id)).TemplateVersionId);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(3));
        var savedAgain = await SaveOkAsync(director, orderA.Id, new { quantity = 5 }, savedA.UpdatedAt);
        Assert.Equal(10m, savedAgain.SellingPrice);
        Assert.Equal(10m, (await GetOrderAsync(director, orderA.Id)).SellingPrice);
        Assert.Equal(20m, (await GetOrderAsync(director, orderB.Id)).SellingPrice);
    }

    [Fact]
    public async Task Save_UsesBackendFormulas_SynchronizesSellingPrice_AndRejectsManipulatedFields()
    {
        var shop = await StartAsync("Save", Full("quantity * material"));
        var opened = await OpenAsync(shop.Director, shop.OrderId);
        Assert.Contains(opened.Elements, element => element.Type == "section" && element.Label == "Pricing");
        Assert.Contains(opened.Elements, element => element.Type == "table");

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var partial = await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 10, note = "panel" }, opened.UpdatedAt);
        Assert.False(partial.CalculationComplete);
        Assert.Equal(0m, partial.SellingPrice);
        Assert.Null(partial.LastCalculatedAt);
        Assert.Equal(10m, partial.Values["quantity"].GetDecimal());
        Assert.Equal("panel", partial.Values["note"].GetString());
        Assert.Contains(partial.FieldErrors, error => error.FieldKey == "material");
        Assert.Equal(0m, (await GetOrderAsync(shop.Director, shop.OrderId)).SellingPrice);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var manipulated = await shop.Director.PatchAsJsonAsync(
            $"/api/orders/{shop.OrderId}/calculator",
            new
            {
                fieldValues = new { quantity = 10, material = "acrylic", note = "panel", rush = true, markup = 3, cost_rate = 4, copies = 2 },
                sellingPrice = 1,
                templateVersionId = Guid.NewGuid(),
                updatedAt = partial.UpdatedAt,
            });
        var manipulatedBody = await manipulated.Content.ReadAsStringAsync();
        Assert.True(manipulated.StatusCode == HttpStatusCode.OK, manipulatedBody);
        var saved = JsonSerializer.Deserialize<CalculatorBody>(manipulatedBody, JsonSerializerOptions.Web)!;
        Assert.True(saved.CalculationComplete);
        Assert.Equal(20m, saved.SellingPrice);
        Assert.Equal(20m, saved.CalculatedValues["selling_price"].GetDecimal());
        Assert.NotNull(saved.LastCalculatedAt);
        Assert.Equal(20m, (await GetOrderAsync(shop.Director, shop.OrderId)).SellingPrice);
        Assert.False(saved.Values.ContainsKey("selling_price"));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var stored = OrderCalculatorEvaluation.ReadStored((await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == shop.OrderId)).FieldValues);
            Assert.Equal(new StoredCalculatorValue.Number(10m), stored["quantity"]);
            Assert.Equal(new StoredCalculatorValue.Text("acrylic"), stored["material"]);
            Assert.Equal(new StoredCalculatorValue.Text("panel"), stored["note"]);
            Assert.Equal(new StoredCalculatorValue.Flag(true), stored["rush"]);
            Assert.DoesNotContain("selling_price", stored.Keys);
            Assert.Equal(shop.VersionId, (await db.OrderCalculators.SingleAsync(calculator => calculator.OrderId == shop.OrderId)).TemplateVersionId);
        }

        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = "abc", material = "acrylic" }, saved.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");
        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = 10, material = "plastic" }, saved.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");
        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = 5000, material = "acrylic" }, saved.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");
        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = 10, material = "acrylic", secret_discount = 0.01m }, saved.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");
        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = 10, material = "acrylic", selling_price = 1 }, saved.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");

        var unchanged = await GetOrderAsync(shop.Director, shop.OrderId);
        Assert.Equal(20m, unchanged.SellingPrice);
        var reloaded = await OpenAsync(shop.Director, shop.OrderId);
        Assert.Equal("acrylic", reloaded.Values["material"].GetString());
        Assert.Equal(10m, reloaded.Values["quantity"].GetDecimal());
    }

    [Fact]
    public async Task IncompleteSaveClearsAPreviousSellingPrice_AndNegativeResultsAreRejected()
    {
        var shop = await StartAsync("Incomplete", Price("quantity - 5"));
        var opened = await OpenAsync(shop.Director, shop.OrderId);
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var saved = await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 10 }, opened.UpdatedAt);
        Assert.Equal(5m, saved.SellingPrice);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var cleared = await SaveOkAsync(shop.Director, shop.OrderId, new { }, saved.UpdatedAt);
        Assert.False(cleared.CalculationComplete);
        Assert.Equal(0m, cleared.SellingPrice);
        Assert.Null(cleared.LastCalculatedAt);
        Assert.Empty(cleared.Values);
        Assert.Equal(0m, (await GetOrderAsync(shop.Director, shop.OrderId)).SellingPrice);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(3));
        var restored = await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 10 }, cleared.UpdatedAt);
        Assert.Equal(5m, restored.SellingPrice);
        await AuthApi.AssertErrorAsync(
            await PatchAsync(shop.Director, shop.OrderId, new { quantity = 1 }, restored.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_CALCULATION_ERROR");
        Assert.Equal(5m, (await GetOrderAsync(shop.Director, shop.OrderId)).SellingPrice);
        Assert.Equal(10m, (await OpenAsync(shop.Director, shop.OrderId)).Values["quantity"].GetDecimal());
    }

    [Fact]
    public async Task StaleSaveReturnsConflictAndDoesNotOverwrite()
    {
        var shop = await StartAsync("Conflict", Price("quantity * 2"));
        using var other = factory.CreateClient();
        await AuthApi.LoginAsync(other, "director", AuthApi.Password);
        var first = await OpenAsync(shop.Director, shop.OrderId);
        var second = await OpenAsync(other, shop.OrderId);
        Assert.Equal(first.UpdatedAt, second.UpdatedAt);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var saved = await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 4 }, first.UpdatedAt);
        Assert.Equal(8m, saved.SellingPrice);

        await AuthApi.AssertErrorAsync(
            await PatchAsync(other, shop.OrderId, new { quantity = 9 }, second.UpdatedAt),
            HttpStatusCode.Conflict,
            "CALCULATOR_CONCURRENCY_CONFLICT");
        var current = await OpenAsync(other, shop.OrderId);
        Assert.Equal(4m, current.Values["quantity"].GetDecimal());
        Assert.Equal(8m, current.SellingPrice);
        Assert.Equal(saved.UpdatedAt, current.UpdatedAt);
    }

    [Fact]
    public async Task OrderTypeChangeRequiresExplicitReset_AndPreservesCostPrice()
    {
        var director = await DirectorClientAsync();
        var first = await PublishAsync(director, "First Template", Price("quantity * 2"));
        var second = await PublishAsync(director, "Second Template", Price("quantity * 3"));
        var unpublished = await CreateTemplateAsync(director, "Unpublished Template");
        var inactive = await PublishAsync(director, "Inactive Target", Price("quantity * 9"));
        var firstType = await CreateOrderTypeAsync(director, "First Type", first.TemplateId);
        var secondType = await CreateOrderTypeAsync(director, "Second Type", second.TemplateId);
        var plainType = await CreateOrderTypeAsync(director, "Plain Type", null);
        var unpublishedType = await CreateOrderTypeAsync(director, "Unpublished Type", unpublished.Id);
        var inactiveType = await CreateOrderTypeAsync(director, "Inactive Target Type", inactive.TemplateId);
        await director.PostAsync($"/api/calculator-templates/{inactive.TemplateId}/deactivate", null);
        var project = await CreateProjectAsync(director, (await CreateClientAsync(director, "Reset Client")).Id, "Reset Project");
        var order = await CreateOrderAsync(director, project.Id, firstType.Id, "Reset Order");

        var moved = await director.PatchAsJsonAsync($"/api/orders/{order.Id}", OrderBody(project.Id, secondType.Id, "Reset Order"));
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(secondType.Id, (await moved.Content.ReadFromJsonAsync<OrderSnapshot>())!.OrderType.Id);
        await director.PatchAsJsonAsync($"/api/orders/{order.Id}", OrderBody(project.Id, firstType.Id, "Reset Order"));

        var opened = await OpenAsync(director, order.Id);
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var saved = await SaveOkAsync(director, order.Id, new { quantity = 6 }, opened.UpdatedAt);
        Assert.Equal(12m, saved.SellingPrice);
        await SetCostPriceAsync(order.Id, 12.50m);

        await AuthApi.AssertErrorAsync(
            await director.PatchAsJsonAsync($"/api/orders/{order.Id}", OrderBody(project.Id, secondType.Id, "Reset Order")),
            HttpStatusCode.Conflict,
            "ORDER_TYPE_CHANGE_REQUIRES_CALCULATOR_RESET");
        Assert.Equal(firstType.Id, (await GetOrderAsync(director, order.Id)).OrderType.Id);
        Assert.Equal(12m, (await GetOrderAsync(director, order.Id)).SellingPrice);

        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/orders/{order.Id}/change-order-type", new { orderTypeId = secondType.Id, resetCalculator = false, updatedAt = saved.UpdatedAt }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/orders/{order.Id}/change-order-type", new { orderTypeId = unpublishedType.Id, resetCalculator = true, updatedAt = saved.UpdatedAt }),
            HttpStatusCode.Conflict,
            "CALCULATOR_NO_PUBLISHED_VERSION");
        await AuthApi.AssertErrorAsync(
            await director.PostAsJsonAsync($"/api/orders/{order.Id}/change-order-type", new { orderTypeId = inactiveType.Id, resetCalculator = true, updatedAt = saved.UpdatedAt }),
            HttpStatusCode.Conflict,
            "CALCULATOR_TEMPLATE_INACTIVE");
        Assert.Equal(opened.TemplateVersion.Id, (await OpenAsync(director, order.Id)).TemplateVersion.Id);

        var changed = await director.PostAsJsonAsync(
            $"/api/orders/{order.Id}/change-order-type",
            new { orderTypeId = secondType.Id, resetCalculator = true, updatedAt = saved.UpdatedAt });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var changedBody = (await changed.Content.ReadFromJsonAsync<OrderSnapshot>())!;
        Assert.Equal(secondType.Id, changedBody.OrderType.Id);
        Assert.Equal(0m, changedBody.SellingPrice);
        Assert.Equal(12.50m, changedBody.CostPrice);
        var rebound = await OpenAsync(director, order.Id);
        Assert.Equal(second.VersionId, rebound.TemplateVersion.Id);
        Assert.Empty(rebound.Values);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var priced = await SaveOkAsync(director, order.Id, new { quantity = 4 }, rebound.UpdatedAt);
        Assert.Equal(12m, priced.SellingPrice);
        var cleared = await director.PostAsJsonAsync(
            $"/api/orders/{order.Id}/calculator/reset",
            new { updatedAt = priced.UpdatedAt });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var clearedBody = (await cleared.Content.ReadFromJsonAsync<CalculatorBody>())!;
        Assert.Equal(second.VersionId, clearedBody.TemplateVersion.Id);
        Assert.Empty(clearedBody.Values);
        Assert.Equal(0m, clearedBody.SellingPrice);
        var afterReset = await GetOrderAsync(director, order.Id);
        Assert.Equal(0m, afterReset.SellingPrice);
        Assert.Equal(12.50m, afterReset.CostPrice);

        var removed = await director.PostAsJsonAsync(
            $"/api/orders/{order.Id}/change-order-type",
            new { orderTypeId = plainType.Id, resetCalculator = true, updatedAt = clearedBody.UpdatedAt });
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var removedBody = (await removed.Content.ReadFromJsonAsync<OrderSnapshot>())!;
        Assert.Equal(plainType.Id, removedBody.OrderType.Id);
        Assert.Equal(0m, removedBody.SellingPrice);
        Assert.Equal(12.50m, removedBody.CostPrice);
        Assert.False(removedBody.CalculatorConfigured);
        await AuthApi.AssertErrorAsync(await director.GetAsync($"/api/orders/{order.Id}/calculator"), HttpStatusCode.Conflict, "CALCULATOR_NOT_CONFIGURED");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal(0, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == order.Id));
        Assert.Equal(12.50m, (await db.Orders.SingleAsync(candidate => candidate.Id == order.Id)).CostPrice);
    }

    [Fact]
    public async Task TwoOrdersUsingTheSameVersionDoNotShareValues()
    {
        var shop = await StartAsync("Shared", Price("quantity * 2"));
        var other = await CreateOrderAsync(shop.Director, shop.ProjectId, shop.OrderTypeId, "Other Order");
        var first = await OpenAsync(shop.Director, shop.OrderId);
        var second = await OpenAsync(shop.Director, other.Id);
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 3 }, first.UpdatedAt);
        var untouched = await OpenAsync(shop.Director, other.Id);
        Assert.Equal(second.CalculatorId, untouched.CalculatorId);
        Assert.Empty(untouched.Values);
        Assert.Equal(0m, untouched.SellingPrice);
        Assert.Equal(6m, (await GetOrderAsync(shop.Director, shop.OrderId)).SellingPrice);
    }

    [Fact]
    public async Task ConcurrentFirstOpenCreatesOneCalculator()
    {
        var shop = await StartAsync("Concurrent Open", Price("1"));
        var clients = Enumerable.Range(0, 8).Select(_ => factory.CreateClient()).ToArray();
        try
        {
            await Task.WhenAll(clients.Select(client => AuthApi.LoginAsync(client, "director", AuthApi.Password)));
            var responses = await Task.WhenAll(clients.Select(client => client.GetAsync($"/api/orders/{shop.OrderId}/calculator")));
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<CalculatorBody>()));
            Assert.Single(bodies.Select(body => body!.CalculatorId).Distinct());
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
        Assert.Equal(1, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == shop.OrderId));
    }

    [Fact]
    public async Task PermissionsFilterFinancialFieldsAndRejectAnonymousOrUnauthorizedCalls()
    {
        var shop = await StartAsync("Permissions", Full("quantity * material"));
        var opened = await OpenAsync(shop.Director, shop.OrderId);
        using var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/orders/{shop.OrderId}/calculator"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PatchAsJsonAsync($"/api/orders/{shop.OrderId}/calculator", new { fieldValues = new { quantity = 1 }, updatedAt = opened.UpdatedAt }),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PostAsJsonAsync($"/api/orders/{shop.OrderId}/calculator/reset", new { updatedAt = opened.UpdatedAt }),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PostAsJsonAsync($"/api/orders/{shop.OrderId}/change-order-type", new { orderTypeId = shop.OrderTypeId, resetCalculator = true }),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");

        await CreateUserAsync(shop.Director, "orderview", await CreateRoleAsync(shop.Director, "Order View", PermissionCatalog.Orders.View));
        await CreateUserAsync(shop.Director, "calcview", await CreateRoleAsync(shop.Director, "Calculator View", PermissionCatalog.Calculator.View));
        await CreateUserAsync(shop.Director, "calcedit", await CreateRoleAsync(shop.Director, "Calculator Edit", PermissionCatalog.Calculator.Edit));
        await CreateUserAsync(
            shop.Director,
            "calcgeneral",
            await CreateRoleAsync(shop.Director, "Calculator General", PermissionCatalog.Calculator.View, PermissionCatalog.Calculator.Edit));
        await CreateUserAsync(shop.Director, "orderedit", await CreateRoleAsync(shop.Director, "Order Edit", PermissionCatalog.Orders.Edit, PermissionCatalog.Orders.View));

        using var orderViewer = await LoginAsync("orderview");
        using var viewer = await LoginAsync("calcview");
        using var editor = await LoginAsync("calcedit");
        using var general = await LoginAsync("calcgeneral");
        using var orderEditor = await LoginAsync("orderedit");

        await AuthApi.AssertErrorAsync(await orderViewer.GetAsync($"/api/orders/{shop.OrderId}/calculator"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await viewer.PatchAsJsonAsync($"/api/orders/{shop.OrderId}/calculator", new { fieldValues = new { quantity = 1 }, updatedAt = opened.UpdatedAt }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await editor.GetAsync($"/api/orders/{shop.OrderId}/calculator"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        var hidden = await OpenAsync(general, shop.OrderId);
        Assert.Null(hidden.SellingPrice);
        Assert.Null(hidden.SellingPriceFieldKey);
        Assert.DoesNotContain(hidden.Elements, element => element.Key is "selling_price" or "markup" or "cost_rate");
        Assert.DoesNotContain(hidden.Elements, element => element.Type == "table" && element.Columns!.Any(column => column.Key == "cost_rate"));
        var raw = await general.GetStringAsync($"/api/orders/{shop.OrderId}/calculator");
        Assert.DoesNotContain("selling_price", raw);
        Assert.DoesNotContain("cost_rate", raw);
        Assert.DoesNotContain("markup", raw);

        await AuthApi.AssertErrorAsync(
            await PatchAsync(general, shop.OrderId, new { quantity = 2, material = "acrylic", cost_rate = 9 }, hidden.UpdatedAt),
            HttpStatusCode.BadRequest,
            "CALCULATOR_INVALID_INPUT");
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var saved = await SaveOkAsync(general, shop.OrderId, new { quantity = 2, material = "wood" }, hidden.UpdatedAt);
        Assert.Null(saved.SellingPrice);
        Assert.False(saved.CalculatedValues.ContainsKey("selling_price"));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        Assert.Equal(6m, (await db.Orders.SingleAsync(order => order.Id == shop.OrderId)).SellingPrice);

        await AuthApi.AssertErrorAsync(await editor.GetAsync($"/api/orders/{Guid.NewGuid()}/calculator"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(await shop.Director.GetAsync($"/api/orders/{Guid.NewGuid()}/calculator"), HttpStatusCode.NotFound, "ORDER_NOT_FOUND");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PatchAsJsonAsync($"/api/orders/{shop.OrderId}/calculator", new { fieldValues = new { quantity = 1 } }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await orderEditor.PostAsJsonAsync($"/api/orders/{shop.OrderId}/change-order-type", new { orderTypeId = shop.OrderTypeId, resetCalculator = true, updatedAt = saved.UpdatedAt }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
    }

    [Fact]
    public async Task DatabaseRejectsDuplicateCalculatorsAndBrokenReferences()
    {
        var shop = await StartAsync("Constraints", Price("quantity * 2"));
        await OpenAsync(shop.Director, shop.OrderId);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var existing = await db.OrderCalculators.AsNoTracking().SingleAsync(calculator => calculator.OrderId == shop.OrderId);
        var now = DateTimeOffset.UtcNow;
        db.OrderCalculators.Add(new OrderCalculator
        {
            Id = Guid.NewGuid(),
            OrderId = existing.OrderId,
            TemplateVersionId = existing.TemplateVersionId,
            FieldValues = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        });
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("order_calculators_order_id", duplicate.InnerException?.Message, StringComparison.Ordinal);
        db.ChangeTracker.Clear();

        db.OrderCalculators.Add(new OrderCalculator
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            TemplateVersionId = existing.TemplateVersionId,
            FieldValues = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.OrderCalculators.Add(new OrderCalculator
        {
            Id = Guid.NewGuid(),
            OrderId = existing.OrderId,
            TemplateVersionId = Guid.NewGuid(),
            FieldValues = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        var order = await db.Orders.SingleAsync(candidate => candidate.Id == shop.OrderId);
        db.Orders.Remove(order);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == shop.OrderId));
        Assert.Equal(0m, (await db.Orders.AsNoTracking().SingleAsync(candidate => candidate.Id == shop.OrderId)).CostPrice);
    }

    [Fact]
    public async Task SellingPriceUpdateRollsBackWithCalculatorValues()
    {
        var shop = await StartAsync("Atomic", Price("quantity * 2"));
        var opened = await OpenAsync(shop.Director, shop.OrderId);
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        await SaveOkAsync(shop.Director, shop.OrderId, new { quantity = 4 }, opened.UpdatedAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var order = await db.Orders.SingleAsync(candidate => candidate.Id == shop.OrderId);
        var calculator = await db.OrderCalculators.SingleAsync(candidate => candidate.OrderId == shop.OrderId);
        await using var transaction = await db.Database.BeginTransactionAsync();
        calculator.FieldValues = """{"quantity":9}""";
        order.SellingPrice = -1m;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await transaction.RollbackAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(8m, (await db.Orders.AsNoTracking().SingleAsync(candidate => candidate.Id == shop.OrderId)).SellingPrice);
        Assert.Contains("4", (await db.OrderCalculators.AsNoTracking().SingleAsync(candidate => candidate.OrderId == shop.OrderId)).FieldValues);
    }

    private async Task<Workshop> StartAsync(string name, CalculatorTemplateDefinition definition)
    {
        var director = await DirectorClientAsync();
        var me = (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!;
        var published = await PublishAsync(director, name, definition);
        var orderType = await CreateOrderTypeAsync(director, name + " Type", published.TemplateId);
        var client = await CreateClientAsync(director, name + " Client");
        var project = await CreateProjectAsync(director, client.Id, name + " Project");
        var order = await CreateOrderAsync(director, project.Id, orderType.Id, name + " Order");
        return new Workshop(director, me.Id, published.TemplateId, published.VersionId, orderType.Id, project.Id, order.Id);
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

    private async Task SetCostPriceAsync(Guid orderId, decimal costPrice)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var order = await db.Orders.SingleAsync(candidate => candidate.Id == orderId);
        order.CostPrice = costPrice;
        await db.SaveChangesAsync();
    }

    private static async Task<CalculatorBody> OpenAsync(HttpClient client, Guid orderId) =>
        (await client.GetFromJsonAsync<CalculatorBody>($"/api/orders/{orderId}/calculator"))!;

    private static async Task<CalculatorBody> SaveOkAsync(HttpClient client, Guid orderId, object fieldValues, DateTimeOffset updatedAt)
    {
        var response = await PatchAsync(client, orderId, fieldValues, updatedAt);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonSerializer.Deserialize<CalculatorBody>(body, JsonSerializerOptions.Web)!;
    }

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, Guid orderId, object fieldValues, DateTimeOffset updatedAt) =>
        client.PatchAsJsonAsync($"/api/orders/{orderId}/calculator", new { fieldValues, updatedAt });

    private static async Task<OrderSnapshot> GetOrderAsync(HttpClient client, Guid orderId) =>
        (await client.GetFromJsonAsync<OrderSnapshot>($"/api/orders/{orderId}"))!;

    private static async Task<TemplateBody> CreateTemplateAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/calculator-templates", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TemplateBody>())!;
    }

    private static async Task<PublishedVersion> PublishAsync(HttpClient client, string name, CalculatorTemplateDefinition definition)
    {
        var template = await CreateTemplateAsync(client, name);
        await SaveDraftAsync(client, template.Id, template.DraftVersionId!.Value, definition);
        var version = await PublishAsync(client, template.Id, template.DraftVersionId!.Value);
        return new PublishedVersion(template.Id, version.Id);
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

    private static async Task<OrderTypeBody> CreateOrderTypeAsync(HttpClient client, string name, Guid? templateId)
    {
        var response = await client.PostAsJsonAsync("/api/order-types", new { name, calculatorTemplateId = templateId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderTypeBody>())!;
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

    private static async Task<IdBody> CreateOrderAsync(HttpClient client, Guid projectId, Guid orderTypeId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new { projectId, orderTypeId, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
    }

    private static async Task CreateUserAsync(HttpClient client, string username, Guid roleId)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { username, password = AuthApi.OtherPassword, roleIds = new[] { roleId } });
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

    private static object OrderBody(Guid projectId, Guid orderTypeId, string name) =>
        new { projectId, orderTypeId, name, priority = "normal" };

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

    private static CalculatorTemplateDefinition Full(string formula) => new()
    {
        SchemaVersion = 1,
        SellingPriceFieldKey = "selling_price",
        Elements =
        [
            new CalculatorElementDefinition { Id = "dimensions", Type = "section", Label = "Dimensions" },
            new CalculatorElementDefinition
            {
                Id = "quantity",
                Type = "number_input",
                Key = "quantity",
                Label = "Quantity",
                Visibility = "general",
                Min = 0,
                Max = 1000,
            },
            new CalculatorElementDefinition
            {
                Id = "note",
                Type = "text_input",
                Key = "note",
                Label = "Note",
                Visibility = "general",
            },
            new CalculatorElementDefinition
            {
                Id = "material",
                Type = "dropdown",
                Key = "material",
                Label = "Material",
                Visibility = "general",
                Options =
                [
                    new CalculatorDropdownOption { Value = "acrylic", Label = "Acrylic", NumericValue = 2 },
                    new CalculatorDropdownOption { Value = "wood", Label = "Wood", NumericValue = 3 },
                ],
            },
            new CalculatorElementDefinition
            {
                Id = "rush",
                Type = "checkbox",
                Key = "rush",
                Label = "Rush",
                Visibility = "general",
            },
            new CalculatorElementDefinition
            {
                Id = "markup",
                Type = "number_input",
                Key = "markup",
                Label = "Markup",
                Visibility = "selling",
            },
            new CalculatorElementDefinition { Id = "pricing", Type = "section", Label = "Pricing" },
            new CalculatorElementDefinition { Id = "caption", Type = "label", Label = "Internal" },
            new CalculatorElementDefinition
            {
                Id = "extras",
                Type = "table",
                Label = "Extras",
                Columns =
                [
                    new CalculatorTableColumn { Key = "copies", Label = "Copies", Type = "number_input", Visibility = "general" },
                    new CalculatorTableColumn { Key = "cost_rate", Label = "Cost rate", Type = "number_input", Visibility = "cost" },
                ],
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

    private sealed record Workshop(
        HttpClient Director,
        Guid UserId,
        Guid TemplateId,
        Guid VersionId,
        Guid OrderTypeId,
        Guid ProjectId,
        Guid OrderId);

    private sealed record PublishedVersion(Guid TemplateId, Guid VersionId);

    private sealed record TemplateBody(Guid Id, Guid? DraftVersionId);

    private sealed record VersionBody(Guid Id, int VersionNumber, string Status);

    private sealed record OrderTypeBody(Guid Id, string Name);

    private sealed record IdBody(Guid Id);

    private sealed record OrderTypeRef(Guid Id, string Name, bool IsActive);

    private sealed record OrderSnapshot(
        Guid Id,
        decimal? SellingPrice,
        decimal? CostPrice,
        bool CalculatorConfigured,
        OrderTypeRef OrderType);

    private sealed record CalculatorBody(
        Guid OrderId,
        Guid CalculatorId,
        TemplateRef Template,
        VersionRef TemplateVersion,
        string? SellingPriceFieldKey,
        ElementBody[] Elements,
        Dictionary<string, JsonElement> Values,
        Dictionary<string, JsonElement> CalculatedValues,
        decimal? SellingPrice,
        bool CalculationComplete,
        FieldError[] FieldErrors,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? LastCalculatedAt);

    private sealed record TemplateRef(Guid Id, string Name);

    private sealed record VersionRef(Guid Id, int VersionNumber);

    private sealed record ElementBody(string Id, string Type, string? Key, string? Label, ColumnBody[]? Columns);

    private sealed record ColumnBody(string Key, string Visibility);

    private sealed record FieldError(string FieldKey, string Code, string Message);

    private sealed record RoleBody(Guid Id, string Name);

    private sealed record PermissionBody(Guid Id, string Code);
}
