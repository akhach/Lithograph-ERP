namespace LithographERP.Domain.Modules.Calculator;

public sealed class CalculatorTemplateVersion
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public int VersionNumber { get; set; }

    public string Status { get; set; } = TemplateVersionStatuses.Draft;

    public CalculatorTemplateDefinition Definition { get; set; } = CalculatorTemplateDefinition.Empty();

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid? PublishedBy { get; set; }
}
