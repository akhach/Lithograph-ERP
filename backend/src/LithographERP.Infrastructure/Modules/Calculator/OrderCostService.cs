using System.Globalization;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Application.Modules.Orders;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Infrastructure.Persistence;
using LithographERP.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Calculator;

public sealed class OrderCostService(
    LithographDbContext db,
    TimeProvider time,
    ILogger<OrderCostService> logger) : IOrderCostService
{
    public const decimal MaximumAmount = 9_999_999_999_999_999.99m;

    public async Task<CostItemListResponse> ListAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw OrderNotFound();
        }

        var items = await db.CostItems.AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var total = await SumAsync(orderId, cancellationToken);
        if (order.CostPrice != total)
        {
            logger.LogWarning(
                "Order {OrderId} cost_price {CostPrice} does not match Cost Item total {TotalCost}.",
                orderId,
                order.CostPrice,
                total);
        }

        return new CostItemListResponse(items.Select(ToResponse).ToArray(), total);
    }

    public async Task<CostItemResponse> CreateAsync(
        Guid actorId,
        Guid orderId,
        SaveCostItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = RequireCategory(request.Category);
        var supplier = OptionalSupplier(request.Supplier);
        var description = OptionalDescription(request.Description);
        var amount = RequireAmount(request.Amount, request.AmountSupplied);
        if (request.SortOrder is < 0)
        {
            throw AuthException.Validation("sortOrder", "Sort order cannot be negative.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await LockOrderAsync(orderId, cancellationToken);
        EnsureMutable(order);
        var sortOrder = request.SortOrder ?? await NextSortOrderAsync(orderId, cancellationToken);
        var now = time.GetUtcNow();
        var item = new CostItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Category = category,
            Supplier = supplier,
            ExpenseDate = request.ExpenseDate,
            Description = description,
            Amount = amount,
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        db.CostItems.Add(item);
        await SaveAsync(cancellationToken);
        await SyncCostPriceAsync(order, actorId, now, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(item);
    }

    public async Task<CostItemResponse> UpdateAsync(
        Guid actorId,
        Guid orderId,
        Guid costItemId,
        UpdateCostItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = RequireCategory(request.Category);
        var supplier = OptionalSupplier(request.Supplier);
        var description = OptionalDescription(request.Description);
        var amount = RequireAmount(request.Amount, request.AmountSupplied);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await LockOrderAsync(orderId, cancellationToken);
        EnsureMutable(order);
        var item = await LoadItemAsync(orderId, costItemId, cancellationToken);
        var now = time.GetUtcNow();
        item.Category = category;
        item.Supplier = supplier;
        item.ExpenseDate = request.ExpenseDate;
        item.Description = description;
        item.Amount = amount;
        item.UpdatedAt = now;
        item.UpdatedBy = actorId;
        await SaveAsync(cancellationToken);
        await SyncCostPriceAsync(order, actorId, now, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(item);
    }

    public async Task DeleteAsync(
        Guid actorId,
        Guid orderId,
        Guid costItemId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await LockOrderAsync(orderId, cancellationToken);
        EnsureMutable(order);
        var item = await LoadItemAsync(orderId, costItemId, cancellationToken);
        db.CostItems.Remove(item);
        await SaveAsync(cancellationToken);
        await SyncCostPriceAsync(order, actorId, time.GetUtcNow(), cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CostItemListResponse> ReorderAsync(
        Guid actorId,
        Guid orderId,
        IReadOnlyList<Guid>? ids,
        CancellationToken cancellationToken = default)
    {
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var order = await LockOrderAsync(orderId, cancellationToken);
            EnsureMutable(order);
            var items = await db.CostItems
                .Where(item => item.OrderId == orderId)
                .ToListAsync(cancellationToken);
            ApplyOrder(items, ids);
            var now = time.GetUtcNow();
            foreach (var item in items)
            {
                if (db.Entry(item).Property(candidate => candidate.SortOrder).IsModified)
                {
                    item.UpdatedAt = now;
                    item.UpdatedBy = actorId;
                }
            }

            await SaveAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return await ListAsync(orderId, cancellationToken);
    }

    private async Task<Order> LockOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM orders.orders WHERE id = {orderId} FOR UPDATE",
            cancellationToken);
        var order = await db.Orders.SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw OrderNotFound();
        }

        return order;
    }

    private async Task<CostItem> LoadItemAsync(Guid orderId, Guid costItemId, CancellationToken cancellationToken)
    {
        var item = await db.CostItems.SingleOrDefaultAsync(
            candidate => candidate.Id == costItemId && candidate.OrderId == orderId,
            cancellationToken);
        if (item is null)
        {
            throw new AuthException(CostErrorCodes.ItemNotFound, "The Cost Item could not be found.", 404);
        }

        return item;
    }

    private async Task SyncCostPriceAsync(Order order, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var total = await SumAsync(order.Id, cancellationToken);
        if (total > MaximumAmount)
        {
            throw AuthException.Validation("amount", "The Cost total is outside the allowed range.");
        }

        if (order.CostPrice == total)
        {
            return;
        }

        order.CostPrice = total;
        order.UpdatedAt = now;
        order.UpdatedBy = actorId;
    }

    private async Task<decimal> SumAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var total = await db.CostItems
            .Where(item => item.OrderId == orderId)
            .SumAsync(item => (decimal?)item.Amount, cancellationToken);
        return total ?? 0m;
    }

    private async Task<int> NextSortOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var max = await db.CostItems
            .Where(item => item.OrderId == orderId)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync(cancellationToken);
        return (max ?? 0) + 10;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsAmountConstraint(exception))
        {
            logger.LogInformation(exception, "Cost amount was rejected by the database.");
            throw new AuthException(CostErrorCodes.AmountInvalid, "Cost amount must be greater than zero.", 400);
        }
        catch (DbUpdateException exception) when (IsNumericOverflow(exception))
        {
            logger.LogInformation(exception, "Cost total exceeded numeric(18,2).");
            throw AuthException.Validation("amount", "The Cost total is outside the allowed range.");
        }
    }

    private static void ApplyOrder(List<CostItem> rows, IReadOnlyList<Guid>? ids)
    {
        if (ids is null)
        {
            throw AuthException.Validation("ids", "A value is required.");
        }

        var distinct = ids.Distinct().Count();
        var rowIds = rows.Select(item => item.Id).ToHashSet();
        if (ids.Count != distinct || rows.Count != ids.Count || ids.Any(itemId => !rowIds.Contains(itemId)))
        {
            throw AuthException.Validation("ids", "The order does not match the current items.");
        }

        var position = new Dictionary<Guid, int>(ids.Count);
        for (var index = 0; index < ids.Count; index++)
        {
            position[ids[index]] = index;
        }

        foreach (var row in rows)
        {
            row.SortOrder = (position[row.Id] + 1) * 10;
        }
    }

    private static void EnsureMutable(Order order)
    {
        if (order.Status == OrderStatuses.Cancelled)
        {
            throw new AuthException(
                CostErrorCodes.MutationNotAllowed,
                "Costs cannot be changed in the current Order state.",
                409);
        }
    }

    private static decimal RequireAmount(string? text, bool supplied)
    {
        if (!supplied || string.IsNullOrWhiteSpace(text))
        {
            throw AuthException.Validation("amount", "A value is required.");
        }

        if (!decimal.TryParse(
                text.Trim(),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount)
            || amount != decimal.Round(amount, 2, MidpointRounding.AwayFromZero))
        {
            throw AuthException.Validation("amount", "Enter an amount with up to 2 decimal places.");
        }

        if (amount <= 0)
        {
            throw new AuthException(CostErrorCodes.AmountInvalid, "Cost amount must be greater than zero.", 400);
        }

        if (amount > MaximumAmount)
        {
            throw AuthException.Validation("amount", "The amount is outside the allowed range.");
        }

        return amount;
    }

    private static string RequireCategory(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw AuthException.Validation("category", "A value is required.");
        }

        if (trimmed.Length > CostItemConfiguration.CategoryMaxLength)
        {
            throw AuthException.Validation("category", $"Must be at most {CostItemConfiguration.CategoryMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? OptionalSupplier(string? value) =>
        OptionalBounded(value, "supplier", CostItemConfiguration.SupplierMaxLength);

    private static string? OptionalDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? OptionalBounded(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw AuthException.Validation(field, $"Must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static CostItemResponse ToResponse(CostItem item) =>
        new(
            item.Id,
            item.Category,
            item.Supplier,
            item.ExpenseDate,
            item.Description,
            item.Amount,
            item.SortOrder,
            item.CreatedAt,
            item.UpdatedAt);

    private static AuthException OrderNotFound() =>
        new(OrderErrorCodes.OrderNotFound, "The Order could not be found.", 404);

    private static bool IsAmountConstraint(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.CheckViolation
        && (postgres.ConstraintName ?? string.Empty).Contains("amount", StringComparison.Ordinal);

    private static bool IsNumericOverflow(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.NumericValueOutOfRange;
}
