using LithographERP.Api.Errors;
using LithographERP.Application.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LithographERP.Api.Authentication;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler(IAuthService auth) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!CurrentUserId.TryGet(context.User, out var userId))
        {
            return;
        }

        var current = await auth.GetCurrentUserAsync(userId, CancellationToken.None);
        if (current.Permissions.Contains(requirement.Permission, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true && !authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(AuthErrorCodes.PermissionDenied, "You do not have permission to perform this action."));
            return;
        }

        var failure = context.Items[AuthFailure.ItemKey] as SessionValidationStatus?;
        var (code, message) = failure switch
        {
            SessionValidationStatus.Expired => (AuthErrorCodes.SessionExpired, "Your session has expired. Please sign in again."),
            SessionValidationStatus.Revoked => (AuthErrorCodes.SessionInvalid, "Your session is no longer valid. Please sign in again."),
            _ => (AuthErrorCodes.AuthenticationRequired, "Authentication is required."),
        };

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(code, message));
    }
}
