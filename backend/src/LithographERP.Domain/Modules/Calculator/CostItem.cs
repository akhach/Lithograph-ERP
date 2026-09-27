namespace LithographERP.Domain.Modules.Calculator;

public sealed class CostItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? Supplier { get; set; }

    public DateOnly? ExpenseDate { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
