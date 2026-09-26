using System.Net;

namespace LithographERP.Domain.Modules.Authentication;

public sealed class Session
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastActivityAt { get; set; }

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsActive { get; set; }

    public User User { get; set; } = null!;
}
