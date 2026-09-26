using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LithographERP.IntegrationTests;

public sealed class LithographApiFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionStringKey = "ConnectionStrings:LithographTestDb";

    private readonly string? _connectionString;

    public LithographApiFactory()
        : this(ReadTestConnectionString())
    {
    }

    private LithographApiFactory(string? connectionString)
    {
        _connectionString = connectionString;
    }

    public static LithographApiFactory WithoutConnectionString() => new(connectionString: null);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:LithographDb", _connectionString ?? string.Empty);
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
