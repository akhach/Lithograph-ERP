namespace LithographERP.Domain.Modules.Orders;

public sealed class FolderLink
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? Name { get; set; }

    public string Path { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Order Order { get; set; } = null!;
}
