using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Orders;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Infrastructure.Persistence;
using LithographERP.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Orders;

public sealed class OrderTypeAdminService(LithographDbContext db, TimeProvider time) : IOrderTypeAdminService
{
    public async Task<IReadOnlyList<OrderTypeResponse>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = db.OrderTypes.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(type => type.IsActive);
        }

        var types = await query.OrderBy(type => type.Name).ThenBy(type => type.Id).ToListAsync(cancellationToken);
        return types.Select(ToResponse).ToArray();
    }

    public async Task<OrderTypeResponse> GetAsync(Guid orderTypeId, CancellationToken cancellationToken = default)
    {
        var type = await db.OrderTypes.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == orderTypeId, cancellationToken);
        if (type is null)
        {
            throw NotFound();
        }

        return ToResponse(type);
    }

    public async Task<OrderTypeResponse> CreateAsync(Guid actorId, SaveOrderTypeRequest request, CancellationToken cancellationToken = default)
    {
        var name = RequireName(request.Name);
        await EnsureNameAvailableAsync(name, exceptId: null, cancellationToken);
        var type = new OrderType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = Text(request.Description),
            IsActive = true,
            CreatedAt = time.GetUtcNow(),
            CreatedBy = actorId,
        };
        db.OrderTypes.Add(type);
        await SaveAsync(cancellationToken);
        return ToResponse(type);
    }

    public async Task<OrderTypeResponse> UpdateAsync(
        Guid actorId,
        Guid orderTypeId,
        SaveOrderTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = await LoadAsync(orderTypeId, cancellationToken);
        var name = RequireName(request.Name);
        await EnsureNameAvailableAsync(name, type.Id, cancellationToken);
        type.Name = name;
        type.Description = Text(request.Description);
        Touch(type, actorId);
        await SaveAsync(cancellationToken);
        return ToResponse(type);
    }

    public async Task<OrderTypeResponse> SetActiveAsync(
        Guid actorId,
        Guid orderTypeId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var type = await LoadAsync(orderTypeId, cancellationToken);
        if (type.IsActive != isActive)
        {
            type.IsActive = isActive;
            Touch(type, actorId);
            await SaveAsync(cancellationToken);
        }

        return ToResponse(type);
    }

    private async Task<OrderType> LoadAsync(Guid orderTypeId, CancellationToken cancellationToken)
    {
        var type = await db.OrderTypes.SingleOrDefaultAsync(candidate => candidate.Id == orderTypeId, cancellationToken);
        if (type is null)
        {
            throw NotFound();
        }

        return type;
    }

    private async Task EnsureNameAvailableAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        var query = db.OrderTypes.Where(type => type.Name.ToLower() == normalized);
        if (exceptId is Guid id)
        {
            query = query.Where(type => type.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw DuplicateName();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && (postgres.ConstraintName ?? string.Empty).Contains("order_types_name", StringComparison.Ordinal))
        {
            throw DuplicateName();
        }
    }

    private void Touch(OrderType type, Guid actorId)
    {
        type.UpdatedAt = time.GetUtcNow();
        type.UpdatedBy = actorId;
    }

    private static string RequireName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("name", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > OrderTypeConfiguration.NameMaxLength)
        {
            throw AuthException.Validation("name", $"Must be at most {OrderTypeConfiguration.NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OrderTypeResponse ToResponse(OrderType type) =>
        new(type.Id, type.Name, type.Description, type.IsActive, type.CreatedAt, type.UpdatedAt);

    private static AuthException NotFound() =>
        new(OrderErrorCodes.OrderTypeNotFound, "The Order Type could not be found.", 404);

    private static AuthException DuplicateName() =>
        new(OrderErrorCodes.OrderTypeNameAlreadyExists, "An Order Type with that name already exists.", 409);
}
