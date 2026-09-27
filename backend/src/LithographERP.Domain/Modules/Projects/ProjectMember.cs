using LithographERP.Domain.Modules.Employees;

namespace LithographERP.Domain.Modules.Projects;

public sealed class ProjectMember
{
    public Guid ProjectId { get; set; }

    public Guid EmployeeId { get; set; }

    public string ProjectRole { get; set; } = string.Empty;

    public DateTimeOffset AssignedAt { get; set; }

    public Guid? AssignedBy { get; set; }

    public Project Project { get; set; } = null!;

    public Employee Employee { get; set; } = null!;
}
