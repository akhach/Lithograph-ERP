namespace LithographERP.Domain.Modules.Authentication;

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Module { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
