using LithographERP.Domain.Modules.Projects;

namespace LithographERP.Domain.Modules.Orders;

public sealed class Order
{
    public Guid Id { get; set; }

    public string BusinessId { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }

    public Guid OrderTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = OrderStatuses.Draft;

    public string Priority { get; set; } = OrderPriorities.Normal;

    public decimal SellingPrice { get; set; }

    public decimal CostPrice { get; set; }

    public DateOnly? Deadline { get; set; }

    public string? PreviewImagePath { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Project Project { get; set; } = null!;

    public OrderType OrderType { get; set; } = null!;

    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    public ICollection<FolderLink> FolderLinks { get; set; } = new List<FolderLink>();
}
