using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinTrack.IntegrationTests;

public sealed class HealthEndpointTests
{
  [Theory]
  [InlineData("Development")]
  [InlineData("Production")]
  public async Task GetHealth_ReturnsHealthyJsonWithoutDatabase(string environment)
  {
    await using var factory = new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.UseEnvironment(environment);
        builder.UseSetting(
          "ConnectionStrings:Database",
          "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused"
        );
      });
    using var client = factory.CreateClient();

    using var response = await client.GetAsync("/health");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal("healthy", body.RootElement.GetProperty("status").GetString());
    Assert.Single(body.RootElement.EnumerateObject());
  }
}
