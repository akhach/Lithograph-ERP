using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Clients;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Clients;

[ApiController]
[Route("api/clients")]
[Authorize]
public sealed class ClientsController(IClientAdminService clients) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Clients.View)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery(Name = "is_active")] bool? isActive,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        [FromQuery] string? view,
        CancellationToken cancellationToken)
    {
        var request = ClientAdminService.Normalize(search, isActive, page, pageSize, sort, direction, view);
        if (request.Selector)
        {
            return Ok(await clients.ListSelectorAsync(request, cancellationToken));
        }

        return Ok(await clients.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Clients.View)]
    public async Task<ClientDetail> Get(Guid id, CancellationToken cancellationToken) =>
        await clients.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Clients.Create)]
    public async Task<ActionResult<ClientDetail>> Create([FromBody] SaveClientBody request, CancellationToken cancellationToken)
    {
        var created = await clients.CreateAsync(CurrentUserId.Require(User), ToRequest(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Clients.Edit)]
    public async Task<ClientDetail> Update(Guid id, [FromBody] SaveClientBody request, CancellationToken cancellationToken) =>
        await clients.UpdateAsync(CurrentUserId.Require(User), id, ToRequest(request), cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCatalog.Clients.Activate)]
    public async Task<ClientDetail> Activate(Guid id, CancellationToken cancellationToken) =>
        await clients.ActivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCatalog.Clients.Activate)]
    public async Task<ClientDetail> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await clients.DeactivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    private static SaveClientRequest ToRequest(SaveClientBody request) =>
        new(request.Name, request.ContactPerson, request.Phone, request.Email, request.Address, request.Notes);
}

public sealed record SaveClientBody(
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);
