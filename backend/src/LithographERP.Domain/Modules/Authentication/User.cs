namespace LithographERP.Domain.Modules.Authentication;

public sealed class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();

    public ICollection<Session> Sessions { get; } = new List<Session>();
}
