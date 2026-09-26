using System.Net;
using System.Net.Http.Json;
using LithographERP.Api.Health;

namespace LithographERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class HealthEndpointTests(LithographApiFactory factory)
{
    [Fact]
    public async Task GetHealth_ReturnsHealthy_WhenDatabaseReachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Application);
        Assert.Equal("Healthy", body.Database);
    }

    [Fact]
    public void Startup_FailsClearly_WhenConnectionStringMissing()
    {
        using var missing = LithographApiFactory.WithoutConnectionString();

        var exception = Assert.ThrowsAny<Exception>(() => missing.CreateClient());

        Assert.Contains("Database connection configuration missing", GetFullMessage(exception));
    }

    private static string GetFullMessage(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" | ", messages);
    }
}
