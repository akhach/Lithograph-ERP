using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LithographERP.Infrastructure.Modules.Authentication;

public sealed class RoleAdminService(LithographDbContext db) : IRoleAdminService
{
    public async Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.WithPermissions().OrderBy(role => role.Name).ToListAsync(cancellationToken);
        return roles.Select(AuthSupport.ToRoleResponse).ToArray();
    }

    public async Task<RoleResponse> GetAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        AuthSupport.ToRoleResponse(await FindRoleAsync(roleId, cancellationToken));

    public async Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await db.Permissions
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Code)
            .ToListAsync(cancellationToken);
        return permissions.Select(AuthSupport.ToPermissionResponse).ToArray();
    }

    public async Task<RoleResponse> CreateAsync(Guid actorId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var displayName = AuthSupport.RequireDisplayName(name, "name");
        var now = DateTimeOffset.UtcNow;
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = displayName,
            NormalizedName = Names.Normalize(displayName),
            Description = NormalizeDescription(description),
            IsSystem = false,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        db.Roles.Add(role);
        await AuthSupport.SaveChangesAsync(db, cancellationToken);
        return AuthSupport.ToRoleResponse(role);
    }

    public async Task<RoleResponse> UpdateAsync(Guid actorId, Guid roleId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(roleId, cancellationToken);
        var displayName = AuthSupport.RequireDisplayName(name, "name");
        var normalized = Names.Normalize(displayName);
        if (role.IsSystem && !string.Equals(normalized, role.NormalizedName, StringComparison.Ordinal))
        {
            throw new AuthException(
                AuthErrorCodes.SystemRoleProtected,
                "This system Role cannot be modified in that way.",
                409);
        }

        role.Name = displayName;
        role.NormalizedName = normalized;
        role.Description = NormalizeDescription(description);
        role.UpdatedAt = DateTimeOffset.UtcNow;
        role.UpdatedBy = actorId;
        await AuthSupport.SaveChangesAsync(db, cancellationToken);
        return AuthSupport.ToRoleResponse(role);
    }

    public async Task<RoleResponse> SetPermissionsAsync(
        Guid actorId,
        Guid roleId,
        IReadOnlyList<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(roleId, cancellationToken);
        if (role.IsSystem)
        {
            throw new AuthException(
                AuthErrorCodes.SystemRoleProtected,
                "This system Role cannot be modified in that way.",
                409);
        }

        var ids = permissionIds.Distinct().ToArray();
        var permissions = ids.Length == 0
            ? []
            : await db.Permissions.Where(permission => ids.Contains(permission.Id)).ToListAsync(cancellationToken);
        if (permissions.Count != ids.Length)
        {
            throw new AuthException(AuthErrorCodes.PermissionNotFound, "The permission could not be found.", 404);
        }

        var desired = permissions.Select(permission => permission.Id).ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var existing in role.RolePermissions.Where(assignment => !desired.Contains(assignment.PermissionId)).ToArray())
        {
            role.RolePermissions.Remove(existing);
            db.RolePermissions.Remove(existing);
        }

        var current = role.RolePermissions.Select(assignment => assignment.PermissionId).ToHashSet();
        foreach (var permission in permissions.Where(permission => !current.Contains(permission.Id)))
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Permission = permission,
                AssignedAt = now,
                AssignedBy = actorId,
            });
        }

        role.UpdatedAt = now;
        role.UpdatedBy = actorId;
        await db.SaveChangesAsync(cancellationToken);
        return AuthSupport.ToRoleResponse(role);
    }

    private async Task<Role> FindRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var role = await db.Roles.WithPermissions().SingleOrDefaultAsync(candidate => candidate.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new AuthException(AuthErrorCodes.RoleNotFound, "The role could not be found.", 404);
        }

        return role;
    }

    private static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
