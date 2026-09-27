using System.Text.Json;
using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Calculator;

[ApiController]
[Route("api/orders/{orderId:guid}/calculator")]
[Authorize]
public sealed class OrderCalculatorsController(IOrderCalculatorService calculators, IAuthService auth) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Calculator.View)]
    public async Task<OrderCalculatorRuntime> Get(Guid orderId, CancellationToken cancellationToken) =>
        await calculators.GetAsync(CurrentUserId.Require(User), orderId, await AccessAsync(cancellationToken), cancellationToken);

    [HttpPatch]
    [Authorize(Policy = PermissionCatalog.Calculator.Edit)]
    public async Task<OrderCalculatorRuntime> Save(
        Guid orderId,
        [FromBody] SaveOrderCalculatorBody request,
        CancellationToken cancellationToken)
    {
        if (request.UpdatedAt is null)
        {
            throw AuthException.Validation("updatedAt", "A value is required.");
        }

        return await calculators.SaveAsync(
            CurrentUserId.Require(User),
            orderId,
            new SaveOrderCalculatorRequest(request.FieldValues, request.UpdatedAt.Value),
            await AccessAsync(cancellationToken),
            cancellationToken);
    }

    [HttpPost("reset")]
    [Authorize(Policy = PermissionCatalog.Calculator.Edit)]
    public async Task<OrderCalculatorRuntime> Reset(
        Guid orderId,
        [FromBody] ResetOrderCalculatorBody request,
        CancellationToken cancellationToken)
    {
        if (request.UpdatedAt is null)
        {
            throw AuthException.Validation("updatedAt", "A value is required.");
        }

        return await calculators.ResetAsync(
            CurrentUserId.Require(User),
            orderId,
            new ResetOrderCalculatorRequest(request.UpdatedAt.Value),
            await AccessAsync(cancellationToken),
            cancellationToken);
    }

    private async Task<OrderCalculatorAccess> AccessAsync(CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        return new OrderCalculatorAccess(
            user.Permissions.Contains(PermissionCatalog.Orders.ViewSellingPrice, StringComparer.Ordinal),
            user.Permissions.Contains(PermissionCatalog.Calculator.ViewCosts, StringComparer.Ordinal));
    }
}

public sealed class SaveOrderCalculatorBody
{
    public JsonElement FieldValues { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ResetOrderCalculatorBody
{
    public DateTimeOffset? UpdatedAt { get; set; }
}
