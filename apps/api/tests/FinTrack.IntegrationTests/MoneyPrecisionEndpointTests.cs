using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class MoneyPrecisionEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    [Fact]
    public async Task DecimalStringsRoundTripAtDatabaseLimitAndAggregatesStayExact()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var account = (await (await client.PostAsJsonAsync("/accounts", new { name = "Exata" })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        foreach (var amount in new[] { "90071992547409.91", "9999999999999999.99" })
        {
            var response = await client.PostAsJsonAsync("/transactions", new { accountId = account, description = "Precisão", amount, type = "Income", occurredOn = "2024-03-05" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(amount, body.GetProperty("amount").GetString());
        }
        var summary = await client.GetFromJsonAsync<JsonElement>("/dashboard/summary?asOf=2024-03-31");
        Assert.Equal("10090071992547409.90", summary.GetProperty("currentBalance").GetString());
        var balances = await client.GetFromJsonAsync<JsonElement>("/accounts/balances");
        Assert.Equal("10090071992547409.90", balances[0].GetProperty("balance").GetString());
        var cardResponse = await client.PostAsJsonAsync("/credit-cards", new { name = "Exato", limit = "9999999999999999.99", closingDay = 25, dueDay = 5 });
        Assert.Equal(HttpStatusCode.Created, cardResponse.StatusCode);
        var card = await cardResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("9999999999999999.99", card.GetProperty("limit").GetString());
        var cardId = card.GetProperty("id").GetGuid();
        var purchaseResponse = await client.PostAsJsonAsync($"/credit-cards/{cardId}/purchases", new { description = "Exata", amount = "90071992547409.91", installmentCount = 3, purchaseDate = "2024-01-25", idempotencyKey = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);
        var invoices = await client.GetFromJsonAsync<JsonElement>($"/credit-cards/{cardId}/invoices");
        Assert.Equal(90071992547409.91m, invoices.EnumerateArray().Sum(item => decimal.Parse(item.GetProperty("total").GetString()!, System.Globalization.CultureInfo.InvariantCulture)));
        foreach (var invalid in new[] { "1.001", "1e2", "NaN", "10000000000000000.00", "0", "-1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/transactions", new { accountId = account, description = "Inválida", amount = invalid, type = "Income", occurredOn = "2024-03-05" })).StatusCode);
        // Numeric clients remain accepted, but exact decimal strings are the public recommendation.
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/transactions", new { accountId = account, description = "Legado", amount = .01m, type = "Expense", occurredOn = "2024-03-05" })).StatusCode);
    }
}
