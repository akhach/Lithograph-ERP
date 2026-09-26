using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth, AuthSettings settings) : ControllerBase
{
    [HttpGet("setup-status")]
    [AllowAnonymous]
    public async Task<SetupStatusResponse> SetupStatus(CancellationToken cancellationToken) =>
        await auth.GetSetupStatusAsync(cancellationToken);

    [HttpPost("setup")]
    [AllowAnonymous]
    public async Task<IActionResult> Setup([FromBody] PasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.SetupDirectorAsync(request.Password, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<CurrentUserResponse> Login([FromBody] LoginBody request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(
            request.Username,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        SessionCookie.Set(HttpContext, settings, result.SessionToken, result.ExpiresAt);
        return result.User;
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(SessionCookie.Read(HttpContext, settings), cancellationToken);
        SessionCookie.Clear(HttpContext, settings);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<CurrentUserResponse> Me(CancellationToken cancellationToken) =>
        await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBody request, CancellationToken cancellationToken)
    {
        await auth.ChangePasswordAsync(
            CurrentUserId.Require(User),
            SessionCookie.Read(HttpContext, settings),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
        return NoContent();
    }

    public sealed record PasswordRequest(string Password);

    public sealed record LoginBody(string Username, string Password);

    public sealed record ChangePasswordBody(string CurrentPassword, string NewPassword);
}

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserAdminService users, IAuthService auth) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Users.View)]
    public async Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        await users.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Users.View)]
    public async Task<UserResponse> Get(Guid id, CancellationToken cancellationToken) =>
        await users.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Users.Create)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserBody request, CancellationToken cancellationToken)
    {
        var actorId = CurrentUserId.Require(User);
        var current = await auth.GetCurrentUserAsync(actorId, cancellationToken);
        var created = await users.CreateAsync(
            actorId,
            new CreateUserRequest(request.Username, request.Password, request.RoleIds, request.IsActive),
            current.Permissions.Contains(PermissionCatalog.Users.ManageRoles, StringComparer.Ordinal),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Users.Edit)]
    public async Task<UserResponse> Update(Guid id, [FromBody] UpdateUserBody request, CancellationToken cancellationToken) =>
        await users.UpdateUsernameAsync(CurrentUserId.Require(User), id, request.Username, cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCatalog.Users.Activate)]
    public async Task<UserResponse> Activate(Guid id, CancellationToken cancellationToken) =>
        await users.ActivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionCatalog.Users.Activate)]
    public async Task<UserResponse> Deactivate(Guid id, CancellationToken cancellationToken) =>
        await users.DeactivateAsync(CurrentUserId.Require(User), id, cancellationToken);

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = PermissionCatalog.Users.ResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordBody request, CancellationToken cancellationToken)
    {
        await users.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = PermissionCatalog.Users.ManageRoles)]
    public async Task<UserResponse> AssignRole(Guid id, Guid roleId, CancellationToken cancellationToken) =>
        await users.AssignRoleAsync(CurrentUserId.Require(User), id, roleId, cancellationToken);

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = PermissionCatalog.Users.ManageRoles)]
    public async Task<UserResponse> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken) =>
        await users.RemoveRoleAsync(CurrentUserId.Require(User), id, roleId, cancellationToken);

    public sealed record CreateUserBody(string Username, string Password, Guid[]? RoleIds, bool? IsActive);

    public sealed record UpdateUserBody(string Username);

    public sealed record ResetPasswordBody(string NewPassword);
}

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(IRoleAdminService roles) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Roles.View)]
    public async Task<IReadOnlyList<RoleResponse>> List(CancellationToken cancellationToken) =>
        await roles.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Roles.View)]
    public async Task<RoleResponse> Get(Guid id, CancellationToken cancellationToken) =>
        await roles.GetAsync(id, cancellationToken);

    [HttpGet("~/api/permissions")]
    [Authorize(Policy = PermissionCatalog.Roles.View)]
    public async Task<IReadOnlyList<PermissionResponse>> Permissions(CancellationToken cancellationToken) =>
        await roles.ListPermissionsAsync(cancellationToken);

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Roles.Create)]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] RoleBody request, CancellationToken cancellationToken)
    {
        var created = await roles.CreateAsync(CurrentUserId.Require(User), request.Name, request.Description, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.Roles.Edit)]
    public async Task<RoleResponse> Update(Guid id, [FromBody] RoleBody request, CancellationToken cancellationToken) =>
        await roles.UpdateAsync(CurrentUserId.Require(User), id, request.Name, request.Description, cancellationToken);

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = PermissionCatalog.Roles.ManagePermissions)]
    public async Task<RoleResponse> SetPermissions(Guid id, [FromBody] SetPermissionsBody request, CancellationToken cancellationToken) =>
        await roles.SetPermissionsAsync(CurrentUserId.Require(User), id, request.PermissionIds ?? [], cancellationToken);

    public sealed record RoleBody(string Name, string? Description);

    public sealed record SetPermissionsBody(Guid[]? PermissionIds);
}
