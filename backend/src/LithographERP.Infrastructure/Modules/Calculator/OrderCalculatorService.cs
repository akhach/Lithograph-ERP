using System.Text.Json;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Application.Modules.Orders;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Calculator;

public sealed class OrderCalculatorService(
    LithographDbContext db,
    TimeProvider time,
    ILogger<OrderCalculatorService> logger) : IOrderCalculatorService
{
    public async Task<OrderCalculatorRuntime> GetAsync(
        Guid actorId,
        Guid orderId,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, tracking: false, cancellationToken);
        var calculator = await db.OrderCalculators.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);
        if (calculator is null)
        {
            calculator = await CreateAsync(actorId, order, cancellationToken);
        }

        return await ToRuntimeAsync(order, calculator, access, cancellationToken);
    }

    public async Task<OrderCalculatorRuntime> SaveAsync(
        Guid actorId,
        Guid orderId,
        SaveOrderCalculatorRequest request,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, tracking: true, cancellationToken);
        var calculator = await RequireCalculatorAsync(orderId, cancellationToken);
        EnsureCurrent(orderId, calculator.UpdatedAt, request.UpdatedAt);
        var version = await RequireVersionAsync(orderId, calculator.TemplateVersionId, cancellationToken);
        var definition = RequireUsable(orderId, version.Id, version.Definition);
        var existing = ReadValues(orderId, calculator.FieldValues);
        if (!OrderCalculatorEvaluation.TryPrepare(definition, request.FieldValues, existing, access, out var values, out var inputErrors))
        {
            throw new CalculatorRequestException(
                CalculatorErrorCodes.InvalidInput,
                "One or more Calculator fields are invalid.",
                400,
                errors: inputErrors);
        }

        var computation = OrderCalculatorEvaluation.Compute(definition, values);
        if (computation.Outcome == OrderCalculatorOutcome.CalculationFailed)
        {
            logger.LogWarning("Calculator calculation failed for Order {OrderId}.", orderId);
            var visibleErrors = VisibleErrors(definition, computation.FieldErrors, access);
            var canSeeSellingField = SellingFieldKey(definition, access) is not null;
            throw new CalculatorRequestException(
                CalculatorErrorCodes.CalculationError,
                canSeeSellingField
                    ? computation.FailureMessage ?? "The Calculator could not be calculated."
                    : "The Calculator could not be calculated.",
                400,
                errors: visibleErrors.Count == 0 ? null : FieldMessages(visibleErrors));
        }

        var now = UtcNow();
        calculator.FieldValues = OrderCalculatorEvaluation.WriteStored(values);
        calculator.UpdatedAt = now;
        calculator.UpdatedBy = actorId;
        calculator.LastCalculatedAt = computation.Outcome == OrderCalculatorOutcome.Complete ? now : null;
        order.SellingPrice = computation.AuthoritativeSellingPrice ?? 0m;
        Touch(order, actorId, now);
        await CommitAsync(cancellationToken);
        return await ToRuntimeAsync(order, calculator, access, cancellationToken);
    }

    public async Task<OrderCalculatorRuntime> ResetAsync(
        Guid actorId,
        Guid orderId,
        ResetOrderCalculatorRequest request,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, tracking: true, cancellationToken);
        var calculator = await RequireCalculatorAsync(orderId, cancellationToken);
        EnsureCurrent(orderId, calculator.UpdatedAt, request.UpdatedAt);
        var now = UtcNow();
        calculator.FieldValues = "{}";
        calculator.LastCalculatedAt = null;
        calculator.UpdatedAt = now;
        calculator.UpdatedBy = actorId;
        order.SellingPrice = 0m;
        // Cost Items and cost_price stay on the Order.
        Touch(order, actorId, now);
        await CommitAsync(cancellationToken);
        return await ToRuntimeAsync(order, calculator, access, cancellationToken);
    }

    public async Task ChangeOrderTypeAsync(
        Guid actorId,
        Guid orderId,
        ChangeOrderTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ResetCalculator)
        {
            throw AuthException.Validation("resetCalculator", "Calculator reset must be confirmed.");
        }

        var order = await RequireOrderAsync(orderId, tracking: true, cancellationToken);
        if (request.OrderTypeId == order.OrderTypeId)
        {
            return;
        }

        var target = await db.OrderTypes.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.OrderTypeId, cancellationToken);
        if (target is null)
        {
            throw new AuthException(OrderErrorCodes.OrderTypeNotFound, "The Order Type could not be found.", 404);
        }

        if (!target.IsActive)
        {
            throw new AuthException(
                OrderErrorCodes.OrderTypeInactive,
                "Inactive Order Types cannot be selected for new Orders.",
                400);
        }

        var versionId = await ResolveTargetVersionAsync(target.CalculatorTemplateId, cancellationToken);
        var calculator = await db.OrderCalculators.SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);
        if (calculator is not null)
        {
            if (request.UpdatedAt is null)
            {
                throw AuthException.Validation("updatedAt", "A value is required.");
            }

            EnsureCurrent(orderId, calculator.UpdatedAt, request.UpdatedAt.Value);
        }

        var now = UtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        order.OrderTypeId = target.Id;
        order.SellingPrice = 0m;
        // Cost Items and cost_price stay on the Order.
        Touch(order, actorId, now);
        if (calculator is not null && versionId is null)
        {
            db.OrderCalculators.Remove(calculator);
        }
        else if (calculator is not null && versionId is Guid publishedId)
        {
            calculator.TemplateVersionId = publishedId;
            calculator.FieldValues = "{}";
            calculator.LastCalculatedAt = null;
            calculator.UpdatedAt = now;
            calculator.UpdatedBy = actorId;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<OrderCalculator> CreateAsync(Guid actorId, Order order, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM orders.orders WHERE id = {order.Id} FOR UPDATE",
            cancellationToken);
        var existing = await db.OrderCalculators.SingleOrDefaultAsync(candidate => candidate.OrderId == order.Id, cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

        var versionId = await ResolveTargetVersionAsync(await TemplateIdAsync(order.OrderTypeId, cancellationToken), cancellationToken);
        if (versionId is null)
        {
            throw NotConfigured();
        }

        var now = UtcNow();
        var calculator = new OrderCalculator
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TemplateVersionId = versionId.Value,
            FieldValues = "{}",
            CreatedAt = now,
            CreatedBy = actorId,
            UpdatedAt = now,
            UpdatedBy = actorId,
        };
        db.OrderCalculators.Add(calculator);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return calculator;
        }
        catch (DbUpdateException exception) when (IsUniqueOrder(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return await db.OrderCalculators.AsNoTracking()
                .SingleAsync(candidate => candidate.OrderId == order.Id, cancellationToken);
        }
    }

    private async Task<OrderCalculatorRuntime> ToRuntimeAsync(
        Order order,
        OrderCalculator calculator,
        OrderCalculatorAccess access,
        CancellationToken cancellationToken)
    {
        var version = await RequireVersionAsync(order.Id, calculator.TemplateVersionId, cancellationToken);
        var definition = RequireUsable(order.Id, version.Id, version.Definition);
        var stored = ReadValues(order.Id, calculator.FieldValues);
        var computation = OrderCalculatorEvaluation.Compute(definition, stored);
        var sellingKey = SellingFieldKey(definition, access);
        var calculated = OrderCalculatorEvaluation.VisibleCalculated(definition, computation.CalculatedValues, access);
        if (computation.Outcome != OrderCalculatorOutcome.Complete && sellingKey is not null)
        {
            calculated.Remove(sellingKey);
        }

        return new OrderCalculatorRuntime(
            order.Id,
            calculator.Id,
            new OrderCalculatorTemplateSummary(version.TemplateId, version.TemplateName),
            new OrderCalculatorVersionSummary(version.Id, version.VersionNumber),
            sellingKey,
            OrderCalculatorEvaluation.VisibleElements(definition, access),
            OrderCalculatorEvaluation.VisibleValues(definition, stored, access),
            calculated,
            access.SellingPrice ? order.SellingPrice : null,
            computation.Outcome == OrderCalculatorOutcome.Complete,
            VisibleErrors(definition, computation.FieldErrors, access),
            calculator.UpdatedAt,
            calculator.LastCalculatedAt);
    }

    private async Task<VersionRow> RequireVersionAsync(Guid orderId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.CalculatorTemplateVersions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId, cancellationToken);
        if (version is null)
        {
            logger.LogWarning(
                "Order {OrderId} is bound to missing Template Version {TemplateVersionId}.",
                orderId,
                versionId);
            throw VersionNotAvailable();
        }

        var template = await db.CalculatorTemplates.AsNoTracking()
            .Where(candidate => candidate.Id == version.TemplateId)
            .Select(candidate => new { candidate.Id, candidate.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            logger.LogWarning(
                "Order {OrderId} Template Version {TemplateVersionId} has no Template.",
                orderId,
                versionId);
            throw VersionNotAvailable();
        }

        return new VersionRow(version.Id, version.VersionNumber, version.Definition, template.Id, template.Name);
    }

    private CalculatorTemplateDefinition RequireUsable(Guid orderId, Guid versionId, CalculatorTemplateDefinition source)
    {
        var definition = CalculatorDefinitionJson.Clone(source);
        var validation = TemplateDefinitionValidator.Validate(definition);
        if (validation.IsValid)
        {
            return definition;
        }

        logger.LogWarning(
            "Order {OrderId} Template Version {TemplateVersionId} failed runtime validation with {IssueCode}.",
            orderId,
            versionId,
            validation.Errors[0].Code);
        throw VersionNotAvailable();
    }

    private Dictionary<string, StoredCalculatorValue> ReadValues(Guid orderId, string json)
    {
        try
        {
            return OrderCalculatorEvaluation.ReadStored(json);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Order {OrderId} has unreadable Calculator field values.", orderId);
            throw new CalculatorRequestException(
                CalculatorErrorCodes.CalculationError,
                "The saved Calculator values could not be read.",
                409);
        }
    }

    private async Task<Guid?> TemplateIdAsync(Guid orderTypeId, CancellationToken cancellationToken)
    {
        var type = await db.OrderTypes.AsNoTracking()
            .Where(candidate => candidate.Id == orderTypeId)
            .Select(candidate => new { candidate.CalculatorTemplateId })
            .SingleAsync(cancellationToken);
        return type.CalculatorTemplateId;
    }

    private async Task<Guid?> ResolveTargetVersionAsync(Guid? templateId, CancellationToken cancellationToken)
    {
        if (templateId is null)
        {
            return null;
        }

        var template = await db.CalculatorTemplates.AsNoTracking()
            .Where(candidate => candidate.Id == templateId)
            .Select(candidate => new { candidate.Id, candidate.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            logger.LogWarning("Calculator Template {TemplateId} referenced by an Order Type is missing.", templateId);
            throw VersionNotAvailable();
        }

        if (!template.IsActive)
        {
            throw new CalculatorRequestException(
                CalculatorErrorCodes.TemplateInactive,
                "The selected Calculator Template is inactive.",
                409);
        }

        var published = await db.CalculatorTemplateVersions.AsNoTracking()
            .Where(candidate => candidate.TemplateId == template.Id && candidate.Status == TemplateVersionStatuses.Published)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        if (published.Count == 0)
        {
            throw new CalculatorRequestException(
                CalculatorErrorCodes.NoPublishedVersion,
                "The Calculator Template has no published version.",
                409);
        }

        if (published.Count > 1)
        {
            logger.LogWarning("Calculator Template {TemplateId} has more than one Published Version.", template.Id);
            throw VersionNotAvailable();
        }

        return published[0];
    }

    private async Task<Order> RequireOrderAsync(Guid orderId, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.Orders : db.Orders.AsNoTracking();
        var order = await query.SingleOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw new AuthException(OrderErrorCodes.OrderNotFound, "The Order could not be found.", 404);
        }

        return order;
    }

    private async Task<OrderCalculator> RequireCalculatorAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var calculator = await db.OrderCalculators.SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);
        if (calculator is null)
        {
            throw new CalculatorRequestException(
                CalculatorErrorCodes.CalculatorNotFound,
                "The Order Calculator could not be found.",
                404);
        }

        return calculator;
    }

    private void EnsureCurrent(Guid orderId, DateTimeOffset stored, DateTimeOffset submitted)
    {
        if (Truncate(stored) == Truncate(submitted))
        {
            return;
        }

        logger.LogInformation("Calculator concurrency conflict for Order {OrderId}.", orderId);
        throw new CalculatorRequestException(
            CalculatorErrorCodes.ConcurrencyConflict,
            "This Calculator was changed by another User. Reload the latest values before saving.",
            409);
    }

    private async Task CommitAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void Touch(Order order, Guid actorId, DateTimeOffset now)
    {
        order.UpdatedAt = now;
        order.UpdatedBy = actorId;
    }

    private DateTimeOffset UtcNow() => Truncate(time.GetUtcNow());

    private static DateTimeOffset Truncate(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var ticks = utc.UtcTicks - (utc.UtcTicks % 10);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private static string? SellingFieldKey(CalculatorTemplateDefinition definition, OrderCalculatorAccess access)
    {
        var key = definition.SellingPriceFieldKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        foreach (var element in definition.Elements)
        {
            if (element.Key == key && OrderCalculatorEvaluation.CanView(element.Visibility, access))
            {
                return key;
            }

            foreach (var column in element.Columns ?? [])
            {
                if (column.Key == key && OrderCalculatorEvaluation.CanView(column.Visibility, access))
                {
                    return key;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<OrderCalculatorFieldError> VisibleErrors(
        CalculatorTemplateDefinition definition,
        IReadOnlyList<OrderCalculatorFieldError> errors,
        OrderCalculatorAccess access)
    {
        var catalog = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var element in definition.Elements)
        {
            if (!string.IsNullOrWhiteSpace(element.Key))
            {
                catalog[element.Key] = element.Visibility;
            }

            foreach (var column in element.Columns ?? [])
            {
                if (!string.IsNullOrWhiteSpace(column.Key))
                {
                    catalog[column.Key] = column.Visibility;
                }
            }
        }

        return errors
            .Where(error => !catalog.TryGetValue(error.FieldKey, out var visibility) || OrderCalculatorEvaluation.CanView(visibility, access))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string[]> FieldMessages(IReadOnlyList<OrderCalculatorFieldError> errors) =>
        errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.FieldKey) ? "calculator" : error.FieldKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray(), StringComparer.Ordinal);

    private static bool IsUniqueOrder(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation
        && (postgres.ConstraintName ?? string.Empty).Contains("order_calculators_order_id", StringComparison.Ordinal);

    private static CalculatorRequestException NotConfigured() =>
        new(
            CalculatorErrorCodes.NotConfigured,
            "No Calculator is configured for this Order Type.",
            409);

    private static CalculatorRequestException VersionNotAvailable() =>
        new(
            CalculatorErrorCodes.TemplateVersionNotAvailable,
            "The Calculator Template Version could not be used.",
            409);

    private sealed record VersionRow(
        Guid Id,
        int VersionNumber,
        CalculatorTemplateDefinition Definition,
        Guid TemplateId,
        string TemplateName);
}
