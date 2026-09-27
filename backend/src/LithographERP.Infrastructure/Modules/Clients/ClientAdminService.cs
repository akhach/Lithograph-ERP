using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Clients;
using LithographERP.Application.Modules.Numbering;
using LithographERP.Domain.Modules.Clients;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Clients;

public sealed class ClientAdminService(
    LithographDbContext db,
    IBusinessIdGenerator businessIds,
    ILogger<ClientAdminService> logger) : IClientAdminService
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 200;

    public async Task<ClientPage<ClientListItem>> ListAsync(ClientListRequest request, CancellationToken cancellationToken = default)
    {
        var query = Filtered(request);
        var total = await query.CountAsync(cancellationToken);
        var pageCount = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        var items = await Ordered(query, request)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(client => new ClientListItem(
                client.Id,
                client.BusinessId,
                client.Name,
                client.ContactPerson,
                client.Phone,
                client.Email,
                client.IsActive))
            .ToListAsync(cancellationToken);
        return new ClientPage<ClientListItem>(items, request.Page, request.PageSize, total, pageCount);
    }

    public async Task<ClientPage<ClientSelectorItem>> ListSelectorAsync(ClientListRequest request, CancellationToken cancellationToken = default)
    {
        var query = Filtered(request);
        var total = await query.CountAsync(cancellationToken);
        var pageCount = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        var items = await Ordered(query, request)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(client => new ClientSelectorItem(client.Id, client.BusinessId, client.Name))
            .ToListAsync(cancellationToken);
        return new ClientPage<ClientSelectorItem>(items, request.Page, request.PageSize, total, pageCount);
    }

    public async Task<ClientDetail> GetAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        ToDetail(await FindAsync(clientId, cancellationToken));

    public async Task<ClientDetail> CreateAsync(Guid actorId, SaveClientRequest request, CancellationToken cancellationToken = default)
    {
        var name = RequireName(request.Name);
        var contactPerson = Optional(request.ContactPerson, "contactPerson", 200);
        var phone = Optional(request.Phone, "phone", 50);
        var email = Email(request.Email);
        var address = Text(request.Address);
        var notes = Text(request.Notes);
        var client = new Client
        {
            Id = Guid.NewGuid(),
            BusinessId = await businessIds.GenerateClientBusinessIdAsync(cancellationToken),
            Name = name,
            ContactPerson = contactPerson,
            Phone = phone,
            Email = email,
            Address = address,
            Notes = notes,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actorId,
        };
        db.Clients.Add(client);
        await SaveAsync(cancellationToken);
        return ToDetail(client);
    }

    public async Task<ClientDetail> UpdateAsync(Guid actorId, Guid clientId, SaveClientRequest request, CancellationToken cancellationToken = default)
    {
        var client = await FindAsync(clientId, cancellationToken);
        client.Name = RequireName(request.Name);
        client.ContactPerson = Optional(request.ContactPerson, "contactPerson", 200);
        client.Phone = Optional(request.Phone, "phone", 50);
        client.Email = Email(request.Email);
        client.Address = Text(request.Address);
        client.Notes = Text(request.Notes);
        Touch(client, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(client);
    }

    public async Task<ClientDetail> ActivateAsync(Guid actorId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await FindAsync(clientId, cancellationToken);
        client.IsActive = true;
        Touch(client, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(client);
    }

    public async Task<ClientDetail> DeactivateAsync(Guid actorId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await FindAsync(clientId, cancellationToken);
        client.IsActive = false;
        Touch(client, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(client);
    }

    private IQueryable<Client> Filtered(ClientListRequest request)
    {
        var clients = db.Clients.AsQueryable();
        if (request.IsActive is not null)
        {
            clients = clients.Where(client => client.IsActive == request.IsActive);
        }

        if (request.Search is not null)
        {
            var pattern = LikePattern(request.Search);
            clients = clients.Where(client =>
                EF.Functions.ILike(client.BusinessId, pattern, "\\")
                || EF.Functions.ILike(client.Name, pattern, "\\")
                || (client.ContactPerson != null && EF.Functions.ILike(client.ContactPerson, pattern, "\\"))
                || (client.Phone != null && EF.Functions.ILike(client.Phone, pattern, "\\"))
                || (client.Email != null && EF.Functions.ILike(client.Email, pattern, "\\")));
        }

        return clients;
    }

    private static IQueryable<Client> Ordered(IQueryable<Client> clients, ClientListRequest request) =>
        request.Sort switch
        {
            "business_id" => request.Descending
                ? clients.OrderByDescending(client => client.BusinessId).ThenBy(client => client.Id)
                : clients.OrderBy(client => client.BusinessId).ThenBy(client => client.Id),
            "name" => request.Descending
                ? clients.OrderByDescending(client => client.Name).ThenBy(client => client.Id)
                : clients.OrderBy(client => client.Name).ThenBy(client => client.Id),
            "created_at" => request.Descending
                ? clients.OrderByDescending(client => client.CreatedAt).ThenBy(client => client.Id)
                : clients.OrderBy(client => client.CreatedAt).ThenBy(client => client.Id),
            "is_active" => request.Descending
                ? clients.OrderByDescending(client => client.IsActive).ThenBy(client => client.Id)
                : clients.OrderBy(client => client.IsActive).ThenBy(client => client.Id),
            _ => throw AuthException.Validation("sort", "Sort must be business_id, name, created_at, or is_active."),
        };

    private async Task<Client> FindAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new AuthException(ClientErrorCodes.ClientNotFound, "The Client could not be found.", 404);
        }

        return client;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation
            && (postgres.ConstraintName ?? string.Empty).Contains("business_id", StringComparison.Ordinal))
        {
            logger.LogWarning(exception, "Client Business ID conflict while saving a Client.");
            throw new AuthException(
                ClientErrorCodes.BusinessIdConflict,
                "The Client ID could not be assigned. Try again.",
                409);
        }
    }

    private static ClientDetail ToDetail(Client client) =>
        new(
            client.Id,
            client.BusinessId,
            client.Name,
            client.ContactPerson,
            client.Phone,
            client.Email,
            client.Address,
            client.Notes,
            client.IsActive,
            client.CreatedAt,
            client.UpdatedAt);

    private static void Touch(Client client, Guid actorId)
    {
        client.UpdatedAt = DateTimeOffset.UtcNow;
        client.UpdatedBy = actorId;
    }

    private static string RequireName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("name", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 250)
        {
            throw AuthException.Validation("name", "Must be at most 250 characters.");
        }

        return trimmed;
    }

    private static string? Optional(string? value, string field, int maxLength)
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

    private static string? Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? Email(string? value)
    {
        var email = Optional(value, "email", 200);
        if (email is null)
        {
            return null;
        }

        var at = email.IndexOf('@');
        var domain = at > 0 && at == email.LastIndexOf('@') ? email[(at + 1)..] : string.Empty;
        if (domain.Length == 0 || !domain.Contains('.', StringComparison.Ordinal) || domain.StartsWith('.') || domain.EndsWith('.'))
        {
            throw AuthException.Validation("email", "Enter a valid email address.");
        }

        return email;
    }

    private static string LikePattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    public static ClientListRequest Normalize(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        string? sort,
        string? direction,
        string? view)
    {
        if (page is < 1)
        {
            throw AuthException.Validation("page", "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw AuthException.Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }

        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.Trim().ToLowerInvariant();
        var normalizedDirection = string.IsNullOrWhiteSpace(direction) ? "asc" : direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
        {
            throw AuthException.Validation("direction", "Direction must be asc or desc.");
        }

        var normalizedView = string.IsNullOrWhiteSpace(view) ? "list" : view.Trim().ToLowerInvariant();
        if (normalizedView is not ("list" or "selector"))
        {
            throw AuthException.Validation("view", "View must be list or selector.");
        }

        return new ClientListRequest(
            string.IsNullOrWhiteSpace(search) ? null : search,
            isActive,
            page ?? 1,
            pageSize ?? DefaultPageSize,
            normalizedSort,
            normalizedDirection == "desc",
            normalizedView == "selector");
    }
}
