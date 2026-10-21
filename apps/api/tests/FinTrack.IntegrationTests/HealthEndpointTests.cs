using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinTrack.IntegrationTests;

public sealed class HealthEndpointTests
{
  [Fact]
  public async Task RegistrationValidationAndRateLimit_DoNotRequireDatabase()
  {
    await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
      builder.UseEnvironment("Production");
      builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-bytes-long");
      builder.UseSetting("ConnectionStrings:Database", "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused");
    });
    using var client = factory.CreateClient();
    for (var index = 0; index < 20; index++)
    {
      using var response = await client.PostAsJsonAsync("/auth/register", new { Email = "invalid", Password = "short" });
      Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    using var throttled = await client.PostAsJsonAsync("/auth/register", new { Email = "invalid", Password = "short" });
    Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
  }

  [Theory]
  [InlineData("Development")]
  [InlineData("Production")]
  public async Task GetHealth_ReturnsHealthyJsonWithoutDatabase(string environment)
  {
    await using var factory = new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.UseEnvironment(environment);
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-bytes-long");
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
