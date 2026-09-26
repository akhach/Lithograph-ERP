using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LithographERP.Infrastructure.Modules.Authentication;

public sealed class UserAdminService(
    LithographDbContext db,
    PasswordHashing passwords,
    AuthService auth,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users.WithRoles().OrderBy(user => user.Username).ToListAsync(cancellationToken);
        return users.Select(AuthSupport.ToUserResponse).ToArray();
    }

    public async Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        AuthSupport.ToUserResponse(await FindUserAsync(userId, cancellationToken));

    public async Task<UserResponse> CreateAsync(Guid actorId, CreateUserRequest request, bool canAssignRoles, CancellationToken cancellationToken = default)
    {
        var username = AuthSupport.RequireDisplayName(request.Username, "username");
        AuthSupport.RequirePassword(request.Password);
        var roleIds = (request.RoleIds ?? []).Distinct().ToArray();
        if (roleIds.Length > 0 && !canAssignRoles)
        {
            throw new AuthException(
                AuthErrorCodes.PermissionDenied,
                "You do not have permission to perform this action.",
                403);
        }

        var roles = await LoadRolesAsync(roleIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = Names.Normalize(username),
            IsActive = request.IsActive ?? true,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                Role = role,
                AssignedAt = now,
                AssignedBy = actorId,
            });
        }

        db.Users.Add(user);
        await AuthSupport.SaveChangesAsync(db, cancellationToken);
        logger.LogInformation("User {UserId} created user {CreatedUserId}.", actorId, user.Id);
        return AuthSupport.ToUserResponse(user);
    }

    public async Task<UserResponse> UpdateUsernameAsync(Guid actorId, Guid userId, string username, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var displayName = AuthSupport.RequireDisplayName(username, "username");
        user.Username = displayName;
        user.NormalizedUsername = Names.Normalize(displayName);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = actorId;
        await AuthSupport.SaveChangesAsync(db, cancellationToken);
        return AuthSupport.ToUserResponse(user);
    }

    public async Task<UserResponse> ActivateAsync(Guid actorId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = actorId;
        await db.SaveChangesAsync(cancellationToken);
        return AuthSupport.ToUserResponse(user);
    }

    public async Task<UserResponse> DeactivateAsync(Guid actorId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AuthSupport.LockDirectorAsync(db, cancellationToken);

        var user = await FindUserAsync(userId, cancellationToken);
        if (user.IsActive && AuthSupport.IsDirector(user) && await AuthSupport.CountActiveDirectorsAsync(db, cancellationToken) <= 1)
        {
            throw new AuthException(
                AuthErrorCodes.FinalDirectorRequired,
                "At least one active Director must remain.",
                409);
        }

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = actorId;
        await auth.RevokeSessionsAsync(user.Id, keepTokenHash: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("User {UserId} deactivated user {TargetUserId}.", actorId, user.Id);
        return AuthSupport.ToUserResponse(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        AuthSupport.RequirePassword(newPassword, "newPassword");
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AuthErrorCodes.UserNotFound, "The user could not be found.", 404);
        }

        user.PasswordHash = passwords.Hash(user, newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await auth.RevokeSessionsAsync(user.Id, keepTokenHash: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset for user {UserId}.", user.Id);
    }

    public async Task<UserResponse> AssignRoleAsync(Guid actorId, Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        var role = await db.Roles.SingleOrDefaultAsync(candidate => candidate.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new AuthException(AuthErrorCodes.RoleNotFound, "The role could not be found.", 404);
        }

        if (user.UserRoles.Any(assignment => assignment.RoleId == roleId))
        {
            throw new AuthException(AuthErrorCodes.RoleAlreadyAssigned, "That role is already assigned to this user.", 409);
        }

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = actorId,
        });
        await db.SaveChangesAsync(cancellationToken);
        return AuthSupport.ToUserResponse(user);
    }

    public async Task<UserResponse> RemoveRoleAsync(Guid actorId, Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AuthSupport.LockDirectorAsync(db, cancellationToken);

        var user = await FindUserAsync(userId, cancellationToken);
        var assignment = user.UserRoles.SingleOrDefault(candidate => candidate.RoleId == roleId);
        if (assignment is null)
        {
            throw new AuthException(AuthErrorCodes.RoleNotAssigned, "That role is not assigned to this user.", 409);
        }

        var removesDirector = assignment.Role.NormalizedName == DirectorRole.NormalizedName;
        if (user.IsActive && removesDirector && await AuthSupport.CountActiveDirectorsAsync(db, cancellationToken) <= 1)
        {
            throw new AuthException(
                AuthErrorCodes.FinalDirectorRequired,
                "At least one active Director must remain.",
                409);
        }

        user.UserRoles.Remove(assignment);
        db.UserRoles.Remove(assignment);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = actorId;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AuthSupport.ToUserResponse(user);
    }

    private async Task<User> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.WithRoles().SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AuthErrorCodes.UserNotFound, "The user could not be found.", 404);
        }

        return user;
    }

    private async Task<List<Role>> LoadRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var roles = await db.Roles.Where(role => roleIds.Contains(role.Id)).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Count)
        {
            throw new AuthException(AuthErrorCodes.RoleNotFound, "The role could not be found.", 404);
        }

        return roles;
    }
}
