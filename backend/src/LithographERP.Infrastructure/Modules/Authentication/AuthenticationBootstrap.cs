using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LithographERP.Infrastructure.Modules.Authentication;

public sealed class AuthenticationBootstrap(LithographDbContext db) : IAuthenticationBootstrap
{
    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existingPermissions = await db.Permissions.ToListAsync(cancellationToken);
        var byCode = existingPermissions.ToDictionary(permission => permission.Code, StringComparer.Ordinal);

        foreach (var definition in PermissionCatalog.All)
        {
            if (!byCode.TryGetValue(definition.Code, out var permission))
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Code,
                    CreatedAt = now,
                };
                byCode[definition.Code] = permission;
                db.Permissions.Add(permission);
            }

            permission.Name = definition.Name;
            permission.Description = definition.Description;
            permission.Module = definition.Module;
        }

        var director = await db.Roles.SingleOrDefaultAsync(role => role.NormalizedName == DirectorRole.NormalizedName, cancellationToken);
        if (director is null)
        {
            director = new Role
            {
                Id = Guid.NewGuid(),
                CreatedAt = now,
            };
            db.Roles.Add(director);
        }

        director.Name = DirectorRole.Name;
        director.NormalizedName = DirectorRole.NormalizedName;
        director.IsSystem = true;

        await AuthSupport.SaveChangesAsync(db, cancellationToken);

        var assigned = await db.RolePermissions
            .Where(assignment => assignment.RoleId == director.Id)
            .Select(assignment => assignment.PermissionId)
            .ToListAsync(cancellationToken);
        var assignedIds = assigned.ToHashSet();

        foreach (var permission in byCode.Values.Where(permission => PermissionCatalog.Codes.Contains(permission.Code)))
        {
            if (assignedIds.Contains(permission.Id))
            {
                continue;
            }

            db.RolePermissions.Add(new RolePermission
            {
                RoleId = director.Id,
                PermissionId = permission.Id,
                AssignedAt = now,
            });
        }

        await AuthSupport.SaveChangesAsync(db, cancellationToken);
    }
}
