using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.CreditCards.DTOs;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class CreditCardEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private const string Route = "/credit-cards";

    [Fact]
    public async Task Crud_PersistsPreciseAmountsAndDeletes()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        using var created = await client.PostAsJsonAsync(Route, new { Name = "  Principal  ", Limit = 1234.56m, ClosingDay = 31, DueDay = 1 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var card = (await created.Content.ReadFromJsonAsync<CreditCardDto>())!;
        var url = $"{Route}/{card.Id}";
        Assert.Equal(url, created.Headers.Location?.OriginalString);
        Assert.Equal("Principal", card.Name);
        using var reader = fixture.CreateClient();
        reader.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        Assert.Equal(card, await reader.GetFromJsonAsync<CreditCardDto>(url));
        Assert.Contains(card, (await reader.GetFromJsonAsync<CreditCardDto[]>(Route))!);
        using var updated = await client.PutAsJsonAsync(url, new { Name = "Novo", Limit = 9999999999999999.99m, ClosingDay = 1, DueDay = 31 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(new CreditCardDto(card.Id, "Novo", 9999999999999999.99m, 1, 31), await reader.GetFromJsonAsync<CreditCardDto>(url));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(url, new { Name = "Novo", Limit = 100m, ClosingDay = 1, DueDay = 31 })).StatusCode);
    }

    [Fact]
    public async Task Ownership_ProtectsEveryOperation()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var request = new { Name = "Privado", Limit = 100m, ClosingDay = 10, DueDay = 20 };
        var created = await owner.PostAsJsonAsync(Route, request);
        var card = (await created.Content.ReadFromJsonAsync<CreditCardDto>())!;
        var url = $"{Route}/{card.Id}";
        Assert.Empty((await other.GetFromJsonAsync<CreditCardDto[]>(Route))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Route, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync(url, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync(url)).StatusCode);
        Assert.Equal(card, await owner.GetFromJsonAsync<CreditCardDto>(url));
    }

    [Fact]
    public async Task InvalidInput_ReturnsFieldErrorsAndPreservesData()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var invalid = new { Name = " ", Limit = 1.001m, ClosingDay = 0, DueDay = 32 };
        var response = await client.PostAsJsonAsync(Route, invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        foreach (var field in new[] { "name", "limit", "closingDay", "dueDay" })
            Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Empty((await client.GetFromJsonAsync<CreditCardDto[]>(Route))!);
        var created = await client.PostAsJsonAsync(Route, new { Name = "Original", Limit = 100m, ClosingDay = 10, DueDay = 20 });
        var card = (await created.Content.ReadFromJsonAsync<CreditCardDto>())!;
        var url = $"{Route}/{card.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(url, invalid)).StatusCode);
        Assert.Equal(card, await client.GetFromJsonAsync<CreditCardDto>(url));
    }
}
