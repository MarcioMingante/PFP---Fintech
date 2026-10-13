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
using Testcontainers.PostgreSql;

namespace FinTrack.IntegrationTests.Fixtures;

public sealed class PostgresApiFixture : IAsyncLifetime
{
    public static readonly Guid LegacyAccountId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid LegacyCardId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private PostgreSqlContainer? container;
    private WebApplicationFactory<Program>? factory;

    public IServiceProvider Services => (factory ?? throw new InvalidOperationException("Fixture não iniciada.")).Services;

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/register", new Credentials($"test-{Guid.NewGuid():N}@example.com", "ValidPassword!123"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    public HttpClient CreateClient() => (factory ?? throw new InvalidOperationException("Fixture não iniciada.")).CreateClient();

    public async Task InitializeAsync()
    {
        try
        {
            container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("fintrack_tests")
                .WithUsername("fintrack_test")
                .WithPassword($"test-{Guid.NewGuid():N}")
                .Build();
            using var startup = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await container.StartAsync(startup.Token);
            var connectionString = container.GetConnectionString();
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-32-bytes-long");
                builder.UseSetting("Auth:RequestsPerMinute", "200");
                builder.UseSetting("ConnectionStrings:Database", connectionString);
            });
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            // Exercise a real upgrade with data created before users existed.
            await database.GetService<IMigrator>().MigrateAsync("20260930173644_InitialCreate");
            await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO accounts (id, name) VALUES ({LegacyAccountId}, {"Legacy account"})");
            await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO categories (id, name) VALUES ({LegacyAccountId}, {"Legacy category"})");
            await database.GetService<IMigrator>().MigrateAsync("20261013022113_AddPurchasesAndInstallments");
            // Real phase-15 records exercise invoice backfill on every integration database.
            await database.Database.ExecuteSqlRawAsync("""
                INSERT INTO users (id, email, password_hash)
                VALUES ('22222222-2222-4222-8222-222222222222', 'legacy-invoices@example.com', 'test-only-unused-hash');
                INSERT INTO credit_cards (id, user_id, name, "limit", closing_day, due_day)
                VALUES ('33333333-3333-4333-8333-333333333333', '22222222-2222-4222-8222-222222222222', 'Legacy card', 5000, 25, 5);
                INSERT INTO purchases ("Id", "CreditCardId", "UserId", "IdempotencyKey", "Description", "Amount", "InstallmentCount", "PurchaseDate", "ClosingDay", "DueDay")
                VALUES ('44444444-4444-4444-8444-444444444444', '33333333-3333-4333-8333-333333333333', '22222222-2222-4222-8222-222222222222', '44444444-4444-4444-8444-444444444444', 'Legacy purchase', 100, 2, '2026-01-01', 25, 5),
                       ('55555555-5555-4555-8555-555555555555', '33333333-3333-4333-8333-333333333333', '22222222-2222-4222-8222-222222222222', '55555555-5555-4555-8555-555555555555', 'Second purchase', 20, 1, '2026-01-02', 25, 5);
                INSERT INTO installments ("Id", "PurchaseId", "Number", "Amount", "ClosingDate", "DueDate")
                VALUES ('66666666-6666-4666-8666-666666666666', '44444444-4444-4444-8444-444444444444', 1, 50, '2026-01-25', '2026-02-05'),
                       ('77777777-7777-4777-8777-777777777777', '44444444-4444-4444-8444-444444444444', 2, 50, '2026-02-25', '2026-03-05'),
                       ('88888888-8888-4888-8888-888888888888', '55555555-5555-4555-8555-555555555555', 1, 20, '2026-01-25', '2026-02-05');
                """);
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
        var currentFactory = factory;
        factory = null;
        try
        {
            if (currentFactory is not null) await currentFactory.DisposeAsync();
        }
        finally
        {
            var currentContainer = container;
            container = null;
            if (currentContainer is not null) await currentContainer.DisposeAsync();
        }
    }
}
