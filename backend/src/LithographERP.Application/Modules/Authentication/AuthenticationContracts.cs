namespace LithographERP.Application.Modules.Authentication;

public sealed class AuthSettings
{
    public const string SectionName = "Authentication";

    public int SessionLifetimeHours { get; set; } = 12;

    public string CookieName { get; set; } = "lithograph.session";

    public string CookieSameSite { get; set; } = "Lax";

    public bool CookieSecure { get; set; } = true;
}

public enum SessionValidationStatus
{
    Valid,
    Expired,
    Revoked,
}

public sealed record SessionValidationResult(SessionValidationStatus Status, Guid? UserId, string? Username);

public sealed record SetupStatusResponse(bool RequiresSetup);

public sealed record CurrentUserResponse(
    Guid Id,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid? LinkedEmployee);

public sealed record RoleSummaryResponse(Guid Id, string Name, bool IsSystem);

public sealed record UserResponse(
    Guid Id,
    string Username,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<RoleSummaryResponse> Roles);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<PermissionResponse> Permissions);

public sealed record PermissionResponse(Guid Id, string Code, string Name, string? Description, string Module);

public sealed record SetupRequest(string Password);

public sealed record LoginRequest(string Username, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record CreateUserRequest(string Username, string Password, IReadOnlyList<Guid>? RoleIds, bool? IsActive);

public sealed record UpdateUserRequest(string Username);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record CreateRoleRequest(string Name, string? Description);

public sealed record UpdateRoleRequest(string Name, string? Description);

public sealed record SetRolePermissionsRequest(IReadOnlyList<Guid> PermissionIds);

public sealed record LoginResult(CurrentUserResponse User, string SessionToken, DateTimeOffset ExpiresAt);

public interface IAuthenticationBootstrap
{
    Task SynchronizeAsync(CancellationToken cancellationToken = default);
}

public interface ISessionValidator
{
    Task<SessionValidationResult> ValidateAsync(string sessionToken, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default);

    Task SetupDirectorAsync(string password, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(string username, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? sessionToken, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, string? currentSessionToken, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(Guid actorId, CreateUserRequest request, bool canAssignRoles, CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateUsernameAsync(Guid actorId, Guid userId, string username, CancellationToken cancellationToken = default);

    Task<UserResponse> ActivateAsync(Guid actorId, Guid userId, CancellationToken cancellationToken = default);

    Task<UserResponse> DeactivateAsync(Guid actorId, Guid userId, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    Task<UserResponse> AssignRoleAsync(Guid actorId, Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<UserResponse> RemoveRoleAsync(Guid actorId, Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse> GetAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse> CreateAsync(Guid actorId, string name, string? description, CancellationToken cancellationToken = default);

    Task<RoleResponse> UpdateAsync(Guid actorId, Guid roleId, string name, string? description, CancellationToken cancellationToken = default);

    Task<RoleResponse> SetPermissionsAsync(Guid actorId, Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default);
}
