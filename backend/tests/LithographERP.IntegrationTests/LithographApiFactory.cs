using LithographERP.Application.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LithographERP.IntegrationTests;

[CollectionDefinition(DatabaseCollection.Name)]
public sealed class DatabaseCollection : ICollectionFixture<LithographApiFactory>
{
    public const string Name = "Database";
}

public sealed class LithographApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestConnectionStringKey = "ConnectionStrings:LithographTestDb";

    private readonly string? _connectionString;
    private readonly SemaphoreSlim _reset = new(1, 1);

    public LithographApiFactory()
        : this(ReadTestConnectionString())
    {
    }

    private LithographApiFactory(string? connectionString)
    {
        _connectionString = connectionString;
    }

    public static LithographApiFactory WithoutConnectionString() => new(connectionString: null);

    public async Task ResetAsync()
    {
        await _reset.WaitAsync();
        try
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE clients.clients SET created_by = NULL, updated_by = NULL;
                DELETE FROM clients.clients;
                SELECT setval('clients.client_business_id_seq', 1, false);
                UPDATE employees.employees SET created_by = NULL, updated_by = NULL;
                DELETE FROM employees.employees;
                UPDATE auth.users SET created_by = NULL, updated_by = NULL;
                UPDATE auth.roles SET created_by = NULL, updated_by = NULL;
                UPDATE auth.user_roles SET assigned_by = NULL;
                UPDATE auth.role_permissions SET assigned_by = NULL;
                DELETE FROM auth.sessions;
                DELETE FROM auth.user_roles;
                DELETE FROM auth.role_permissions;
                DELETE FROM auth.users;
                DELETE FROM auth.roles;
                DELETE FROM auth.permissions;
                """);
            await scope.ServiceProvider.GetRequiredService<IAuthenticationBootstrap>().SynchronizeAsync();
        }
        finally
        {
            _reset.Release();
        }
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        _ = Services;
        await Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LithographDb", _connectionString ?? string.Empty);
        builder.UseSetting("Authentication:CookieSecure", "false");
        builder.UseSetting("Authentication:CookieSameSite", "Lax");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:CookieSecure"] = "false",
                ["Authentication:CookieSameSite"] = "Lax",
            });
        });
    }

    private static string ReadTestConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<LithographApiFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration[TestConnectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Integration test database configuration missing. Set '{TestConnectionStringKey}' " +
                "(User Secrets of LithographERP.IntegrationTests or environment variable 'ConnectionStrings__LithographTestDb').");
        }

        return connectionString;
    }
}

