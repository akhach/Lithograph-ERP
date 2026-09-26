using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Authentication;

internal static class AuthSupport
{
    public const long DirectorLockKey = 2_702_7027;

    public static IQueryable<User> WithRoles(this IQueryable<User> users) =>
        users.Include(user => user.UserRoles).ThenInclude(assignment => assignment.Role);

    public static IQueryable<User> WithAccess(this IQueryable<User> users) =>
        users.WithRoles()
            .Include(user => user.UserRoles)
            .ThenInclude(assignment => assignment.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(assignment => assignment.Permission);

    public static IQueryable<Role> WithPermissions(this IQueryable<Role> roles) =>
        roles.Include(role => role.RolePermissions).ThenInclude(assignment => assignment.Permission);

    public static string RequireDisplayName(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation(field, "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > Names.MaxLength)
        {
            throw AuthException.Validation(field, $"Must be at most {Names.MaxLength} characters.");
        }

        return trimmed;
    }

    public static void RequirePassword(string? password, string field = "password")
    {
        if (!PasswordRules.IsAcceptable(password))
        {
            throw AuthException.Validation(field, $"Password must be at least {PasswordRules.MinimumLength} characters.");
        }
    }

    public static async Task SaveChangesAsync(LithographDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var constraint = postgres.ConstraintName ?? string.Empty;
            if (constraint.Contains("normalized_username", StringComparison.Ordinal))
            {
                throw new AuthException(AuthErrorCodes.UsernameAlreadyExists, "That username is already in use.", 409);
            }

            if (constraint.Contains("normalized_name", StringComparison.Ordinal))
            {
                throw new AuthException(AuthErrorCodes.RoleNameAlreadyExists, "That role name is already in use.", 409);
            }

            throw;
        }
    }

    public static async Task LockDirectorAsync(LithographDbContext db, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({DirectorLockKey})", cancellationToken);

    public static Task<int> CountActiveDirectorsAsync(LithographDbContext db, CancellationToken cancellationToken) =>
        db.Users.CountAsync(
            user => user.IsActive && user.UserRoles.Any(assignment => assignment.Role.NormalizedName == DirectorRole.NormalizedName),
            cancellationToken);

    public static bool IsDirector(User user) =>
        user.UserRoles.Any(assignment => assignment.Role.NormalizedName == DirectorRole.NormalizedName);

    public static UserResponse ToUserResponse(User user) =>
        new(
            user.Id,
            user.Username,
            user.IsActive,
            user.LastLoginAt,
            user.UserRoles
                .Select(assignment => new RoleSummaryResponse(assignment.Role.Id, assignment.Role.Name, assignment.Role.IsSystem))
                .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray());

    public static CurrentUserResponse ToCurrentUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.UserRoles.Select(assignment => assignment.Role.Name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
            user.UserRoles.SelectMany(assignment => assignment.Role.RolePermissions).Select(assignment => assignment.Permission.Code).Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray(),
            null);

    public static PermissionResponse ToPermissionResponse(Permission permission) =>
        new(permission.Id, permission.Code, permission.Name, permission.Description, permission.Module);

    public static RoleResponse ToRoleResponse(Role role) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.RolePermissions
                .Select(assignment => ToPermissionResponse(assignment.Permission))
                .OrderBy(permission => permission.Module, StringComparer.Ordinal)
                .ThenBy(permission => permission.Code, StringComparer.Ordinal)
                .ToArray());
}
