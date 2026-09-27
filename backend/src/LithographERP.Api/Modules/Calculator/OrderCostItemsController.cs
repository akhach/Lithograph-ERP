using System.Text.Json;
using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Calculator;

[ApiController]
[Route("api/orders/{orderId:guid}/cost-items")]
[Authorize]
public sealed class OrderCostItemsController(IOrderCostService costs) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Calculator.ViewCosts)]
    public async Task<CostItemListResponse> List(Guid orderId, CancellationToken cancellationToken) =>
        await costs.ListAsync(orderId, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Calculator.EditCosts)]
    public async Task<ActionResult<CostItemResponse>> Create(
        Guid orderId,
        [FromBody] SaveCostItemBody request,
        CancellationToken cancellationToken)
    {
        var created = await costs.CreateAsync(
            CurrentUserId.Require(User),
            orderId,
            new SaveCostItemRequest(
                request.Category,
                request.Supplier,
                request.ExpenseDate,
                request.Description,
                ReadAmount(request.Amount, out var supplied),
                supplied,
                request.SortOrder),
            cancellationToken);
        return Created($"/api/orders/{orderId}/cost-items/{created.Id}", created);
    }

    [HttpPatch("{costItemId:guid}")]
    [Authorize(Policy = PermissionCatalog.Calculator.EditCosts)]
    public async Task<CostItemResponse> Update(
        Guid orderId,
        Guid costItemId,
        [FromBody] UpdateCostItemBody request,
        CancellationToken cancellationToken) =>
        await costs.UpdateAsync(
            CurrentUserId.Require(User),
            orderId,
            costItemId,
            new UpdateCostItemRequest(
                request.Category,
                request.Supplier,
                request.ExpenseDate,
                request.Description,
                ReadAmount(request.Amount, out var supplied),
                supplied),
            cancellationToken);

    [HttpDelete("{costItemId:guid}")]
    [Authorize(Policy = PermissionCatalog.Calculator.EditCosts)]
    public async Task<IActionResult> Delete(Guid orderId, Guid costItemId, CancellationToken cancellationToken)
    {
        await costs.DeleteAsync(CurrentUserId.Require(User), orderId, costItemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("reorder")]
    [Authorize(Policy = PermissionCatalog.Calculator.EditCosts)]
    public async Task<CostItemListResponse> Reorder(
        Guid orderId,
        [FromBody] ReorderCostItemsBody request,
        CancellationToken cancellationToken) =>
        await costs.ReorderAsync(CurrentUserId.Require(User), orderId, request.Ids, cancellationToken);

    private static string? ReadAmount(JsonElement amount, out bool supplied)
    {
        supplied = amount.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null);
        return amount.ValueKind switch
        {
            JsonValueKind.Number => amount.GetRawText(),
            JsonValueKind.String => amount.GetString(),
            _ => null,
        };
    }
}

public sealed class SaveCostItemBody
{
    public string? Category { get; set; }

    public string? Supplier { get; set; }

    public DateOnly? ExpenseDate { get; set; }

    public string? Description { get; set; }

    public JsonElement Amount { get; set; }

    public int? SortOrder { get; set; }
}

public sealed class UpdateCostItemBody
{
    public string? Category { get; set; }

    public string? Supplier { get; set; }

    public DateOnly? ExpenseDate { get; set; }

    public string? Description { get; set; }

    public JsonElement Amount { get; set; }
}

public sealed class ReorderCostItemsBody
{
    public IReadOnlyList<Guid>? Ids { get; set; }
}
