using LithographERP.Domain.Modules.Authentication;

namespace LithographERP.Domain.Modules.Employees;

public sealed class Employee
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Position { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public User? User { get; set; }
}
