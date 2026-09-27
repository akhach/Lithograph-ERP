using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class CostItemTests(LithographApiFactory factory)
{
    [Fact]
    public async Task CostItems_CreateEditDelete_RecalculateTheFullSum()
    {
        var shop = await StartAsync();
        var before = await LoadOrderAsync(shop.OrderId);
        Assert.Equal(0m, before.CostPrice);
        Assert.Empty(await ListAsync(shop.Director, shop.OrderId));

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var first = await CreateAsync(shop.Director, shop.OrderId, new
        {
            category = "  Material  ",
            supplier = " ABC Plastics ",
            expenseDate = "2026-09-20",
            description = " 3 mm acrylic ",
            amount = 100_000m,
        });
        Assert.Equal("Material", first.Category);
        Assert.Equal("ABC Plastics", first.Supplier);
        Assert.Equal(new DateOnly(2026, 9, 20), first.ExpenseDate);
        Assert.Equal("3 mm acrylic", first.Description);
        Assert.Equal(100_000m, first.Amount);
        Assert.Equal(10, first.SortOrder);
        Assert.Null(first.UpdatedAt);
        Assert.Equal(100_000m, (await ListResponseAsync(shop.Director, shop.OrderId)).TotalCost);
        var created = await LoadItemAsync(first.Id);
        Assert.Equal(shop.UserId, created.CreatedBy);
        Assert.Equal(LithographApiFactory.DefaultTestTime.AddMinutes(1), created.CreatedAt);
        Assert.Null(created.UpdatedBy);
        var afterCreate = await LoadOrderAsync(shop.OrderId);
        Assert.Equal(100_000m, afterCreate.CostPrice);
        Assert.Equal(shop.UserId, afterCreate.UpdatedBy);
        Assert.Equal(created.CreatedAt, afterCreate.UpdatedAt);
        await AssertInvariantAsync(shop.OrderId);

        var createdAt = created.CreatedAt;
        var listed = await ListResponseAsync(shop.Director, shop.OrderId);
        Assert.Equal(createdAt, (await LoadItemAsync(first.Id)).CreatedAt);
        Assert.Equal(afterCreate.UpdatedAt, (await LoadOrderAsync(shop.OrderId)).UpdatedAt);
        Assert.Equal(100_000m, listed.TotalCost);

        var second = await CreateAsync(shop.Director, shop.OrderId, new { category = "Outsource", amount = 50_000m });
        var third = await CreateAsync(shop.Director, shop.OrderId, new { category = "Transport", amount = 25_000m });
        Assert.Equal(175_000m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Equal(175_000m, (await ListResponseAsync(shop.Director, shop.OrderId)).TotalCost);
        Assert.Equal([first.Id, second.Id, third.Id], (await ListAsync(shop.Director, shop.OrderId)).Select(item => item.Id));

        var beforeSupplierEdit = await LoadOrderAsync(shop.OrderId);
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var renamed = await UpdateAsync(shop.Director, shop.OrderId, second.Id, new
        {
            category = "Outsource",
            supplier = "MetalCo",
            description = "Frame",
            amount = 50_000m,
        });
        Assert.Equal("MetalCo", renamed.Supplier);
        Assert.Equal("Frame", renamed.Description);
        Assert.NotNull(renamed.UpdatedAt);
        var renamedRow = await LoadItemAsync(second.Id);
        Assert.Equal(shop.UserId, renamedRow.UpdatedBy);
        Assert.Equal(175_000m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Equal(beforeSupplierEdit.UpdatedAt, (await LoadOrderAsync(shop.OrderId)).UpdatedAt);
        Assert.Equal(beforeSupplierEdit.UpdatedBy, (await LoadOrderAsync(shop.OrderId)).UpdatedBy);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(3));
        var repriced = await UpdateAsync(shop.Director, shop.OrderId, second.Id, new
        {
            category = "Outsource",
            supplier = "MetalCo",
            description = "Frame",
            amount = 70_000m,
        });
        Assert.Equal(70_000m, repriced.Amount);
        Assert.Equal(195_000m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Equal(shop.UserId, (await LoadOrderAsync(shop.OrderId)).UpdatedBy);
        Assert.Equal(LithographApiFactory.DefaultTestTime.AddMinutes(3), (await LoadOrderAsync(shop.OrderId)).UpdatedAt);
        await AssertInvariantAsync(shop.OrderId);

        var orderBeforeReorder = await LoadOrderAsync(shop.OrderId);
        var reordered = await ReorderAsync(shop.Director, shop.OrderId, [third.Id, first.Id, second.Id]);
        Assert.Equal([third.Id, first.Id, second.Id], reordered.Items.Select(item => item.Id));
        Assert.Equal(195_000m, reordered.TotalCost);
        Assert.Equal(orderBeforeReorder.UpdatedAt, (await LoadOrderAsync(shop.OrderId)).UpdatedAt);
        Assert.Equal(10, (await LoadItemAsync(third.Id)).SortOrder);

        Assert.Equal(HttpStatusCode.NoContent, (await shop.Director.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{third.Id}")).StatusCode);
        Assert.Equal(170_000m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Equal(HttpStatusCode.NoContent, (await shop.Director.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{first.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await shop.Director.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{second.Id}")).StatusCode);
        Assert.Equal(0m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Empty(await ListAsync(shop.Director, shop.OrderId));
        await AssertInvariantAsync(shop.OrderId);

        var precise = await CreateAsync(shop.Director, shop.OtherOrderId, new { category = "Material", amount = 10.10m });
        await CreateAsync(shop.Director, shop.OtherOrderId, new { category = "Transport", amount = 20.25m });
        Assert.Equal(30.35m, (await LoadOrderAsync(shop.OtherOrderId)).CostPrice);
        Assert.Equal(30.35m, (await ListResponseAsync(shop.Director, shop.OtherOrderId)).TotalCost);
        Assert.Equal(10.10m, (await LoadItemAsync(precise.Id)).Amount);

        var large = await CreateAsync(shop.Director, shop.ThirdOrderId, new { category = "Material", amount = 9_999_999_999_999_999.99m });
        Assert.Equal(9_999_999_999_999_999.99m, (await LoadItemAsync(large.Id)).Amount);
        Assert.Equal(9_999_999_999_999_999.99m, (await LoadOrderAsync(shop.ThirdOrderId)).CostPrice);
        await AssertInvariantAsync(shop.OrderId);
        await AssertInvariantAsync(shop.OtherOrderId);
        await AssertInvariantAsync(shop.ThirdOrderId);
    }

    [Fact]
    public async Task CostItems_RejectInvalidValues_AndKeepSupplierOptional()
    {
        var shop = await StartAsync();
        var optional = await CreateAsync(shop.Director, shop.OrderId, new { category = "Transport", amount = 5m });
        Assert.Null(optional.Supplier);
        Assert.Null(optional.ExpenseDate);
        Assert.Null(optional.Description);

        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = -1m }),
            HttpStatusCode.BadRequest,
            "COST_AMOUNT_INVALID");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = 0m }),
            HttpStatusCode.BadRequest,
            "COST_AMOUNT_INVALID");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = 1.239m }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = "nope" }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material" }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "   ", amount = 5m }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync(
                $"/api/orders/{shop.OrderId}/cost-items",
                new { category = new string('A', 151), amount = 5m }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync(
                $"/api/orders/{shop.OrderId}/cost-items",
                new { category = "Material", supplier = new string('B', 201), amount = 5m }),
            HttpStatusCode.BadRequest,
            "VALIDATION_FAILED");
        Assert.Equal(5m, (await LoadOrderAsync(shop.OrderId)).CostPrice);

        await AuthApi.AssertErrorAsync(
            await shop.Director.PatchAsJsonAsync(
                $"/api/orders/{shop.OtherOrderId}/cost-items/{optional.Id}",
                new { category = "Transport", amount = 9m }),
            HttpStatusCode.NotFound,
            "COST_ITEM_NOT_FOUND");
        await AuthApi.AssertErrorAsync(
            await shop.Director.DeleteAsync($"/api/orders/{shop.OtherOrderId}/cost-items/{optional.Id}"),
            HttpStatusCode.NotFound,
            "COST_ITEM_NOT_FOUND");
        await AuthApi.AssertErrorAsync(
            await shop.Director.GetAsync($"/api/orders/{Guid.NewGuid()}/cost-items"),
            HttpStatusCode.NotFound,
            "ORDER_NOT_FOUND");
        Assert.Equal(5m, (await LoadItemAsync(optional.Id)).Amount);

        var renamed = await shop.Director.PatchAsJsonAsync($"/api/orders/{shop.OrderId}", new
        {
            projectId = shop.ProjectId,
            orderTypeId = shop.OrderTypeId,
            name = "Renamed",
            priority = "normal",
            costPrice = 1m,
            sellingPrice = 9m,
        });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var stored = await LoadOrderAsync(shop.OrderId);
        Assert.Equal(5m, stored.CostPrice);
        Assert.Equal(0m, stored.SellingPrice);
    }

    [Fact]
    public async Task CostItems_DoNotDependOnCalculator_AndSurviveResetAndOrderTypeChange()
    {
        var director = await DirectorClientAsync();
        var me = (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!;
        var plainType = await CreateOrderTypeAsync(director, "Plain", null);
        var project = await CreateProjectAsync(director, (await CreateClientAsync(director, "Cost Client")).Id, "Cost Project");
        var plainOrder = await CreateOrderAsync(director, project.Id, plainType.Id, "No Calculator");
        var added = await CreateAsync(director, plainOrder.Id, new { category = "Installation", supplier = "Local crew", amount = 15m });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            Assert.Equal(0, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == plainOrder.Id));
        }

        Assert.Equal(15m, (await LoadOrderAsync(plainOrder.Id)).CostPrice);
        var otherPlain = await CreateOrderTypeAsync(director, "Other plain", null);
        var changedPlain = await director.PatchAsJsonAsync($"/api/orders/{plainOrder.Id}", new
        {
            projectId = project.Id,
            orderTypeId = otherPlain.Id,
            name = "No Calculator",
            priority = "normal",
        });
        Assert.Equal(HttpStatusCode.OK, changedPlain.StatusCode);
        Assert.Equal(added.Id, Assert.Single(await ListAsync(director, plainOrder.Id)).Id);
        Assert.Equal(15m, (await LoadOrderAsync(plainOrder.Id)).CostPrice);

        var published = await PublishAsync(director, "Cost Calculator", Price("quantity * 2"));
        var calculatedType = await CreateOrderTypeAsync(director, "Calculated", published.TemplateId);
        var order = await CreateOrderAsync(director, project.Id, calculatedType.Id, "Calculated Order");
        var opened = (await director.GetFromJsonAsync<CalculatorStamp>($"/api/orders/{order.Id}/calculator"))!;
        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(1));
        var saved = await SaveAsync(director, order.Id, new { quantity = 4 }, opened.UpdatedAt);
        Assert.Equal(8m, saved.SellingPrice);
        await CreateAsync(director, order.Id, new { category = "Material", amount = 12.50m });
        await CreateAsync(director, order.Id, new { category = "Transport", amount = 7.50m });
        Assert.Equal(20m, (await LoadOrderAsync(order.Id)).CostPrice);
        Assert.Equal(8m, (await LoadOrderAsync(order.Id)).SellingPrice);

        factory.SetUtcNow(LithographApiFactory.DefaultTestTime.AddMinutes(2));
        var reset = await ResetAsync(director, order.Id, saved.UpdatedAt);
        Assert.Equal(0m, reset.SellingPrice);
        Assert.Equal(20m, (await LoadOrderAsync(order.Id)).CostPrice);
        Assert.Equal(0m, (await LoadOrderAsync(order.Id)).SellingPrice);
        Assert.Equal(2, (await ListAsync(director, order.Id)).Length);

        var switched = await director.PostAsJsonAsync($"/api/orders/{order.Id}/change-order-type", new
        {
            orderTypeId = plainType.Id,
            resetCalculator = true,
            updatedAt = reset.UpdatedAt,
        });
        var switchedBody = await switched.Content.ReadAsStringAsync();
        Assert.True(switched.StatusCode == HttpStatusCode.OK, switchedBody);
        Assert.Equal(20m, (await LoadOrderAsync(order.Id)).CostPrice);
        Assert.Equal(0m, (await LoadOrderAsync(order.Id)).SellingPrice);
        Assert.Equal(2, (await ListAsync(director, order.Id)).Length);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            Assert.Equal(0, await db.OrderCalculators.CountAsync(calculator => calculator.OrderId == order.Id));
        }

        await AssertInvariantAsync(plainOrder.Id);
        await AssertInvariantAsync(order.Id);
        Assert.NotEqual(Guid.Empty, me.Id);
    }

    [Fact]
    public async Task CostItems_FollowCompletedAndCancelledRules()
    {
        var shop = await StartAsync();
        Assert.Equal(HttpStatusCode.OK, (await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/status", new { status = "completed" })).StatusCode);
        var late = await CreateAsync(shop.Director, shop.OrderId, new { category = "Transport", description = "Late invoice", amount = 18.40m });
        var corrected = await UpdateAsync(shop.Director, shop.OrderId, late.Id, new
        {
            category = "Transport",
            description = "Late invoice",
            amount = 21.40m,
        });
        Assert.Equal(21.40m, corrected.Amount);
        Assert.Equal(21.40m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        Assert.Equal(HttpStatusCode.NoContent, (await shop.Director.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{late.Id}")).StatusCode);
        Assert.Equal(0m, (await LoadOrderAsync(shop.OrderId)).CostPrice);

        var historical = await CreateAsync(shop.Director, shop.OtherOrderId, new { category = "Material", supplier = "Archive Co", amount = 44m });
        Assert.Equal(HttpStatusCode.OK, (await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OtherOrderId}/status", new { status = "cancelled" })).StatusCode);
        var history = await ListAsync(shop.Director, shop.OtherOrderId);
        Assert.Equal(historical.Id, Assert.Single(history).Id);
        Assert.Equal(44m, history[0].Amount);
        Assert.Equal("Archive Co", history[0].Supplier);
        Assert.Equal(44m, (await LoadOrderAsync(shop.OtherOrderId)).CostPrice);

        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OtherOrderId}/cost-items", new { category = "Material", amount = 1m }),
            HttpStatusCode.Conflict,
            "COST_MUTATION_NOT_ALLOWED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PatchAsJsonAsync(
                $"/api/orders/{shop.OtherOrderId}/cost-items/{historical.Id}",
                new { category = "Material", amount = 2m }),
            HttpStatusCode.Conflict,
            "COST_MUTATION_NOT_ALLOWED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.DeleteAsync($"/api/orders/{shop.OtherOrderId}/cost-items/{historical.Id}"),
            HttpStatusCode.Conflict,
            "COST_MUTATION_NOT_ALLOWED");
        await AuthApi.AssertErrorAsync(
            await shop.Director.PostAsJsonAsync($"/api/orders/{shop.OtherOrderId}/cost-items/reorder", new { ids = new[] { historical.Id } }),
            HttpStatusCode.Conflict,
            "COST_MUTATION_NOT_ALLOWED");
        Assert.Equal(44m, (await LoadItemAsync(historical.Id)).Amount);
        Assert.Equal(44m, (await LoadOrderAsync(shop.OtherOrderId)).CostPrice);
    }

    [Fact]
    public async Task CostItems_EnforceSeparatePermissions()
    {
        var shop = await StartAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var order = await db.Orders.SingleAsync(candidate => candidate.Id == shop.OrderId);
            order.SellingPrice = 200m;
            await db.SaveChangesAsync();
        }

        const string supplier = "Hidden Supplier 91";
        await CreateAsync(shop.Director, shop.OrderId, new { category = "Material", supplier, amount = 40m });
        var anonymous = factory.CreateClient();
        await AuthApi.AssertErrorAsync(await anonymous.GetAsync($"/api/orders/{shop.OrderId}/cost-items"), HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED");
        await AuthApi.AssertErrorAsync(
            await anonymous.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = 1m }),
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED");

        await CreateUserAsync(shop.Director, "orderview", await CreateRoleAsync(shop.Director, "Order View", PermissionCatalog.Orders.View));
        await CreateUserAsync(
            shop.Director,
            "costprice",
            await CreateRoleAsync(shop.Director, "Cost Price", PermissionCatalog.Orders.View, PermissionCatalog.Orders.ViewCostPrice));
        await CreateUserAsync(
            shop.Director,
            "selling",
            await CreateRoleAsync(shop.Director, "Selling", PermissionCatalog.Orders.View, PermissionCatalog.Orders.ViewSellingPrice));
        await CreateUserAsync(
            shop.Director,
            "bothmoney",
            await CreateRoleAsync(
                shop.Director,
                "Both Money",
                PermissionCatalog.Orders.View,
                PermissionCatalog.Orders.ViewSellingPrice,
                PermissionCatalog.Orders.ViewCostPrice));
        await CreateUserAsync(
            shop.Director,
            "costview",
            await CreateRoleAsync(shop.Director, "Cost View", PermissionCatalog.Orders.View, PermissionCatalog.Calculator.ViewCosts));
        await CreateUserAsync(
            shop.Director,
            "costseller",
            await CreateRoleAsync(
                shop.Director,
                "Cost Seller",
                PermissionCatalog.Orders.View,
                PermissionCatalog.Orders.ViewSellingPrice,
                PermissionCatalog.Calculator.ViewCosts));
        await CreateUserAsync(shop.Director, "costedit", await CreateRoleAsync(shop.Director, "Cost Edit", PermissionCatalog.Calculator.EditCosts));

        var viewer = await LoginAsync("orderview");
        await AuthApi.AssertErrorAsync(await viewer.GetAsync($"/api/orders/{shop.OrderId}/cost-items"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var viewerOrder = await viewer.GetStringAsync($"/api/orders/{shop.OrderId}");
        Assert.DoesNotContain(supplier, viewerOrder);
        using (var document = JsonDocument.Parse(viewerOrder))
        {
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("sellingPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        var aggregate = await LoginAsync("costprice");
        var aggregateDenied = await aggregate.GetAsync($"/api/orders/{shop.OrderId}/cost-items");
        await AuthApi.AssertErrorAsync(aggregateDenied, HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        Assert.DoesNotContain(supplier, await aggregateDenied.Content.ReadAsStringAsync());
        var aggregateOrder = await aggregate.GetStringAsync($"/api/orders/{shop.OrderId}");
        Assert.DoesNotContain(supplier, aggregateOrder);
        using (var document = JsonDocument.Parse(aggregateOrder))
        {
            Assert.Equal(40m, document.RootElement.GetProperty("costPrice").GetDecimal());
            Assert.False(document.RootElement.TryGetProperty("sellingPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        var seller = await LoginAsync("selling");
        var sellerOrder = await seller.GetStringAsync($"/api/orders/{shop.OrderId}");
        using (var document = JsonDocument.Parse(sellerOrder))
        {
            Assert.Equal(200m, document.RootElement.GetProperty("sellingPrice").GetDecimal());
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        var both = await LoginAsync("bothmoney");
        var bothOrder = await both.GetStringAsync($"/api/orders/{shop.OrderId}");
        Assert.DoesNotContain(supplier, bothOrder);
        using (var document = JsonDocument.Parse(bothOrder))
        {
            Assert.Equal(200m, document.RootElement.GetProperty("sellingPrice").GetDecimal());
            Assert.Equal(40m, document.RootElement.GetProperty("costPrice").GetDecimal());
            Assert.Equal(160m, document.RootElement.GetProperty("profit").GetDecimal());
        }

        await AuthApi.AssertErrorAsync(await both.GetAsync($"/api/orders/{shop.OrderId}/cost-items"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");

        var details = await LoginAsync("costview");
        var visible = await ListAsync(details, shop.OrderId);
        Assert.Equal(supplier, Assert.Single(visible).Supplier);
        Assert.Equal(40m, (await ListResponseAsync(details, shop.OrderId)).TotalCost);
        var detailOrder = await details.GetStringAsync($"/api/orders/{shop.OrderId}");
        using (var document = JsonDocument.Parse(detailOrder))
        {
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        await AuthApi.AssertErrorAsync(
            await details.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = 1m }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await details.PatchAsJsonAsync(
                $"/api/orders/{shop.OrderId}/cost-items/{visible[0].Id}",
                new { category = "Material", amount = 1m }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await details.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{visible[0].Id}"),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");
        await AuthApi.AssertErrorAsync(
            await details.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items/reorder", new { ids = new[] { visible[0].Id } }),
            HttpStatusCode.Forbidden,
            "PERMISSION_DENIED");

        var sellerWithDetails = await LoginAsync("costseller");
        var sellerWithDetailsOrder = await sellerWithDetails.GetStringAsync($"/api/orders/{shop.OrderId}");
        using (var document = JsonDocument.Parse(sellerWithDetailsOrder))
        {
            Assert.Equal(200m, document.RootElement.GetProperty("sellingPrice").GetDecimal());
            Assert.False(document.RootElement.TryGetProperty("costPrice", out _));
            Assert.False(document.RootElement.TryGetProperty("profit", out _));
        }

        Assert.Equal(supplier, Assert.Single(await ListAsync(sellerWithDetails, shop.OrderId)).Supplier);

        var editor = await LoginAsync("costedit");
        await AuthApi.AssertErrorAsync(await editor.GetAsync($"/api/orders/{shop.OrderId}/cost-items"), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var edited = await CreateAsync(editor, shop.OrderId, new { category = "Outsource", amount = 10m });
        Assert.Equal(10m, edited.Amount);
        Assert.Equal(50m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        await AssertInvariantAsync(shop.OrderId);
    }

    [Fact]
    public async Task CostItems_PreserveTheTotalUnderConcurrencyAndDatabaseConstraints()
    {
        var shop = await StartAsync();
        var second = factory.CreateClient();
        await AuthApi.LoginAsync(second, "director", AuthApi.Password);
        var adds = await Task.WhenAll(
            shop.Director.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Material", amount = 100m }),
            second.PostAsJsonAsync($"/api/orders/{shop.OrderId}/cost-items", new { category = "Transport", amount = 50m }));
        foreach (var response in adds)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        }

        Assert.Equal(2, (await ListAsync(shop.Director, shop.OrderId)).Length);
        Assert.Equal(150m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        await AssertInvariantAsync(shop.OrderId);

        var left = await CreateAsync(shop.Director, shop.OrderId, new { category = "Installation", amount = 100m });
        var right = await CreateAsync(shop.Director, shop.OrderId, new { category = "Finishing", amount = 40m });
        var mixed = await Task.WhenAll(
            shop.Director.PatchAsJsonAsync(
                $"/api/orders/{shop.OrderId}/cost-items/{left.Id}",
                new { category = "Installation", amount = 70m }),
            second.DeleteAsync($"/api/orders/{shop.OrderId}/cost-items/{right.Id}"));
        Assert.Equal(HttpStatusCode.OK, mixed[0].StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, mixed[1].StatusCode);
        Assert.Equal(70m + 150m, (await LoadOrderAsync(shop.OrderId)).CostPrice);
        await AssertInvariantAsync(shop.OrderId);

        var contested = await CreateAsync(shop.Director, shop.OtherOrderId, new { category = "Material", amount = 30m });
        await Task.WhenAll(
            shop.Director.PatchAsJsonAsync(
                $"/api/orders/{shop.OtherOrderId}/cost-items/{contested.Id}",
                new { category = "Material", amount = 80m }),
            second.DeleteAsync($"/api/orders/{shop.OtherOrderId}/cost-items/{contested.Id}"));
        await AssertInvariantAsync(shop.OtherOrderId);
        var remaining = await ListAsync(shop.Director, shop.OtherOrderId);
        Assert.True(remaining.Length is 0 or 1);
        if (remaining.Length == 1)
        {
            Assert.Equal(80m, remaining[0].Amount);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION calculator.reject_test_cost_sync()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                  IF NEW.cost_price = 15.15 THEN
                    RAISE EXCEPTION 'simulated cost sync failure';
                  END IF;
                  RETURN NEW;
                END;
                $$;
                DROP TRIGGER IF EXISTS reject_test_cost_sync ON orders.orders;
                CREATE TRIGGER reject_test_cost_sync
                BEFORE UPDATE OF cost_price ON orders.orders
                FOR EACH ROW
                EXECUTE FUNCTION calculator.reject_test_cost_sync();
                """);
        }

        try
        {
            var failed = await shop.Director.PostAsJsonAsync(
                $"/api/orders/{shop.ThirdOrderId}/cost-items",
                new { category = "Material", amount = 15.15m });
            var failedBody = await failed.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
            Assert.Contains("An unexpected error occurred.", failedBody);
            Assert.DoesNotContain("simulated", failedBody);
            Assert.DoesNotContain("cost_items", failedBody);
            Assert.Equal(0m, (await LoadOrderAsync(shop.ThirdOrderId)).CostPrice);
            Assert.Empty(await ListAsync(shop.Director, shop.ThirdOrderId));
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS reject_test_cost_sync ON orders.orders;
                DROP FUNCTION IF EXISTS calculator.reject_test_cost_sync();
                """);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var negative = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO calculator.cost_items (id, order_id, category, amount, sort_order, created_at)
                VALUES ({0}, {1}, 'Material', -1.00, 1, TIMESTAMPTZ '2026-09-27 12:00:00+00')
                """,
                Guid.NewGuid(),
                shop.OrderId));
            Assert.Equal(PostgresErrorCodes.CheckViolation, negative.SqlState);
            Assert.Equal("cost_items_amount_ck", negative.ConstraintName);

            var missingCategory = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO calculator.cost_items (id, order_id, category, amount, sort_order, created_at)
                VALUES ({0}, {1}, NULL, 1.00, 1, TIMESTAMPTZ '2026-09-27 12:00:00+00')
                """,
                Guid.NewGuid(),
                shop.OrderId));
            Assert.Equal(PostgresErrorCodes.NotNullViolation, missingCategory.SqlState);

            db.CostItems.Add(new CostItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Category = "Material",
                Amount = 1m,
                SortOrder = 1,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            var order = await db.Orders.SingleAsync(candidate => candidate.Id == shop.OrderId);
            db.Orders.Remove(order);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            Assert.True(await db.CostItems.AnyAsync(item => item.OrderId == shop.OrderId));
            Assert.True(await db.Orders.AnyAsync(candidate => candidate.Id == shop.OrderId));
        }

        await CreateUserAsync(shop.Director, "costowner", await CreateRoleAsync(shop.Director, "Cost Owner", PermissionCatalog.Calculator.EditCosts));
        var owner = await LoginAsync("costowner");
        var owned = await CreateAsync(owner, shop.ThirdOrderId, new { category = "Material", amount = 3m });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            var user = await db.Users.SingleAsync(candidate => candidate.Username == "costowner");
            await db.Sessions.Where(session => session.UserId == user.Id).ExecuteDeleteAsync();
            await db.UserRoles.Where(assignment => assignment.UserId == user.Id).ExecuteDeleteAsync();
            db.Users.Remove(user);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            Assert.True(await db.Users.AnyAsync(candidate => candidate.Id == user.Id));
            Assert.Equal(3m, (await db.CostItems.SingleAsync(item => item.Id == owned.Id)).Amount);
        }

        await AssertInvariantAsync(shop.OrderId);
        await AssertInvariantAsync(shop.OtherOrderId);
        await AssertInvariantAsync(shop.ThirdOrderId);
    }

    private async Task AssertInvariantAsync(Guid orderId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        var costPrice = await db.Orders.AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => order.CostPrice)
            .SingleAsync();
        var total = await db.CostItems.AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .SumAsync(item => (decimal?)item.Amount) ?? 0m;
        Assert.Equal(total, costPrice);
    }

    private async Task<OrderRow> LoadOrderAsync(Guid orderId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        return await db.Orders.AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new OrderRow(order.CostPrice, order.SellingPrice, order.UpdatedAt, order.UpdatedBy))
            .SingleAsync();
    }

    private async Task<CostItem> LoadItemAsync(Guid costItemId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
        return await db.CostItems.AsNoTracking().SingleAsync(item => item.Id == costItemId);
    }

    private async Task<Shop> StartAsync()
    {
        var director = await DirectorClientAsync();
        var me = (await director.GetFromJsonAsync<CurrentUserBody>("/api/auth/me"))!;
        var orderType = await CreateOrderTypeAsync(director, "Cost Type", null);
        var project = await CreateProjectAsync(director, (await CreateClientAsync(director, "Cost Client")).Id, "Cost Project");
        var first = await CreateOrderAsync(director, project.Id, orderType.Id, "First");
        var second = await CreateOrderAsync(director, project.Id, orderType.Id, "Second");
        var third = await CreateOrderAsync(director, project.Id, orderType.Id, "Third");
        return new Shop(director, me.Id, project.Id, orderType.Id, first.Id, second.Id, third.Id);
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

    private static async Task<CostItemBody> CreateAsync(HttpClient client, Guid orderId, object body)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cost-items", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, text);
        return JsonSerializer.Deserialize<CostItemBody>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<CostItemBody> UpdateAsync(HttpClient client, Guid orderId, Guid costItemId, object body)
    {
        var response = await client.PatchAsJsonAsync($"/api/orders/{orderId}/cost-items/{costItemId}", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonSerializer.Deserialize<CostItemBody>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<CostListBody> ListResponseAsync(HttpClient client, Guid orderId)
    {
        var response = await client.GetAsync($"/api/orders/{orderId}/cost-items");
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonSerializer.Deserialize<CostListBody>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<CostItemBody[]> ListAsync(HttpClient client, Guid orderId) =>
        (await ListResponseAsync(client, orderId)).Items;

    private static async Task<CostListBody> ReorderAsync(HttpClient client, Guid orderId, Guid[] ids)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cost-items/reorder", new { ids });
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonSerializer.Deserialize<CostListBody>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<CalculatorStamp> SaveAsync(HttpClient client, Guid orderId, object fieldValues, DateTimeOffset updatedAt)
    {
        var response = await client.PatchAsJsonAsync($"/api/orders/{orderId}/calculator", new { fieldValues, updatedAt });
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonSerializer.Deserialize<CalculatorStamp>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<CalculatorStamp> ResetAsync(HttpClient client, Guid orderId, DateTimeOffset updatedAt)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/calculator/reset", new { updatedAt });
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonSerializer.Deserialize<CalculatorStamp>(text, JsonSerializerOptions.Web)!;
    }

    private static async Task<PublishedVersion> PublishAsync(HttpClient client, string name, CalculatorTemplateDefinition definition)
    {
        var created = await client.PostAsJsonAsync("/api/calculator-templates", new { name });
        var template = (await created.Content.ReadFromJsonAsync<TemplateBody>())!;
        var draft = await client.PatchAsJsonAsync(
            $"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}",
            new { definition });
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        var published = await client.PostAsync($"/api/calculator-templates/{template.Id}/versions/{template.DraftVersionId}/publish", null);
        var version = (await published.Content.ReadFromJsonAsync<VersionBody>())!;
        return new PublishedVersion(template.Id, version.Id);
    }

    private static async Task<IdBody> CreateOrderTypeAsync(HttpClient client, string name, Guid? templateId)
    {
        var response = await client.PostAsJsonAsync("/api/order-types", new { name, calculatorTemplateId = templateId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdBody>())!;
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
        var saved = await client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new { permissionIds });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        return role.Id;
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

    private sealed record Shop(
        HttpClient Director,
        Guid UserId,
        Guid ProjectId,
        Guid OrderTypeId,
        Guid OrderId,
        Guid OtherOrderId,
        Guid ThirdOrderId);

    private sealed record OrderRow(decimal CostPrice, decimal SellingPrice, DateTimeOffset? UpdatedAt, Guid? UpdatedBy);

    private sealed record CostItemBody(
        Guid Id,
        string Category,
        string? Supplier,
        DateOnly? ExpenseDate,
        string? Description,
        decimal Amount,
        int SortOrder,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    private sealed record CostListBody(CostItemBody[] Items, decimal TotalCost);

    private sealed record CalculatorStamp(decimal? SellingPrice, DateTimeOffset UpdatedAt);

    private sealed record TemplateBody(Guid Id, Guid? DraftVersionId);

    private sealed record VersionBody(Guid Id);

    private sealed record PublishedVersion(Guid TemplateId, Guid VersionId);

    private sealed record IdBody(Guid Id);

    private sealed record RoleBody(Guid Id, string Name);

    private sealed record PermissionBody(Guid Id, string Code);
}
