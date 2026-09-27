namespace LithographERP.Domain.Modules.Orders;

public sealed class OrderType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // calculator_template_id is added by the Calculator migration once calculator.templates exists.
    // This phase does not create that column or a foreign key, and does not create Calculator tables.

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
