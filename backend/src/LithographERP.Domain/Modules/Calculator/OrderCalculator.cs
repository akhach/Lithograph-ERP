namespace LithographERP.Domain.Modules.Calculator;

public sealed class OrderCalculator
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid TemplateVersionId { get; set; }

    public string FieldValues { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? LastCalculatedAt { get; set; }
}
