using LithographERP.Api.Authentication;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Dashboard;
using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithographERP.Api.Modules.Dashboard;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboard, IAuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<DashboardResponse> Get(CancellationToken cancellationToken)
    {
        var user = await auth.GetCurrentUserAsync(CurrentUserId.Require(User), cancellationToken);
        return await dashboard.GetAsync(
            user.Permissions.Contains(PermissionCatalog.Orders.View, StringComparer.Ordinal),
            user.Permissions.Contains(PermissionCatalog.Projects.View, StringComparer.Ordinal),
            cancellationToken);
    }
}
