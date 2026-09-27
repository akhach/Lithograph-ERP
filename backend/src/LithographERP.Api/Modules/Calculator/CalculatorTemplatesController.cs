using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Calculator;

[ApiController]
[Authorize]
[Route("api/calculator-templates")]
public sealed class CalculatorTemplatesController(ICalculatorTemplateAdminService templates, IAuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? view, CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        if (string.Equals(view, "selector", StringComparison.OrdinalIgnoreCase))
        {
            RequireAny(
                user,
                PermissionCatalog.Calculator.ManageTemplates,
                PermissionCatalog.Calculator.View,
                PermissionCatalog.Calculator.PublishTemplates,
                PermissionCatalog.Orders.ManageTypes);
            return Ok(await templates.ListActiveOptionsAsync(cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(view))
        {
            throw CalculatorRequestException.Field("view", "View must be selector.");
        }

        RequireAny(
            user,
            PermissionCatalog.Calculator.ManageTemplates,
            PermissionCatalog.Calculator.View,
            PermissionCatalog.Calculator.PublishTemplates);
        return Ok(await templates.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<CalculatorTemplateDetail> Get(Guid id, CancellationToken cancellationToken)
    {
        await RequireReaderAsync(cancellationToken);
        return await templates.GetAsync(id, cancellationToken);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<ActionResult<CalculatorTemplateDetail>> Create(
        [FromBody] SaveCalculatorTemplateBody request,
        CancellationToken cancellationToken)
    {
        var created = await templates.CreateAsync(
            CurrentUserId.Require(User),
            new SaveCalculatorTemplateRequest(request.Name, request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<CalculatorTemplateDetail> Update(
        Guid id,
        [FromBody] SaveCalculatorTemplateBody request,
        CancellationToken cancellationToken) =>
        await templates.UpdateAsync(
            CurrentUserId.Require(User),
            id,
            new SaveCalculatorTemplateRequest(request.Name, request.Description),
            cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<CalculatorTemplateDetail> Activate(Guid id, CancellationToken cancellationToken) =>
        await templates.SetActiveAsync(CurrentUserId.Require(User), id, true, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<CalculatorTemplateDetail> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await templates.SetActiveAsync(CurrentUserId.Require(User), id, false, cancellationToken);

    [HttpGet("{templateId:guid}/versions")]
    public async Task<IReadOnlyList<CalculatorTemplateVersionSummary>> ListVersions(Guid templateId, CancellationToken cancellationToken)
    {
        await RequireReaderAsync(cancellationToken);
        return await templates.ListVersionsAsync(templateId, cancellationToken);
    }

    [HttpGet("{templateId:guid}/versions/{versionId:guid}")]
    public async Task<CalculatorTemplateVersionDetail> GetVersion(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        await RequireReaderAsync(cancellationToken);
        return await templates.GetVersionAsync(templateId, versionId, cancellationToken);
    }

    [HttpPost("{templateId:guid}/versions")]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<ActionResult<CalculatorTemplateVersionDetail>> CreateVersion(Guid templateId, CancellationToken cancellationToken)
    {
        var created = await templates.CreateVersionAsync(CurrentUserId.Require(User), templateId, cancellationToken);
        return CreatedAtAction(nameof(GetVersion), new { templateId, versionId = created.Id }, created);
    }

    [HttpPatch("{templateId:guid}/versions/{versionId:guid}")]
    [Authorize(Policy = PermissionCatalog.Calculator.ManageTemplates)]
    public async Task<CalculatorTemplateVersionDetail> UpdateDraft(
        Guid templateId,
        Guid versionId,
        [FromBody] SaveCalculatorTemplateVersionBody request,
        CancellationToken cancellationToken)
    {
        if (request.Definition is null)
        {
            throw CalculatorRequestException.Field("definition", "A value is required.");
        }

        return await templates.UpdateDraftAsync(
            CurrentUserId.Require(User),
            templateId,
            versionId,
            request.Definition,
            cancellationToken);
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/validate")]
    public async Task<TemplateValidationResult> Validate(Guid templateId, Guid versionId, CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        RequireAny(user, PermissionCatalog.Calculator.ManageTemplates, PermissionCatalog.Calculator.PublishTemplates);
        return await templates.ValidateAsync(templateId, versionId, cancellationToken);
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/publish")]
    [Authorize(Policy = PermissionCatalog.Calculator.PublishTemplates)]
    public async Task<CalculatorTemplateVersionDetail> Publish(Guid templateId, Guid versionId, CancellationToken cancellationToken) =>
        await templates.PublishAsync(CurrentUserId.Require(User), templateId, versionId, cancellationToken);

    private async Task RequireReaderAsync(CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        RequireAny(
            user,
            PermissionCatalog.Calculator.ManageTemplates,
            PermissionCatalog.Calculator.View,
            PermissionCatalog.Calculator.PublishTemplates);
    }

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

public sealed record SaveCalculatorTemplateBody(string Name, string? Description);

public sealed record SaveCalculatorTemplateVersionBody(CalculatorTemplateDefinition? Definition);
