using LithographERP.Domain.Modules.Clients;

namespace LithographERP.Domain.Modules.Projects;

public sealed class Project
{
    public Guid Id { get; set; }

    public string BusinessId { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = ProjectStatuses.Draft;

    public DateOnly? StartDate { get; set; }

    public DateOnly? Deadline { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Client Client { get; set; } = null!;

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
