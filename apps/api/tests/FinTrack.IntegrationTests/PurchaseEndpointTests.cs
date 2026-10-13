using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.CreditCards.DTOs;
using FinTrack.Application.Purchases;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class PurchaseEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private static CreatePurchaseCommand Command(Guid? key = null) => new("  Compra  ", 100m, 3, new(2026, 1, 25), key ?? Guid.NewGuid());
    private static async Task<Guid> CreateCard(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/credit-cards", new { Name = "Cartão", Limit = 5000m, ClosingDay = 25, DueDay = 5 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreditCardDto>())!.Id;
    }

    [Fact]
    public async Task Create_PersistsScheduleAndReplayDoesNotDuplicate()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var cardId = await CreateCard(client);
        var route = $"/credit-cards/{cardId}/purchases";
        var command = Command();
        var created = await client.PostAsJsonAsync(route, command);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var purchase = (await created.Content.ReadFromJsonAsync<PurchaseDto>())!;
        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, purchase.Installments.Select(item => item.Amount));
        Assert.Equal(new DateOnly(2026, 3, 5), purchase.Installments[0].DueDate);
        var replay = await client.PostAsJsonAsync(route, command);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayed = (await replay.Content.ReadFromJsonAsync<PurchaseDto>())!;
        Assert.Equal(purchase.Id, replayed.Id);
        Assert.Equal(purchase.Installments, replayed.Installments);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(route, command with { Amount = 101m })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/credit-cards/{cardId}", new { Name = "Mudou", Limit = 3000m, ClosingDay = 10, DueDay = 20 })).StatusCode);
        var list = (await client.GetFromJsonAsync<PurchaseDto[]>(route))!;
        Assert.Single(list);
        Assert.Equal(purchase.Installments, list[0].Installments);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/credit-cards/{cardId}")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentReplay_IsAtomic()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var cardId = await CreateCard(client);
        var route = $"/credit-cards/{cardId}/purchases";
        var command = Command();
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync(route, command)));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK }));
        var list = (await client.GetFromJsonAsync<PurchaseDto[]>(route))!;
        Assert.Single(list);
        Assert.Equal(3, list[0].Installments.Count);
        Assert.Equal(100m, list[0].Installments.Sum(item => item.Amount));
    }

    [Fact]
    public async Task OwnershipAndValidation_PreventUnauthorizedOrPartialWrites()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var cardId = await CreateCard(owner);
        var route = $"/credit-cards/{cardId}/purchases";
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(route, Command())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(route, Command())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/credit-cards/{Guid.NewGuid()}/purchases", Command())).StatusCode);
        var invalid = await owner.PostAsJsonAsync(route, new CreatePurchaseCommand(" ", 0.001m, 0, default, Guid.Empty));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var body = await invalid.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        foreach (var field in new[] { "description", "amount", "installmentCount", "purchaseDate", "idempotencyKey" })
            Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Empty((await owner.GetFromJsonAsync<PurchaseDto[]>(route))!);
        // Same key is scoped to the card, allowing independent purchases on another card.
        var command = Command();
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync(route, command)).StatusCode);
        var secondCard = await CreateCard(owner);
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync($"/credit-cards/{secondCard}/purchases", command)).StatusCode);
    }
}
