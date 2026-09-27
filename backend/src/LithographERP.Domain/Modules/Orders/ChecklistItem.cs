namespace LithographERP.Domain.Modules.Orders;

public sealed class ChecklistItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public int SortOrder { get; set; }

    public Order Order { get; set; } = null!;
}
