namespace LithographERP.Application.Modules.Clients;

public static class ClientErrorCodes
{
    public const string ClientNotFound = "CLIENT_NOT_FOUND";
    public const string ClientInactive = "CLIENT_INACTIVE";
    public const string BusinessIdConflict = "CLIENT_BUSINESS_ID_CONFLICT";
}

public sealed record ClientListItem(
    Guid Id,
    string BusinessId,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    bool IsActive);

public sealed record ClientSelectorItem(Guid Id, string BusinessId, string Name);

public sealed record ClientDetail(
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

public sealed record ClientPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record ClientListRequest(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize,
    string Sort,
    bool Descending,
    bool Selector);

public sealed record SaveClientRequest(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);

public interface IClientAdminService
{
    Task<ClientPage<ClientListItem>> ListAsync(ClientListRequest request, CancellationToken cancellationToken = default);

    Task<ClientPage<ClientSelectorItem>> ListSelectorAsync(ClientListRequest request, CancellationToken cancellationToken = default);

    Task<ClientDetail> GetAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<ClientDetail> CreateAsync(Guid actorId, SaveClientRequest request, CancellationToken cancellationToken = default);

    Task<ClientDetail> UpdateAsync(Guid actorId, Guid clientId, SaveClientRequest request, CancellationToken cancellationToken = default);

    Task<ClientDetail> ActivateAsync(Guid actorId, Guid clientId, CancellationToken cancellationToken = default);

    Task<ClientDetail> DeactivateAsync(Guid actorId, Guid clientId, CancellationToken cancellationToken = default);
}
