using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Orders;
using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Orders;

[ApiController]
[Route("api/order-types")]
[Authorize]
public sealed class OrderTypesController(IOrderTypeAdminService orderTypes, IAuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<OrderTypeResponse>> List([FromQuery] string? view, CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        var selector = string.Equals(view, "selector", StringComparison.OrdinalIgnoreCase);
        if (selector)
        {
            RequireAny(
                user,
                PermissionCatalog.Orders.View,
                PermissionCatalog.Orders.Create,
                PermissionCatalog.Orders.Edit,
                PermissionCatalog.Orders.ManageTypes);
        }
        else
        {
            RequireAny(user, PermissionCatalog.Orders.View, PermissionCatalog.Orders.ManageTypes);
        }

        return await orderTypes.ListAsync(selector, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageTypes)]
    public async Task<OrderTypeResponse> Get(Guid id, CancellationToken cancellationToken) =>
        await orderTypes.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Orders.ManageTypes)]
    public async Task<ActionResult<OrderTypeResponse>> Create([FromBody] SaveOrderTypeBody request, CancellationToken cancellationToken)
    {
        var created = await orderTypes.CreateAsync(
            CurrentUserId.Require(User),
            new SaveOrderTypeRequest(request.Name, request.Description, request.CalculatorTemplateId),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageTypes)]
    public async Task<OrderTypeResponse> Update(Guid id, [FromBody] SaveOrderTypeBody request, CancellationToken cancellationToken) =>
        await orderTypes.UpdateAsync(
            CurrentUserId.Require(User),
            id,
            new SaveOrderTypeRequest(request.Name, request.Description, request.CalculatorTemplateId),
            cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageTypes)]
    public async Task<OrderTypeResponse> Activate(Guid id, CancellationToken cancellationToken) =>
        await orderTypes.SetActiveAsync(CurrentUserId.Require(User), id, true, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageTypes)]
    public async Task<OrderTypeResponse> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await orderTypes.SetActiveAsync(CurrentUserId.Require(User), id, false, cancellationToken);

    private static void RequireAny(CurrentUserResponse user, params string[] permissions)
    {
        if (!permissions.Any(permission => user.Permissions.Contains(permission, StringComparer.Ordinal)))
        {
            throw new AuthException(
                AuthErrorCodes.PermissionDenied,
                "You do not have permission to perform this action.",
                StatusCodes.Status403Forbidden);
        }
    }
}

public sealed record SaveOrderTypeBody(string Name, string? Description, Guid? CalculatorTemplateId);
