using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using FinTrack.Application.Authentication;
using FinTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FinTrack.IntegrationTests.Fixtures;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FINTRACK_TEST_DATABASE")))
            Skip = "Configure FINTRACK_TEST_DATABASE para executar integração com PostgreSQL.";
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FINTRACK_TEST_DATABASE")))
            Skip = "Configure FINTRACK_TEST_DATABASE para executar integração com PostgreSQL.";
    }
}

public sealed class PostgresApiFixture : IAsyncLifetime
{
    public static readonly Guid LegacyAccountId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private readonly string databaseName = $"fintrack_test_{Guid.NewGuid():N}";
    private string? adminConnection;
    private WebApplicationFactory<Program>? factory;
    private bool databaseCreated;

    public IServiceProvider Services => factory!.Services;

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/register", new Credentials($"test-{Guid.NewGuid():N}@example.com", "ValidPassword!123"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    public HttpClient CreateClient() => factory!.CreateClient();

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("FINTRACK_TEST_DATABASE");
        if (string.IsNullOrWhiteSpace(connection)) return;

        var settings = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres", Pooling = false };
        adminConnection = settings.ConnectionString;
        try
        {
            await ExecuteAdminAsync($"CREATE DATABASE \"{databaseName}\"");
            databaseCreated = true;
            settings.Database = databaseName;
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-bytes-long");
                builder.UseSetting("Auth:RequestsPerMinute", "200");
                builder.UseSetting("ConnectionStrings:Database", settings.ConnectionString);
            });
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            // Exercise a real upgrade with data created before users existed.
            await database.GetService<IMigrator>().MigrateAsync("20260930173644_InitialCreate");
            await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO accounts (id, name) VALUES ({LegacyAccountId}, {"Legacy account"})");
            await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO categories (id, name) VALUES ({LegacyAccountId}, {"Legacy category"})");
            await database.Database.MigrateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        if (databaseCreated)
        {
            await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
            databaseCreated = false;
        }
    }

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
