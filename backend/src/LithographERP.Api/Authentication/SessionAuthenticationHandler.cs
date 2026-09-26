using LithographERP.Application.Modules.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace LithographERP.Api.Authentication;

public static class AuthFailure
{
    public const string ItemKey = "Lithograph.AuthFailure";
}

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISessionValidator sessions,
    AuthSettings settings) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(settings.CookieName, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var result = await sessions.ValidateAsync(token, Context.RequestAborted);
        if (result.Status != SessionValidationStatus.Valid || result.UserId is null || result.Username is null)
        {
            Context.Items[AuthFailure.ItemKey] = result.Status;
            return AuthenticateResult.Fail("Session is not valid.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId.Value.ToString()),
            new Claim(ClaimTypes.Name, result.Username),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
