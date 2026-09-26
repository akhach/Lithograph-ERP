using System.Net;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LithographERP.Infrastructure.Modules.Authentication;

public sealed class AuthService(
    LithographDbContext db,
    PasswordHashing passwords,
    AuthSettings settings,
    ILogger<AuthService> logger) : IAuthService, ISessionValidator
{
    public async Task<SetupStatusResponse> GetSetupStatusAsync(CancellationToken cancellationToken = default) =>
        new(!await db.Users.AnyAsync(cancellationToken));

    public async Task SetupDirectorAsync(string password, CancellationToken cancellationToken = default)
    {
        AuthSupport.RequirePassword(password);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AuthSupport.LockDirectorAsync(db, cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
        {
            throw new AuthException(
                AuthErrorCodes.SetupAlreadyCompleted,
                "Initial setup has already been completed.",
                StatusCodes.Conflict);
        }

        var directorRole = await db.Roles.SingleOrDefaultAsync(
            role => role.NormalizedName == DirectorRole.NormalizedName,
            cancellationToken);
        if (directorRole is null)
        {
            throw new InvalidOperationException("Director role is missing. Authentication catalog synchronization did not run.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = DirectorRole.InitialUsername,
            NormalizedUsername = Names.Normalize(DirectorRole.InitialUsername),
            IsActive = true,
            CreatedAt = now,
        };
        user.PasswordHash = passwords.Hash(user, password);
        user.UserRoles.Add(new UserRole
        {
            RoleId = directorRole.Id,
            AssignedAt = now,
        });
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException postgres && postgres.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            throw new AuthException(
                AuthErrorCodes.SetupAlreadyCompleted,
                "Initial setup has already been completed.",
                StatusCodes.Conflict);
        }

        logger.LogInformation("Initial Director account created.");
    }

    public async Task<LoginResult> LoginAsync(
        string username,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalized = Names.Normalize(username ?? string.Empty);
        var user = await db.Users.WithAccess().SingleOrDefaultAsync(candidate => candidate.NormalizedUsername == normalized, cancellationToken);
        var passwordMatches = user is not null && passwords.Verify(user, password ?? string.Empty);
        if (user is null)
        {
            passwords.VerifyDummy(password ?? string.Empty);
        }

        if (user is null || !user.IsActive || !passwordMatches)
        {
            logger.LogInformation("Login failed for normalized username {NormalizedUsername}.", normalized);
            throw new AuthException(AuthErrorCodes.InvalidCredentials, "Invalid username or password.", StatusCodes.Unauthorized);
        }

        var now = DateTimeOffset.UtcNow;
        var token = SessionTokens.Create();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SessionTokens.Hash(token),
            CreatedAt = now,
            ExpiresAt = now.AddHours(settings.SessionLifetimeHours),
            LastActivityAt = now,
            IpAddress = ParseIp(ipAddress),
            UserAgent = Truncate(userAgent),
            IsActive = true,
        };
        user.LastLoginAt = now;
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} logged in.", user.Id);
        return new LoginResult(AuthSupport.ToCurrentUser(user), token, session.ExpiresAt);
    }

    public async Task LogoutAsync(string? sessionToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            var hash = SessionTokens.Hash(sessionToken);
            var session = await db.Sessions.SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);
            if (session is not null)
            {
                session.IsActive = false;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.WithAccess().SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AuthErrorCodes.UserNotFound, "The user could not be found.", StatusCodes.NotFound);
        }

        return AuthSupport.ToCurrentUser(user);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string? currentSessionToken,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        AuthSupport.RequirePassword(newPassword, "newPassword");
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null || !passwords.Verify(user, currentPassword ?? string.Empty))
        {
            throw AuthException.Validation("currentPassword", "Current password is incorrect.");
        }

        user.PasswordHash = passwords.Hash(user, newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.UpdatedBy = userId;
        await RevokeSessionsAsync(userId, KeepToken(currentSessionToken), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} changed their password.", userId);
    }

    public async Task<SessionValidationResult> ValidateAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await db.Sessions
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == SessionTokens.Hash(sessionToken), cancellationToken);

        if (session is null || !session.IsActive || !session.User.IsActive)
        {
            return new SessionValidationResult(SessionValidationStatus.Revoked, null, null);
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return new SessionValidationResult(SessionValidationStatus.Expired, null, null);
        }

        session.LastActivityAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new SessionValidationResult(SessionValidationStatus.Valid, session.UserId, session.User.Username);
    }

    internal async Task RevokeSessionsAsync(Guid userId, string? keepTokenHash, CancellationToken cancellationToken)
    {
        var sessions = await db.Sessions.Where(session => session.UserId == userId && session.IsActive).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            if (keepTokenHash is null || !string.Equals(session.TokenHash, keepTokenHash, StringComparison.Ordinal))
            {
                session.IsActive = false;
            }
        }
    }

    internal static string? KeepToken(string? sessionToken) =>
        string.IsNullOrWhiteSpace(sessionToken) ? null : SessionTokens.Hash(sessionToken);

    private static IPAddress? ParseIp(string? ipAddress) =>
        IPAddress.TryParse(ipAddress, out var parsed) ? parsed : null;

    private static string? Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= 2000 ? value : value[..2000];

    private static class StatusCodes
    {
        public const int Unauthorized = 401;
        public const int NotFound = 404;
        public const int Conflict = 409;
    }
}
