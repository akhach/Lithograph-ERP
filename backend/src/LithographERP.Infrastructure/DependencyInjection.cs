using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Clients;
using LithographERP.Application.Modules.Employees;
using LithographERP.Application.Modules.Numbering;
using LithographERP.Infrastructure.Modules.Authentication;
using LithographERP.Infrastructure.Modules.Clients;
using LithographERP.Infrastructure.Modules.Employees;
using LithographERP.Infrastructure.Modules.Numbering;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<LithographDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(ReadAuthSettings(configuration));
        services.AddSingleton<PasswordHashing>();
        services.AddScoped<IAuthenticationBootstrap, AuthenticationBootstrap>();
        services.AddScoped<AuthService>();
        services.AddScoped<IAuthService>(provider => provider.GetRequiredService<AuthService>());
        services.AddScoped<ISessionValidator>(provider => provider.GetRequiredService<AuthService>());
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IEmployeeAdminService, EmployeeAdminService>();
        services.AddScoped<IBusinessIdGenerator, BusinessIdGenerator>();
        services.AddScoped<IClientAdminService, ClientAdminService>();

        return services;
    }

    private static AuthSettings ReadAuthSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection(AuthSettings.SectionName);
        var settings = new AuthSettings();
        if (int.TryParse(section[nameof(AuthSettings.SessionLifetimeHours)], out var hours))
        {
            settings.SessionLifetimeHours = hours;
        }

        if (settings.SessionLifetimeHours < 1)
        {
            throw new InvalidOperationException("Authentication:SessionLifetimeHours must be at least 1.");
        }

        var cookieName = section[nameof(AuthSettings.CookieName)];
        if (!string.IsNullOrWhiteSpace(cookieName))
        {
            settings.CookieName = cookieName;
        }

        var sameSite = section[nameof(AuthSettings.CookieSameSite)];
        if (!string.IsNullOrWhiteSpace(sameSite))
        {
            settings.CookieSameSite = sameSite;
        }

        if (bool.TryParse(section[nameof(AuthSettings.CookieSecure)], out var secure))
        {
            settings.CookieSecure = secure;
        }

        return settings;
    }
}
