using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FinTrack.Application.Authentication;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.IntegrationTests;

public sealed class AuthenticationTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private sealed record Resource(Guid Id, string Name);

    [Fact]
    public async Task RegisterLoginRefreshLogout_EnforcesCredentialAndSessionLifecycle()
    {
        using var client = fixture.CreateClient();
        var credentials = new Credentials($"user-{Guid.NewGuid():N}@example.com", "Password!123456");
        using var registered = await client.PostAsJsonAsync("/auth/register", credentials);
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var initial = (await registered.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.True(initial.ExpiresAt > DateTimeOffset.UtcNow);

        using var duplicate = await client.PostAsJsonAsync("/auth/register", credentials with { Email = credentials.Email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/auth/login", credentials with { Password = "wrong" });
        using var unknown = await client.PostAsJsonAsync("/auth/login", credentials with { Email = "unknown@example.com" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        using var loggedIn = await client.PostAsJsonAsync("/auth/login", credentials);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);

        using var refreshed = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(initial.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotated = (await refreshed.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.NotEqual(initial.RefreshToken, rotated.RefreshToken);
        using var replay = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(initial.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        client.DefaultRequestHeaders.Authorization = new("Bearer", rotated.AccessToken);
        using var me = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var logout = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var revoked = await client.GetAsync("/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var revokedRefresh = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, revokedRefresh.StatusCode);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var user = await db.Users.SingleAsync(user => user.Email == credentials.Email);
        Assert.NotEqual(credentials.Password, user.PasswordHash);
        var sessions = await db.RefreshSessions.Where(session => session.UserId == user.Id).ToListAsync();
        Assert.All(sessions, session => { Assert.Equal(64, session.TokenHash.Length); Assert.NotEqual(initial.RefreshToken, session.TokenHash); });
    }

    [Theory]
    [InlineData("/accounts")]
    [InlineData("/categories")]
    public async Task DifferentUsers_CannotListReadChangeOrDeleteOthersResources(string route)
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var created = await owner.PostAsJsonAsync(route, new { Name = "Privado" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var resource = (await created.Content.ReadFromJsonAsync<Resource>())!;
        Assert.Contains(resource, (await owner.GetFromJsonAsync<Resource[]>(route))!);
        Assert.DoesNotContain((await other.GetFromJsonAsync<Resource[]>(route))!, item => item.Id == resource.Id);
        using var get = await other.GetAsync($"{route}/{resource.Id}");
        using var update = await other.PutAsJsonAsync($"{route}/{resource.Id}", new { Name = "Ataque" });
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        if (route == "/accounts")
        {
            using var delete = await other.DeleteAsync($"{route}/{resource.Id}");
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
        Assert.Equal(resource, await owner.GetFromJsonAsync<Resource>($"{route}/{resource.Id}"));
    }

    [Fact]
    public async Task ConcurrentRefresh_OnlyOneRequestConsumesToken()
    {
        using var client = fixture.CreateClient();
        using var registered = await client.PostAsJsonAsync("/auth/register", new Credentials($"race-{Guid.NewGuid():N}@example.com", "Password!123456"));
        var tokens = (await registered.Content.ReadFromJsonAsync<AuthTokens>())!;
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(tokens.RefreshToken)),
            client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(tokens.RefreshToken)));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task BearerValidation_RejectsTamperingWrongIssuerAudienceAndExpiredTokens()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var source = new JwtSecurityTokenHandler().ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter);
        var now = DateTime.UtcNow;
        var claims = source.Claims.Where(claim => claim.Type is not ("iss" or "aud" or "exp" or "nbf")).ToArray();
        foreach (var variant in new[] { "signature", "issuer", "audience", "expired" })
        {
            var key = variant == "signature" ? "wrong-key-that-is-at-least-32-bytes-long" : "test-only-signing-key-at-least-32-bytes-long";
            var jwt = new JwtSecurityToken(variant == "issuer" ? "WrongIssuer" : "FinTrack", variant == "audience" ? "WrongAudience" : "FinTrack.Client", claims,
                now.AddMinutes(-20), variant == "expired" ? now.AddMinutes(-10) : now.AddMinutes(10),
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
            using var response = await client.GetAsync("/accounts");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("GET", "/accounts")]
    [InlineData("POST", "/accounts")]
    [InlineData("GET", "/accounts/44444444-4444-4444-8444-444444444444")]
    [InlineData("PUT", "/accounts/44444444-4444-4444-8444-444444444444")]
    [InlineData("DELETE", "/accounts/44444444-4444-4444-8444-444444444444")]
    [InlineData("GET", "/categories")]
    [InlineData("POST", "/categories")]
    [InlineData("GET", "/categories/44444444-4444-4444-8444-444444444444")]
    [InlineData("PUT", "/categories/44444444-4444-4444-8444-444444444444")]
    [InlineData("POST", "/auth/logout")]
    public async Task AnonymousRequests_CannotAccessProtectedEndpoints(string method, string path)
    {
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { Name = "Conta" }) };
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredSession_RejectsAccessAndRefresh()
    {
        using var client = fixture.CreateClient();
        using var registered = await client.PostAsJsonAsync("/auth/register", new Credentials($"expired-{Guid.NewGuid():N}@example.com", "Password!123456"));
        var tokens = (await registered.Content.ReadFromJsonAsync<AuthTokens>())!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var sessionId = Guid.Parse(jwt.Claims.Single(claim => claim.Type == "sid").Value);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        await db.RefreshSessions.Where(session => session.Id == sessionId).ExecuteUpdateAsync(update => update.SetProperty(session => session.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        using var me = await client.GetAsync("/auth/me");
        using var refresh = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task LegacyUnownedData_RemainsStoredButCannotBeClaimedByNewUser()
    {
        var id = PostgresApiFixture.LegacyAccountId;
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        using var client = await fixture.CreateAuthenticatedClientAsync();
        using var get = await client.GetAsync($"/accounts/{id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.True(await db.Accounts.IgnoreQueryFilters().AnyAsync(account => account.Id == id && account.UserId == null));
        Assert.True(await db.Categories.IgnoreQueryFilters().AnyAsync(category => category.Id == id && category.UserId == null));
    }
}
