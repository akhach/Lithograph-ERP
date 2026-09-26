using System.Security.Claims;
using LithographERP.Application.Modules.Authentication;

namespace LithographERP.Api.Authentication;

public static class CurrentUserId
{
    public static bool TryGet(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

    public static Guid Require(ClaimsPrincipal principal)
    {
        if (!TryGet(principal, out var userId))
        {
            throw new AuthException(AuthErrorCodes.AuthenticationRequired, "Authentication is required.", StatusCodes.Status401Unauthorized);
        }

        return userId;
    }
}

public static class SessionCookie
{
    public static void Set(HttpContext httpContext, AuthSettings settings, string token, DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(settings.CookieName, token, Options(settings, expiresAt));
    }

    public static void Clear(HttpContext httpContext, AuthSettings settings)
    {
        httpContext.Response.Cookies.Delete(settings.CookieName, Options(settings, expiresAt: null));
    }

    public static string? Read(HttpContext httpContext, AuthSettings settings) =>
        httpContext.Request.Cookies.TryGetValue(settings.CookieName, out var token) ? token : null;

    private static CookieOptions Options(AuthSettings settings, DateTimeOffset? expiresAt) =>
        new()
        {
            HttpOnly = true,
            Secure = settings.CookieSecure,
            SameSite = Enum.Parse<SameSiteMode>(settings.CookieSameSite, ignoreCase: true),
            Path = "/",
            IsEssential = true,
            Expires = expiresAt,
        };
}
