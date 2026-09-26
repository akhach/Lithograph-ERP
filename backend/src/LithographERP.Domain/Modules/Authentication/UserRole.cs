namespace LithographERP.Domain.Modules.Authentication;

public sealed class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public Guid? AssignedBy { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
