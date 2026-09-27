namespace LithographERP.Application.Modules.Employees;

public static class EmployeeErrorCodes
{
    public const string EmployeeNotFound = "EMPLOYEE_NOT_FOUND";
    public const string UserAlreadyLinked = "USER_ALREADY_LINKED_TO_EMPLOYEE";
    public const string EmployeeAlreadyLinked = "EMPLOYEE_ALREADY_LINKED_TO_USER";
    public const string UserLinkNotFound = "EMPLOYEE_USER_LINK_NOT_FOUND";
}

public sealed record LinkedUserSummary(Guid Id, string Username, bool IsActive);

public sealed record EmployeeListItem(
    Guid Id,
    string FullName,
    string? Position,
    string? Phone,
    string? Email,
    bool IsActive,
    LinkedUserSummary? LinkedUser);

public sealed record EmployeeSelectorItem(Guid Id, string FullName, string? Position);

public sealed record EmployeeDetail(
    Guid Id,
    string FullName,
    string? Position,
    string? Phone,
    string? Email,
    bool IsActive,
    LinkedUserSummary? LinkedUser,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record EmployeePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record EmployeeListRequest(
    string? Search,
    bool? IsActive,
    bool? HasUser,
    int Page,
    int PageSize,
    string Sort,
    bool Descending,
    bool Selector);

public sealed record CreateEmployeeRequest(string FullName, string? Position, string? Phone, string? Email, Guid? UserId);

public sealed record UpdateEmployeeRequest(string FullName, string? Position, string? Phone, string? Email);

public sealed record LinkEmployeeUserRequest(Guid UserId);

public interface IEmployeeAdminService
{
    Task<EmployeePage<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken = default);

    Task<EmployeePage<EmployeeSelectorItem>> ListSelectorAsync(EmployeeListRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> GetAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> CreateAsync(Guid actorId, CreateEmployeeRequest request, bool canLinkUser, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> UpdateAsync(Guid actorId, Guid employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> ActivateAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> DeactivateAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> LinkUserAsync(Guid actorId, Guid employeeId, Guid userId, CancellationToken cancellationToken = default);

    Task<EmployeeDetail> UnlinkUserAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkedUserSummary>> ListAvailableUsersAsync(CancellationToken cancellationToken = default);
}
