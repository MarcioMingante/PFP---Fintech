using System.Net;
using System.Net.Http.Json;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.IntegrationTests;

public sealed class DatabaseIsolationTests
{
    private sealed record Account(Guid Id, string Name);

    [Fact]
    public async Task IndependentFixtures_IsolateDatabaseAndDisposalPreservesOtherFixture()
    {
        var first = new PostgresApiFixture();
        var second = new PostgresApiFixture();
        try
        {
            await first.InitializeAsync();
            await second.InitializeAsync();
            using var writer = await first.CreateAuthenticatedClientAsync();
            var response = await writer.PostAsJsonAsync("/accounts", new { Name = "Only first database" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var account = (await response.Content.ReadFromJsonAsync<Account>())!;
            using (var firstScope = first.Services.CreateScope())
            using (var secondScope = second.Services.CreateScope())
            {
                var firstDatabase = firstScope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
                var secondDatabase = secondScope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
                // Bypass ownership filters to prove physical isolation, not just user isolation.
                Assert.True(await firstDatabase.Accounts.IgnoreQueryFilters().AnyAsync(item => item.Id == account.Id));
                Assert.False(await secondDatabase.Accounts.IgnoreQueryFilters().AnyAsync(item => item.Id == account.Id));
                Assert.NotEmpty(await secondDatabase.Database.GetAppliedMigrationsAsync());
                Assert.Empty(await secondDatabase.Database.GetPendingMigrationsAsync());
            }
            await first.DisposeAsync();
            using var survivor = await second.CreateAuthenticatedClientAsync();
            var survivingAccount = await survivor.PostAsJsonAsync("/accounts", new { Name = "Second database remains available" });
            Assert.Equal(HttpStatusCode.Created, survivingAccount.StatusCode);
            Assert.Single((await survivor.GetFromJsonAsync<Account[]>("/accounts"))!);
        }
        finally
        {
            try { await first.DisposeAsync(); }
            finally { await second.DisposeAsync(); }
        }
    }
}
