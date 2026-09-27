namespace LithographERP.Application.Modules.Projects;

public static class ProjectErrorCodes
{
    public const string ProjectNotFound = "PROJECT_NOT_FOUND";
    public const string ProjectCompleted = "PROJECT_COMPLETED";
    public const string ProjectCancelled = "PROJECT_CANCELLED";
    public const string ProjectClosed = "PROJECT_CLOSED";
    public const string OwnerConflict = "PROJECT_OWNER_CONFLICT";
    public const string AssigneeConflict = "PROJECT_ASSIGNEE_CONFLICT";
    public const string MemberAlreadyExists = "PROJECT_MEMBER_ALREADY_EXISTS";
    public const string MemberNotFound = "PROJECT_MEMBER_NOT_FOUND";
    public const string InvalidStatus = "PROJECT_INVALID_STATUS";
    public const string BusinessIdConflict = "PROJECT_BUSINESS_ID_CONFLICT";
}

public sealed record ClientSummary(Guid Id, string BusinessId, string Name, bool IsActive);

public sealed record EmployeeSummary(Guid Id, string FullName, string? Position, bool IsActive);

public sealed record ProjectListItem(
    Guid Id,
    string BusinessId,
    string Name,
    ClientSummary Client,
    string Status,
    DateOnly? StartDate,
    DateOnly? Deadline,
    EmployeeSummary? Owner,
    EmployeeSummary? Assignee);

public sealed record ProjectSelectorItem(Guid Id, string BusinessId, string Name, string ClientName, string Status);

public sealed record ProjectTeam(
    EmployeeSummary? Owner,
    EmployeeSummary? Assignee,
    IReadOnlyList<EmployeeSummary> Participants,
    IReadOnlyList<EmployeeSummary> Observers);

public sealed record ProjectDetail(
    Guid Id,
    string BusinessId,
    ClientSummary Client,
    string Name,
    string? Description,
    string Status,
    DateOnly? StartDate,
    DateOnly? Deadline,
    ProjectTeam Team,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ProjectPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record ProjectListRequest(
    string? Search,
    string? Status,
    Guid? ClientId,
    Guid? OwnerEmployeeId,
    Guid? AssigneeEmployeeId,
    DateOnly? StartFrom,
    DateOnly? StartTo,
    DateOnly? DeadlineFrom,
    DateOnly? DeadlineTo,
    int Page,
    int PageSize,
    string Sort,
    bool Descending,
    bool Selector);

public sealed record SaveProjectRequest(
    Guid ClientId,
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? Deadline);

public interface IProjectAdminService
{
    Task<ProjectPage<ProjectListItem>> ListAsync(ProjectListRequest request, CancellationToken cancellationToken = default);

    Task<ProjectPage<ProjectSelectorItem>> ListSelectorAsync(ProjectListRequest request, CancellationToken cancellationToken = default);

    Task<ProjectDetail> GetAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> CreateAsync(Guid actorId, SaveProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectDetail> UpdateAsync(Guid actorId, Guid projectId, SaveProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectDetail> ChangeStatusAsync(Guid actorId, Guid projectId, string status, CancellationToken cancellationToken = default);

    Task<ProjectDetail> SetOwnerAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> ClearOwnerAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> SetAssigneeAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> ClearAssigneeAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> AddParticipantAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> RemoveParticipantAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> AddObserverAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<ProjectDetail> RemoveObserverAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default);
}
