using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Clients;
using LithographERP.Application.Modules.Employees;
using LithographERP.Application.Modules.Numbering;
using LithographERP.Application.Modules.Projects;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Projects;

public sealed class ProjectAdminService(
    LithographDbContext db,
    IBusinessIdGenerator businessIds,
    TimeProvider time,
    ILogger<ProjectAdminService> logger) : IProjectAdminService
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 200;

    public async Task<ProjectPage<ProjectListItem>> ListAsync(ProjectListRequest request, CancellationToken cancellationToken = default)
    {
        var query = Filtered(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await Ordered(query, request)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(project => new ProjectListItem(
                project.Id,
                project.BusinessId,
                project.Name,
                new ClientSummary(project.Client.Id, project.Client.BusinessId, project.Client.Name, project.Client.IsActive),
                project.Status,
                project.StartDate,
                project.Deadline,
                project.Members.Where(member => member.ProjectRole == ProjectRoles.Owner)
                    .Select(member => new EmployeeSummary(member.Employee.Id, member.Employee.FullName, member.Employee.Position, member.Employee.IsActive))
                    .FirstOrDefault(),
                project.Members.Where(member => member.ProjectRole == ProjectRoles.Assignee)
                    .Select(member => new EmployeeSummary(member.Employee.Id, member.Employee.FullName, member.Employee.Position, member.Employee.IsActive))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return Page(items, request, total);
    }

    public async Task<ProjectPage<ProjectSelectorItem>> ListSelectorAsync(ProjectListRequest request, CancellationToken cancellationToken = default)
    {
        var query = Filtered(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await Ordered(query, request)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(project => new ProjectSelectorItem(
                project.Id,
                project.BusinessId,
                project.Name,
                project.Client.Name,
                project.Status))
            .ToListAsync(cancellationToken);
        return Page(items, request, total);
    }

    public Task<ProjectDetail> GetAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        DetailAsync(projectId, cancellationToken);

    public async Task<ProjectDetail> CreateAsync(Guid actorId, SaveProjectRequest request, CancellationToken cancellationToken = default)
    {
        var name = RequireName(request.Name);
        var description = Text(request.Description);
        RequireDates(request.StartDate, request.Deadline);
        var client = await RequireClientAsync(request.ClientId, mustBeActive: true, cancellationToken);
        var now = time.GetUtcNow();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            BusinessId = await businessIds.GenerateProjectBusinessIdAsync(cancellationToken),
            ClientId = client.Id,
            Name = name,
            Description = description,
            Status = ProjectStatuses.Draft,
            StartDate = request.StartDate,
            Deadline = request.Deadline,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        db.Projects.Add(project);
        await SaveAsync(cancellationToken);
        return await DetailAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDetail> UpdateAsync(Guid actorId, Guid projectId, SaveProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await LoadTrackedAsync(projectId, cancellationToken);
        var name = RequireName(request.Name);
        var description = Text(request.Description);
        RequireDates(request.StartDate, request.Deadline);
        if (request.ClientId != project.ClientId)
        {
            var client = await RequireClientAsync(request.ClientId, mustBeActive: true, cancellationToken);
            project.ClientId = client.Id;
        }

        project.Name = name;
        project.Description = description;
        project.StartDate = request.StartDate;
        project.Deadline = request.Deadline;
        Touch(project, actorId);
        await SaveAsync(cancellationToken);
        return await DetailAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDetail> ChangeStatusAsync(Guid actorId, Guid projectId, string status, CancellationToken cancellationToken = default)
    {
        if (!ProjectStatuses.TryParse(status, out var parsed))
        {
            throw new AuthException(ProjectErrorCodes.InvalidStatus, "The Project status is not valid.", 400);
        }

        var project = await LoadTrackedAsync(projectId, cancellationToken);
        if (project.Status != parsed)
        {
            project.Status = parsed;
            Touch(project, actorId);
            await SaveAsync(cancellationToken);
        }

        return await DetailAsync(project.Id, cancellationToken);
    }

    public Task<ProjectDetail> SetOwnerAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        SetSingularAsync(actorId, projectId, employeeId, ProjectRoles.Owner, cancellationToken);

    public Task<ProjectDetail> ClearOwnerAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        ClearSingularAsync(projectId, ProjectRoles.Owner, cancellationToken);

    public Task<ProjectDetail> SetAssigneeAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        SetSingularAsync(actorId, projectId, employeeId, ProjectRoles.Assignee, cancellationToken);

    public Task<ProjectDetail> ClearAssigneeAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        ClearSingularAsync(projectId, ProjectRoles.Assignee, cancellationToken);

    public Task<ProjectDetail> AddParticipantAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        AddMemberAsync(actorId, projectId, employeeId, ProjectRoles.Participant, cancellationToken);

    public Task<ProjectDetail> RemoveParticipantAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        RemoveMemberAsync(projectId, employeeId, ProjectRoles.Participant, cancellationToken);

    public Task<ProjectDetail> AddObserverAsync(Guid actorId, Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        AddMemberAsync(actorId, projectId, employeeId, ProjectRoles.Observer, cancellationToken);

    public Task<ProjectDetail> RemoveObserverAsync(Guid projectId, Guid employeeId, CancellationToken cancellationToken = default) =>
        RemoveMemberAsync(projectId, employeeId, ProjectRoles.Observer, cancellationToken);

    private async Task<ProjectDetail> SetSingularAsync(
        Guid actorId,
        Guid projectId,
        Guid employeeId,
        string role,
        CancellationToken cancellationToken)
    {
        await EnsureExistsAsync(projectId, cancellationToken);
        await RequireActiveEmployeeAsync(employeeId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await db.ProjectMembers
            .Where(member => member.ProjectId == projectId && member.ProjectRole == role)
            .ToListAsync(cancellationToken);
        if (current.Count == 1 && current[0].EmployeeId == employeeId)
        {
            await transaction.CommitAsync(cancellationToken);
            return await DetailAsync(projectId, cancellationToken);
        }

        db.ProjectMembers.RemoveRange(current);
        db.ProjectMembers.Add(NewMember(actorId, projectId, employeeId, role));
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await DetailAsync(projectId, cancellationToken);
    }

    private async Task<ProjectDetail> ClearSingularAsync(Guid projectId, string role, CancellationToken cancellationToken)
    {
        await EnsureExistsAsync(projectId, cancellationToken);
        var current = await db.ProjectMembers
            .Where(member => member.ProjectId == projectId && member.ProjectRole == role)
            .ToListAsync(cancellationToken);
        if (current.Count == 0)
        {
            throw new AuthException(ProjectErrorCodes.MemberNotFound, "The Project team assignment could not be found.", 404);
        }

        db.ProjectMembers.RemoveRange(current);
        await SaveAsync(cancellationToken);
        return await DetailAsync(projectId, cancellationToken);
    }

    private async Task<ProjectDetail> AddMemberAsync(
        Guid actorId,
        Guid projectId,
        Guid employeeId,
        string role,
        CancellationToken cancellationToken)
    {
        await EnsureExistsAsync(projectId, cancellationToken);
        await RequireActiveEmployeeAsync(employeeId, cancellationToken);
        var exists = await db.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId && member.EmployeeId == employeeId && member.ProjectRole == role,
            cancellationToken);
        if (exists)
        {
            throw new AuthException(
                ProjectErrorCodes.MemberAlreadyExists,
                "This Employee already has that Project role.",
                409);
        }

        db.ProjectMembers.Add(NewMember(actorId, projectId, employeeId, role));
        await SaveAsync(cancellationToken);
        return await DetailAsync(projectId, cancellationToken);
    }

    private async Task<ProjectDetail> RemoveMemberAsync(Guid projectId, Guid employeeId, string role, CancellationToken cancellationToken)
    {
        await EnsureExistsAsync(projectId, cancellationToken);
        var member = await db.ProjectMembers.SingleOrDefaultAsync(
            candidate => candidate.ProjectId == projectId && candidate.EmployeeId == employeeId && candidate.ProjectRole == role,
            cancellationToken);
        if (member is null)
        {
            throw new AuthException(ProjectErrorCodes.MemberNotFound, "The Project team assignment could not be found.", 404);
        }

        db.ProjectMembers.Remove(member);
        await SaveAsync(cancellationToken);
        return await DetailAsync(projectId, cancellationToken);
    }

    private ProjectMember NewMember(Guid actorId, Guid projectId, Guid employeeId, string role) =>
        new()
        {
            ProjectId = projectId,
            EmployeeId = employeeId,
            ProjectRole = role,
            AssignedAt = time.GetUtcNow(),
            AssignedBy = actorId,
        };

    private IQueryable<Project> Filtered(ProjectListRequest request)
    {
        var projects = db.Projects.AsQueryable();
        if (request.Status == "open")
        {
            projects = projects.Where(project =>
                project.Status == ProjectStatuses.Draft
                || project.Status == ProjectStatuses.Active
                || project.Status == ProjectStatuses.OnHold);
        }
        else if (request.Status is not null)
        {
            projects = projects.Where(project => project.Status == request.Status);
        }
        else if (request.Selector)
        {
            projects = projects.Where(project =>
                project.Status == ProjectStatuses.Draft
                || project.Status == ProjectStatuses.Active
                || project.Status == ProjectStatuses.OnHold);
        }

        if (request.ClientId is not null)
        {
            projects = projects.Where(project => project.ClientId == request.ClientId);
        }

        if (request.OwnerEmployeeId is not null)
        {
            projects = projects.Where(project => project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Owner && member.EmployeeId == request.OwnerEmployeeId));
        }

        if (request.AssigneeEmployeeId is not null)
        {
            projects = projects.Where(project => project.Members.Any(member =>
                member.ProjectRole == ProjectRoles.Assignee && member.EmployeeId == request.AssigneeEmployeeId));
        }

        if (request.StartFrom is not null)
        {
            projects = projects.Where(project => project.StartDate != null && project.StartDate >= request.StartFrom);
        }

        if (request.StartTo is not null)
        {
            projects = projects.Where(project => project.StartDate != null && project.StartDate <= request.StartTo);
        }

        if (request.DeadlineFrom is not null)
        {
            projects = projects.Where(project => project.Deadline != null && project.Deadline >= request.DeadlineFrom);
        }

        if (request.DeadlineTo is not null)
        {
            projects = projects.Where(project => project.Deadline != null && project.Deadline <= request.DeadlineTo);
        }

        if (request.Search is not null)
        {
            var pattern = LikePattern(request.Search);
            projects = projects.Where(project =>
                EF.Functions.ILike(project.BusinessId, pattern, "\\")
                || EF.Functions.ILike(project.Name, pattern, "\\")
                || EF.Functions.ILike(project.Client.BusinessId, pattern, "\\")
                || EF.Functions.ILike(project.Client.Name, pattern, "\\"));
        }

        return projects;
    }

    private static IQueryable<Project> Ordered(IQueryable<Project> projects, ProjectListRequest request) =>
        request.Sort switch
        {
            "business_id" => request.Descending
                ? projects.OrderByDescending(project => project.BusinessId).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.BusinessId).ThenBy(project => project.Id),
            "name" => request.Descending
                ? projects.OrderByDescending(project => project.Name).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.Name).ThenBy(project => project.Id),
            "status" => request.Descending
                ? projects.OrderByDescending(project => project.Status).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.Status).ThenBy(project => project.Id),
            "start_date" => request.Descending
                ? projects.OrderByDescending(project => project.StartDate).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.StartDate).ThenBy(project => project.Id),
            "deadline" => request.Descending
                ? projects.OrderByDescending(project => project.Deadline).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.Deadline).ThenBy(project => project.Id),
            "created_at" => request.Descending
                ? projects.OrderByDescending(project => project.CreatedAt).ThenBy(project => project.Id)
                : projects.OrderBy(project => project.CreatedAt).ThenBy(project => project.Id),
            _ => throw AuthException.Validation("sort", "Sort must be business_id, name, status, start_date, deadline, or created_at."),
        };

    private async Task<Domain.Modules.Clients.Client> RequireClientAsync(Guid clientId, bool mustBeActive, CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new AuthException(ClientErrorCodes.ClientNotFound, "The Client could not be found.", 404);
        }

        if (mustBeActive && !client.IsActive)
        {
            throw new AuthException(ClientErrorCodes.ClientInactive, "The selected Client is inactive.", 400);
        }

        return client;
    }

    private async Task RequireActiveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            throw new AuthException(EmployeeErrorCodes.EmployeeNotFound, "The Employee could not be found.", 404);
        }

        if (!employee.IsActive)
        {
            throw new AuthException(
                EmployeeErrorCodes.EmployeeInactive,
                "Inactive Employees cannot be assigned to new Project roles.",
                400);
        }
    }

    private async Task EnsureExistsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new AuthException(ProjectErrorCodes.ProjectNotFound, "The Project could not be found.", 404);
        }
    }

    private async Task<Project> LoadTrackedAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(candidate => candidate.Id == projectId, cancellationToken);
        if (project is null)
        {
            throw new AuthException(ProjectErrorCodes.ProjectNotFound, "The Project could not be found.", 404);
        }

        return project;
    }

    private async Task<ProjectDetail> DetailAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db.Projects.AsNoTracking()
            .Include(candidate => candidate.Client)
            .Include(candidate => candidate.Members)
            .ThenInclude(member => member.Employee)
            .SingleOrDefaultAsync(candidate => candidate.Id == projectId, cancellationToken);
        if (project is null)
        {
            throw new AuthException(ProjectErrorCodes.ProjectNotFound, "The Project could not be found.", 404);
        }

        return ToDetail(project);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var constraint = postgres.ConstraintName ?? string.Empty;
            if (constraint.Contains("business_id", StringComparison.Ordinal))
            {
                logger.LogWarning(exception, "Project Business ID conflict while saving a Project.");
                throw new AuthException(ProjectErrorCodes.BusinessIdConflict, "The Project ID could not be assigned. Try again.", 409);
            }

            if (constraint.Contains("one_owner", StringComparison.Ordinal))
            {
                throw new AuthException(ProjectErrorCodes.OwnerConflict, "The Project already has an Owner.", 409);
            }

            if (constraint.Contains("one_assignee", StringComparison.Ordinal))
            {
                throw new AuthException(ProjectErrorCodes.AssigneeConflict, "The Project already has an Assignee.", 409);
            }

            if (constraint.Contains("project_members", StringComparison.Ordinal))
            {
                throw new AuthException(
                    ProjectErrorCodes.MemberAlreadyExists,
                    "This Employee already has that Project role.",
                    409);
            }

            throw;
        }
    }

    private static ProjectDetail ToDetail(Project project)
    {
        EmployeeSummary Member(ProjectMember member) =>
            new(member.Employee.Id, member.Employee.FullName, member.Employee.Position, member.Employee.IsActive);

        EmployeeSummary? Singular(string role) =>
            project.Members.Where(member => member.ProjectRole == role).Select(Member).FirstOrDefault();

        IReadOnlyList<EmployeeSummary> Many(string role) =>
            project.Members.Where(member => member.ProjectRole == role)
                .OrderBy(member => member.Employee.FullName)
                .Select(Member)
                .ToArray();

        return new ProjectDetail(
            project.Id,
            project.BusinessId,
            new ClientSummary(project.Client.Id, project.Client.BusinessId, project.Client.Name, project.Client.IsActive),
            project.Name,
            project.Description,
            project.Status,
            project.StartDate,
            project.Deadline,
            new ProjectTeam(Singular(ProjectRoles.Owner), Singular(ProjectRoles.Assignee), Many(ProjectRoles.Participant), Many(ProjectRoles.Observer)),
            project.CreatedAt,
            project.UpdatedAt);
    }

    private void Touch(Project project, Guid actorId)
    {
        project.UpdatedAt = time.GetUtcNow();
        project.UpdatedBy = actorId;
    }

    private static ProjectPage<T> Page<T>(IReadOnlyList<T> items, ProjectListRequest request, int total)
    {
        var pageCount = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        return new ProjectPage<T>(items, request.Page, request.PageSize, total, pageCount);
    }

    private static string RequireName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("name", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 250)
        {
            throw AuthException.Validation("name", "Must be at most 250 characters.");
        }

        return trimmed;
    }

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void RequireDates(DateOnly? start, DateOnly? deadline)
    {
        if (!ProjectDates.IsValidRange(start, deadline))
        {
            throw AuthException.Validation("deadline", "Deadline cannot be before the start date.");
        }
    }

    private static string LikePattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    public static ProjectListRequest Normalize(
        string? search,
        string? status,
        Guid? clientId,
        Guid? ownerEmployeeId,
        Guid? assigneeEmployeeId,
        string? startFrom,
        string? startTo,
        string? deadlineFrom,
        string? deadlineTo,
        int? page,
        int? pageSize,
        string? sort,
        string? direction,
        string? view)
    {
        if (page is < 1)
        {
            throw AuthException.Validation("page", "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw AuthException.Validation("pageSize", $"Page size must be between 1 and {MaximumPageSize}.");
        }

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = status.Trim().ToLowerInvariant();
            if (normalizedStatus != "open" && !ProjectStatuses.TryParse(normalizedStatus, out normalizedStatus))
            {
                throw AuthException.Validation("status", "Status is not recognized.");
            }
        }

        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "created_at" : sort.Trim().ToLowerInvariant();
        var normalizedDirection = string.IsNullOrWhiteSpace(direction) ? "desc" : direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
        {
            throw AuthException.Validation("direction", "Direction must be asc or desc.");
        }

        var normalizedView = string.IsNullOrWhiteSpace(view) ? "list" : view.Trim().ToLowerInvariant();
        if (normalizedView is not ("list" or "selector"))
        {
            throw AuthException.Validation("view", "View must be list or selector.");
        }

        var parsedStartFrom = ParseDate(startFrom, "startFrom");
        var parsedStartTo = ParseDate(startTo, "startTo");
        var parsedDeadlineFrom = ParseDate(deadlineFrom, "deadlineFrom");
        var parsedDeadlineTo = ParseDate(deadlineTo, "deadlineTo");
        if (parsedStartFrom is not null && parsedStartTo is not null && parsedStartTo < parsedStartFrom)
        {
            throw AuthException.Validation("startTo", "The end of the range cannot be before the start.");
        }

        if (parsedDeadlineFrom is not null && parsedDeadlineTo is not null && parsedDeadlineTo < parsedDeadlineFrom)
        {
            throw AuthException.Validation("deadlineTo", "The end of the range cannot be before the start.");
        }

        return new ProjectListRequest(
            string.IsNullOrWhiteSpace(search) ? null : search,
            normalizedStatus,
            clientId,
            ownerEmployeeId,
            assigneeEmployeeId,
            parsedStartFrom,
            parsedStartTo,
            parsedDeadlineFrom,
            parsedDeadlineTo,
            page ?? 1,
            pageSize ?? DefaultPageSize,
            normalizedSort,
            normalizedDirection == "desc",
            normalizedView == "selector");
    }

    private static DateOnly? ParseDate(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            throw AuthException.Validation(field, "Enter a valid date.");
        }

        return date;
    }
}
