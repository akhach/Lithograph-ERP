using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Application.Modules.Orders;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Orders;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(IOrderAdminService orders, IOrderCalculatorService calculators, IAuthService auth) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Orders.View)]
    public async Task<OrderPage<OrderListItem>> List(
        [FromQuery] string? search,
        [FromQuery(Name = "project_id")] Guid? projectId,
        [FromQuery(Name = "client_id")] Guid? clientId,
        [FromQuery(Name = "order_type_id")] Guid? orderTypeId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery(Name = "owner_employee_id")] Guid? ownerEmployeeId,
        [FromQuery(Name = "assignee_employee_id")] Guid? assigneeEmployeeId,
        [FromQuery(Name = "deadline_from")] string? deadlineFrom,
        [FromQuery(Name = "deadline_to")] string? deadlineTo,
        [FromQuery] int? page,
        [FromQuery(Name = "page_size")] int? pageSize,
        [FromQuery] string? sort,
        [FromQuery] string? direction,
        CancellationToken cancellationToken)
    {
        var request = OrderAdminService.Normalize(
            search,
            projectId,
            clientId,
            orderTypeId,
            status,
            priority,
            ownerEmployeeId,
            assigneeEmployeeId,
            deadlineFrom,
            deadlineTo,
            page,
            pageSize,
            sort,
            direction);
        return await orders.ListAsync(request, await AccessAsync(cancellationToken), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.View)]
    public async Task<OrderDetail> Get(Guid id, CancellationToken cancellationToken) =>
        await orders.GetAsync(id, await AccessAsync(cancellationToken), cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Orders.Create)]
    public async Task<ActionResult<OrderDetail>> Create([FromBody] SaveOrderBody request, CancellationToken cancellationToken)
    {
        var created = await orders.CreateAsync(CurrentUserId.Require(User), ToRequest(request), await AccessAsync(cancellationToken), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.Edit)]
    public async Task<OrderDetail> Update(Guid id, [FromBody] SaveOrderBody request, CancellationToken cancellationToken) =>
        await orders.UpdateAsync(CurrentUserId.Require(User), id, ToRequest(request), await AccessAsync(cancellationToken), cancellationToken);

    [HttpPost("{orderId:guid}/change-order-type")]
    [Authorize(Policy = PermissionCatalog.Orders.Edit)]
    public async Task<OrderDetail> ChangeOrderType(
        Guid orderId,
        [FromBody] ChangeOrderTypeBody request,
        CancellationToken cancellationToken)
    {
        await RequirePermissionAsync(PermissionCatalog.Calculator.Edit, cancellationToken);
        await calculators.ChangeOrderTypeAsync(
            CurrentUserId.Require(User),
            orderId,
            new ChangeOrderTypeRequest(request.OrderTypeId, request.ResetCalculator, request.UpdatedAt),
            cancellationToken);
        return await orders.GetAsync(orderId, await AccessAsync(cancellationToken), cancellationToken);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = PermissionCatalog.Orders.ChangeStatus)]
    public async Task<OrderDetail> ChangeStatus(Guid id, [FromBody] ChangeOrderStatusBody request, CancellationToken cancellationToken) =>
        await orders.ChangeStatusAsync(CurrentUserId.Require(User), id, request.Status, await AccessAsync(cancellationToken), cancellationToken);

    [HttpGet("{orderId:guid}/checklist-items")]
    [Authorize(Policy = PermissionCatalog.Orders.View)]
    public async Task<IReadOnlyList<ChecklistItemResponse>> ListChecklist(Guid orderId, CancellationToken cancellationToken) =>
        await orders.ListChecklistAsync(orderId, cancellationToken);

    [HttpPost("{orderId:guid}/checklist-items")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageChecklist)]
    public async Task<ActionResult<ChecklistItemResponse>> AddChecklistItem(
        Guid orderId,
        [FromBody] SaveChecklistItemBody request,
        CancellationToken cancellationToken)
    {
        var created = await orders.AddChecklistItemAsync(orderId, new SaveChecklistItemRequest(request.Text, request.SortOrder), cancellationToken);
        return Created($"/api/orders/{orderId}/checklist-items/{created.Id}", created);
    }

    [HttpPatch("{orderId:guid}/checklist-items/{itemId:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageChecklist)]
    public async Task<ChecklistItemResponse> UpdateChecklistItem(
        Guid orderId,
        Guid itemId,
        [FromBody] UpdateChecklistItemBody request,
        CancellationToken cancellationToken) =>
        await orders.UpdateChecklistItemAsync(orderId, itemId, new UpdateChecklistItemRequest(request.Text, request.IsCompleted), cancellationToken);

    [HttpDelete("{orderId:guid}/checklist-items/{itemId:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageChecklist)]
    public async Task<IActionResult> DeleteChecklistItem(Guid orderId, Guid itemId, CancellationToken cancellationToken)
    {
        await orders.DeleteChecklistItemAsync(orderId, itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{orderId:guid}/checklist-items/reorder")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageChecklist)]
    public async Task<IReadOnlyList<ChecklistItemResponse>> ReorderChecklist(
        Guid orderId,
        [FromBody] ReorderBody request,
        CancellationToken cancellationToken) =>
        await orders.ReorderChecklistAsync(orderId, request.Ids, cancellationToken);

    [HttpGet("{orderId:guid}/folder-links")]
    [Authorize(Policy = PermissionCatalog.Orders.View)]
    public async Task<IReadOnlyList<FolderLinkResponse>> ListFolderLinks(Guid orderId, CancellationToken cancellationToken) =>
        await orders.ListFolderLinksAsync(orderId, cancellationToken);

    [HttpPost("{orderId:guid}/folder-links")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageFolderLinks)]
    public async Task<ActionResult<FolderLinkResponse>> AddFolderLink(
        Guid orderId,
        [FromBody] SaveFolderLinkBody request,
        CancellationToken cancellationToken)
    {
        var created = await orders.AddFolderLinkAsync(orderId, new SaveFolderLinkRequest(request.Name, request.Path), cancellationToken);
        return Created($"/api/orders/{orderId}/folder-links/{created.Id}", created);
    }

    [HttpPatch("{orderId:guid}/folder-links/{linkId:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageFolderLinks)]
    public async Task<FolderLinkResponse> UpdateFolderLink(
        Guid orderId,
        Guid linkId,
        [FromBody] SaveFolderLinkBody request,
        CancellationToken cancellationToken) =>
        await orders.UpdateFolderLinkAsync(orderId, linkId, new SaveFolderLinkRequest(request.Name, request.Path), cancellationToken);

    [HttpDelete("{orderId:guid}/folder-links/{linkId:guid}")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageFolderLinks)]
    public async Task<IActionResult> DeleteFolderLink(Guid orderId, Guid linkId, CancellationToken cancellationToken)
    {
        await orders.DeleteFolderLinkAsync(orderId, linkId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{orderId:guid}/folder-links/reorder")]
    [Authorize(Policy = PermissionCatalog.Orders.ManageFolderLinks)]
    public async Task<IReadOnlyList<FolderLinkResponse>> ReorderFolderLinks(
        Guid orderId,
        [FromBody] ReorderBody request,
        CancellationToken cancellationToken) =>
        await orders.ReorderFolderLinksAsync(orderId, request.Ids, cancellationToken);

    private async Task RequirePermissionAsync(string permission, CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        if (!user.Permissions.Contains(permission, StringComparer.Ordinal))
        {
            throw new AuthException(
                AuthErrorCodes.PermissionDenied,
                "You do not have permission to perform this action.",
                StatusCodes.Status403Forbidden);
        }
    }

    private async Task<OrderFinancialAccess> AccessAsync(CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        return new OrderFinancialAccess(
            user.Permissions.Contains(PermissionCatalog.Orders.ViewSellingPrice, StringComparer.Ordinal),
            user.Permissions.Contains(PermissionCatalog.Orders.ViewCostPrice, StringComparer.Ordinal));
    }

    private static SaveOrderRequest ToRequest(SaveOrderBody request) =>
        new(request.ProjectId, request.OrderTypeId, request.Name, request.Description, request.Priority, request.Deadline, request.PreviewImagePath);
}

public sealed record SaveOrderBody(
    Guid ProjectId,
    Guid OrderTypeId,
    string Name,
    string? Description,
    string? Priority,
    DateOnly? Deadline,
    string? PreviewImagePath);

public sealed record ChangeOrderStatusBody(string Status);

public sealed class ChangeOrderTypeBody
{
    public Guid OrderTypeId { get; set; }

    public bool ResetCalculator { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed record SaveChecklistItemBody(string Text, int? SortOrder);

public sealed record UpdateChecklistItemBody(string? Text, bool? IsCompleted);

public sealed record SaveFolderLinkBody(string? Name, string Path);

public sealed record ReorderBody(IReadOnlyList<Guid> Ids);
