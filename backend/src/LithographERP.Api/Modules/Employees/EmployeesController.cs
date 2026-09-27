using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Employees;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Employees;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeAdminService employees, IAuthService auth) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Employees.View)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery(Name = "is_active")] bool? isActive,
        [FromQuery(Name = "has_user")] bool? hasUser,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        [FromQuery] string? view,
        CancellationToken cancellationToken)
    {
        var request = EmployeeAdminService.Normalize(search, isActive, hasUser, page, pageSize, sort, direction, view);
        if (request.Selector)
        {
            return Ok(await employees.ListSelectorAsync(request, cancellationToken));
        }

        return Ok(await employees.ListAsync(request, cancellationToken));
    }

    [HttpGet("available-users")]
    [Authorize(Policy = PermissionCatalog.Employees.LinkUser)]
    public async Task<IReadOnlyList<LinkedUserSummary>> AvailableUsers(CancellationToken cancellationToken) =>
        await employees.ListAvailableUsersAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Employees.View)]
    public async Task<EmployeeDetail> Get(Guid id, CancellationToken cancellationToken) =>
        await employees.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Employees.Create)]
    public async Task<ActionResult<EmployeeDetail>> Create([FromBody] CreateEmployeeBody request, CancellationToken cancellationToken)
    {
        var actorId = CurrentUserId.Require(User);
        var current = await auth.GetCurrentUserAsync(actorId, cancellationToken);
        var created = await employees.CreateAsync(
            actorId,
            new CreateEmployeeRequest(request.FullName, request.Position, request.Phone, request.Email, request.UserId),
            current.Permissions.Contains(PermissionCatalog.Employees.LinkUser, StringComparer.Ordinal),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Employees.Edit)]
    public async Task<EmployeeDetail> Update(Guid id, [FromBody] UpdateEmployeeBody request, CancellationToken cancellationToken) =>
        await employees.UpdateAsync(
            CurrentUserId.Require(User),
            id,
            new UpdateEmployeeRequest(request.FullName, request.Position, request.Phone, request.Email),
            cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCatalog.Employees.Activate)]
    public async Task<EmployeeDetail> Activate(Guid id, CancellationToken cancellationToken) =>
        await employees.ActivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCatalog.Employees.Activate)]
    public async Task<EmployeeDetail> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await employees.DeactivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    [HttpPost("{id:guid}/link-user")]
    [Authorize(Policy = PermissionCatalog.Employees.LinkUser)]
    public async Task<EmployeeDetail> LinkUser(Guid id, [FromBody] LinkUserBody request, CancellationToken cancellationToken) =>
        await employees.LinkUserAsync(CurrentUserId.Require(User), id, request.UserId, cancellationToken);

    [HttpDelete("{id:guid}/user-link")]
    [Authorize(Policy = PermissionCatalog.Employees.LinkUser)]
    public async Task<EmployeeDetail> UnlinkUser(Guid id, CancellationToken cancellationToken) =>
        await employees.UnlinkUserAsync(CurrentUserId.Require(User), id, cancellationToken);

    public sealed record CreateEmployeeBody(string FullName, string? Position, string? Phone, string? Email, Guid? UserId);

    public sealed record UpdateEmployeeBody(string FullName, string? Position, string? Phone, string? Email);

    public sealed record LinkUserBody(Guid UserId);
}
