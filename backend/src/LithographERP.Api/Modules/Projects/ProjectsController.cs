using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Projects;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Projects;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController(IProjectAdminService projects) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Projects.View)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "owner_employee_id")] Guid? ownerEmployeeId,
        [FromQuery(Name = "assignee_employee_id")] Guid? assigneeEmployeeId,
        [FromQuery(Name = "start_from")] string? startFrom,
        [FromQuery(Name = "start_to")] string? startTo,
        [FromQuery(Name = "deadline_from")] string? deadlineFrom,
        [FromQuery(Name = "deadline_to")] string? deadlineTo,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        [FromQuery] string? view,
        CancellationToken cancellationToken)
    {
        var request = ProjectAdminService.Normalize(
            search,
            status,
            clientId,
            ownerEmployeeId,
            assigneeEmployeeId,
            startFrom,
            startTo,
            deadlineFrom,
            deadlineTo,
            page,
            pageSize,
            sort,
            direction,
            view);
        if (request.Selector)
        {
            return Ok(await projects.ListSelectorAsync(request, cancellationToken));
        }

        return Ok(await projects.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.View)]
    public async Task<ProjectDetail> Get(Guid id, CancellationToken cancellationToken) =>
        await projects.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Projects.Create)]
    public async Task<ActionResult<ProjectDetail>> Create([FromBody] SaveProjectBody request, CancellationToken cancellationToken)
    {
        var created = await projects.CreateAsync(CurrentUserId.Require(User), ToRequest(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.Edit)]
    public async Task<ProjectDetail> Update(Guid id, [FromBody] SaveProjectBody request, CancellationToken cancellationToken) =>
        await projects.UpdateAsync(CurrentUserId.Require(User), id, ToRequest(request), cancellationToken);

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = PermissionCatalog.Projects.ChangeStatus)]
    public async Task<ProjectDetail> ChangeStatus(Guid id, [FromBody] ChangeProjectStatusBody request, CancellationToken cancellationToken) =>
        await projects.ChangeStatusAsync(CurrentUserId.Require(User), id, request.Status, cancellationToken);

    [HttpPut("{id:guid}/owner")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> SetOwner(Guid id, [FromBody] AssignEmployeeBody request, CancellationToken cancellationToken) =>
        await projects.SetOwnerAsync(CurrentUserId.Require(User), id, request.EmployeeId, cancellationToken);

    [HttpDelete("{id:guid}/owner")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> ClearOwner(Guid id, CancellationToken cancellationToken) =>
        await projects.ClearOwnerAsync(id, cancellationToken);

    [HttpPut("{id:guid}/assignee")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> SetAssignee(Guid id, [FromBody] AssignEmployeeBody request, CancellationToken cancellationToken) =>
        await projects.SetAssigneeAsync(CurrentUserId.Require(User), id, request.EmployeeId, cancellationToken);

    [HttpDelete("{id:guid}/assignee")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> ClearAssignee(Guid id, CancellationToken cancellationToken) =>
        await projects.ClearAssigneeAsync(id, cancellationToken);

    [HttpPost("{id:guid}/participants/{employeeId:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> AddParticipant(Guid id, Guid employeeId, CancellationToken cancellationToken) =>
        await projects.AddParticipantAsync(CurrentUserId.Require(User), id, employeeId, cancellationToken);

    [HttpDelete("{id:guid}/participants/{employeeId:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> RemoveParticipant(Guid id, Guid employeeId, CancellationToken cancellationToken) =>
        await projects.RemoveParticipantAsync(id, employeeId, cancellationToken);

    [HttpPost("{id:guid}/observers/{employeeId:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> AddObserver(Guid id, Guid employeeId, CancellationToken cancellationToken) =>
        await projects.AddObserverAsync(CurrentUserId.Require(User), id, employeeId, cancellationToken);

    [HttpDelete("{id:guid}/observers/{employeeId:guid}")]
    [Authorize(Policy = PermissionCatalog.Projects.ManageTeam)]
    public async Task<ProjectDetail> RemoveObserver(Guid id, Guid employeeId, CancellationToken cancellationToken) =>
        await projects.RemoveObserverAsync(id, employeeId, cancellationToken);

    private static SaveProjectRequest ToRequest(SaveProjectBody request) =>
        new(request.ClientId, request.Name, request.Description, request.StartDate, request.Deadline);
}

public sealed record SaveProjectBody(
    Guid ClientId,
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? Deadline);

public sealed record ChangeProjectStatusBody(string Status);

public sealed record AssignEmployeeBody(Guid EmployeeId);
