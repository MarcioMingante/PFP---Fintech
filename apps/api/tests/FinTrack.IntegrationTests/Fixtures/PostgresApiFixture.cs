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
    private readonly string databaseName = $"fintrack_test_{Guid.NewGuid():N}";
    private string? adminConnection;
    private WebApplicationFactory<Program>? factory;
    private bool databaseCreated;

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
                builder.UseSetting("ConnectionStrings:Database", settings.ConnectionString);
            });
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
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
