using LithographERP.Application.Modules.Authentication;
using LithographERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
            SetUtcNow(DefaultTestTime);
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LithographDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE calculator.order_calculators SET created_by = NULL, updated_by = NULL;
                DELETE FROM calculator.order_calculators;
                UPDATE calculator.cost_items SET created_by = NULL, updated_by = NULL;
                DELETE FROM calculator.cost_items;
                UPDATE orders.order_types SET calculator_template_id = NULL;
                ALTER TABLE calculator.template_versions DISABLE TRIGGER template_versions_immutable;
                DELETE FROM calculator.template_versions;
                ALTER TABLE calculator.template_versions ENABLE TRIGGER template_versions_immutable;
                UPDATE calculator.templates SET created_by = NULL, updated_by = NULL;
                DELETE FROM calculator.templates;
                DELETE FROM orders.checklist_items;
                DELETE FROM orders.folder_links;
                UPDATE orders.orders SET created_by = NULL, updated_by = NULL;
                DELETE FROM orders.orders;
                UPDATE orders.order_types SET created_by = NULL, updated_by = NULL;
                DELETE FROM orders.order_types;
                DO $$
                DECLARE sequence_name text;
                BEGIN
                  FOR sequence_name IN
                    SELECT schemaname || '.' || sequencename
                    FROM pg_sequences
                    WHERE schemaname = 'orders' AND sequencename LIKE 'order_business_id_%'
                  LOOP
                    EXECUTE format('SELECT setval(%L, 1, false)', sequence_name);
                  END LOOP;
                END $$;
                UPDATE projects.project_members SET assigned_by = NULL;
                DELETE FROM projects.project_members;
                UPDATE projects.projects SET created_by = NULL, updated_by = NULL;
                DELETE FROM projects.projects;
                DO $$
                DECLARE sequence_name text;
                BEGIN
                  FOR sequence_name IN
                    SELECT schemaname || '.' || sequencename
                    FROM pg_sequences
                    WHERE schemaname = 'projects' AND sequencename LIKE 'project_business_id_%'
                  LOOP
                    EXECUTE format('SELECT setval(%L, 1, false)', sequence_name);
                  END LOOP;
                END $$;
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
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<AdjustableTimeProvider>();
            services.AddSingleton<TimeProvider>(provider => provider.GetRequiredService<AdjustableTimeProvider>());
        });
    }

    public static readonly DateTimeOffset DefaultTestTime = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    public void SetUtcNow(DateTimeOffset value) =>
        Services.GetRequiredService<AdjustableTimeProvider>().UtcNow = value;

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

