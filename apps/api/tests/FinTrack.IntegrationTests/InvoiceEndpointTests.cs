using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.CreditCards.DTOs;
using FinTrack.Application.Invoices;
using FinTrack.Application.Purchases;
using FinTrack.Domain.Purchases;
using FinTrack.Infrastructure.Persistence;
using FinTrack.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.IntegrationTests;

public sealed class InvoiceEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private static async Task<Guid> Card(HttpClient client, decimal limit = 5000m)
    {
        var created = await client.PostAsJsonAsync("/credit-cards", new { Name = "Principal", Limit = limit, ClosingDay = 25, DueDay = 5 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<CreditCardDto>())!.Id;
    }
    private static async Task<PurchaseDto> Buy(HttpClient client, Guid cardId, decimal amount, int count, DateOnly date)
    {
        var created = await client.PostAsJsonAsync($"/credit-cards/{cardId}/purchases", new CreatePurchaseCommand("Compra", amount, count, date, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<PurchaseDto>())!;
    }

    [PostgresFact]
    public async Task Invoices_GroupInstallmentsAndExposeTotalsCurrentFutureAndLimit()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var card = await Card(client);
        var first = await Buy(client, card, 100m, 3, new(2026, 1, 24));
        var second = await Buy(client, card, 20m, 1, new(2026, 1, 1));
        var route = $"/credit-cards/{card}/invoices";
        var all = (await client.GetFromJsonAsync<InvoiceDto[]>(route))!;
        Assert.Equal(3, all.Length);
        Assert.Equal(120m, all.Sum(item => item.Total));
        Assert.Equal(53.34m, all[0].Total);
        Assert.Equal(2, all[0].Items.Count);
        Assert.Equal(first.Installments[0].InvoiceId, second.Installments[0].InvoiceId);
        Assert.Equal(first.Installments[0].InvoiceId, all[0].Id);
        var detail = (await client.GetFromJsonAsync<InvoiceDto>($"{route}/{all[0].Id}"))!;
        Assert.Equal(all[0].Items, detail.Items);
        Assert.Equal(all[0].Total, detail.Total);
        var summary = (await client.GetFromJsonAsync<CreditCardInvoiceSummaryDto>($"{route}/summary?asOf=2026-01-24"))!;
        Assert.Equal(all[0].Id, summary.CurrentInvoice.Id);
        Assert.Equal(2, summary.FutureInvoices.Count);
        Assert.Equal(120m, summary.UsedLimit);
        Assert.Equal(4880m, summary.AvailableLimit);
        Assert.Equal(new DateOnly(2026, 2, 5), summary.CurrentInvoice.DueDate);
        var atClosing = (await client.GetFromJsonAsync<CreditCardInvoiceSummaryDto>($"{route}/summary?asOf=2026-01-25"))!;
        Assert.Equal(all[1].Id, atClosing.CurrentInvoice.Id);
        Assert.Single(atClosing.FutureInvoices);
        Assert.Equal(120m, atClosing.UsedLimit); // Closed invoices are not automatically paid.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{route}/{Guid.NewGuid()}")).StatusCode);
    }

    [PostgresFact]
    public async Task EmptyCycle_IsReadOnlyAndLimitCanShowExcess()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var card = await Card(client, 50m);
        var route = $"/credit-cards/{card}/invoices";
        var empty = (await client.GetFromJsonAsync<CreditCardInvoiceSummaryDto>($"{route}/summary?asOf=2024-02-29"))!;
        Assert.Null(empty.CurrentInvoice.Id);
        Assert.Equal(0m, empty.CurrentInvoice.Total);
        Assert.Empty(empty.FutureInvoices);
        Assert.Equal(50m, empty.AvailableLimit);
        Assert.Equal(new DateOnly(2024, 3, 25), empty.CurrentInvoice.ClosingDate);
        Assert.Empty((await client.GetFromJsonAsync<InvoiceDto[]>(route))!);
        await Buy(client, card, 100m, 1, new(2026, 1, 1));
        var later = (await client.GetFromJsonAsync<CreditCardInvoiceSummaryDto>($"{route}/summary?asOf=2027-01-01"))!;
        Assert.Equal(-50m, later.AvailableLimit);
        Assert.Equal(100m, later.UsedLimit);
        Assert.Equal(0m, later.CurrentInvoice.Total);
        Assert.Empty(later.FutureInvoices);
        Assert.Single((await client.GetFromJsonAsync<InvoiceDto[]>(route))!);
    }

    [PostgresFact]
    public async Task CalendarChange_PreservesInvoicesAndCreatesSeparateSnapshotCycles()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var card = await Card(client);
        var original = await Buy(client, card, 50m, 2, new(2026, 1, 1));
        var update = await client.PutAsJsonAsync($"/credit-cards/{card}", new { Name = "Principal", Limit = 5000m, ClosingDay = 25, DueDay = 10 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        await Buy(client, card, 10m, 1, new(2026, 1, 2));
        var all = (await client.GetFromJsonAsync<InvoiceDto[]>($"/credit-cards/{card}/invoices"))!;
        Assert.Equal(3, all.Length);
        var previous = Assert.Single(all, invoice => invoice.Id == original.Installments[0].InvoiceId);
        Assert.Equal(new DateOnly(2026, 2, 5), previous.DueDate);
        Assert.Contains(all, invoice => invoice.DueDate == new DateOnly(2026, 2, 10));
        var summary = (await client.GetFromJsonAsync<CreditCardInvoiceSummaryDto>($"/credit-cards/{card}/invoices/summary?asOf=2026-01-20"))!;
        Assert.Equal(previous.Id, summary.CurrentInvoice.Id);
        Assert.Equal(2, summary.FutureInvoices.Count);
        Assert.Equal(60m, summary.UsedLimit);
    }

    [PostgresFact]
    public async Task ConcurrentDistinctPurchases_ShareOneInvoiceWithoutLosingAmounts()
    {
        using var client = await fixture.CreateAuthenticatedClientAsync();
        var card = await Card(client);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Buy(client, card, .01m, 1, new(2026, 1, 1))));
        var invoices = (await client.GetFromJsonAsync<InvoiceDto[]>($"/credit-cards/{card}/invoices"))!;
        var invoice = Assert.Single(invoices);
        Assert.Equal(.08m, invoice.Total);
        Assert.Equal(8, invoice.Items.Count);
        Assert.Equal(8, invoice.Items.Select(item => item.InstallmentId).Distinct().Count());
    }

    [PostgresFact]
    public async Task Endpoints_ProtectOwnershipAndValidateReferenceDate()
    {
        using var owner = await fixture.CreateAuthenticatedClientAsync();
        using var other = await fixture.CreateAuthenticatedClientAsync();
        using var anonymous = fixture.CreateClient();
        var card = await Card(owner);
        var purchase = await Buy(owner, card, 1m, 1, new(2026, 1, 1));
        var route = $"/credit-cards/{card}/invoices";
        foreach (var path in new[] { route, $"{route}/summary", $"{route}/{purchase.Installments[0].InvoiceId}" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        }
        var otherCard = await Card(owner);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/credit-cards/{otherCard}/invoices/{purchase.Installments[0].InvoiceId}")).StatusCode);
        foreach (var date in new[] { "0001-01-01", "9999-12-31", "invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"{route}/summary?asOf={date}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/credit-cards/{Guid.NewGuid()}/invoices")).StatusCode);
    }

    [PostgresFact]
    public async Task Migration_BackfillsPhase15DataAndReapplicationIsIdempotent()
    {
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var invoices = await database.CreditCardInvoices.IgnoreQueryFilters().Where(item => item.CreditCardId == PostgresApiFixture.LegacyCardId).ToListAsync();
        Assert.Equal(2, invoices.Count);
        var purchaseIds = await database.Purchases.IgnoreQueryFilters().Where(item => item.CreditCardId == PostgresApiFixture.LegacyCardId).Select(item => item.Id).ToListAsync();
        var installments = await database.Set<Installment>().IgnoreQueryFilters().Where(item => purchaseIds.Contains(item.PurchaseId)).ToListAsync();
        Assert.Equal(3, installments.Count);
        Assert.Equal(120m, installments.Sum(item => item.Amount));
        Assert.All(installments, item => Assert.Contains(invoices, invoice => invoice.Id == item.InvoiceId
            && invoice.ClosingDate == item.ClosingDate && invoice.DueDate == item.DueDate));
        Assert.Equal(70m, installments.Where(item => item.ClosingDate == new DateOnly(2026, 1, 25)).Sum(item => item.Amount));
        await database.Database.MigrateAsync();
        Assert.Equal(2, await database.CreditCardInvoices.IgnoreQueryFilters().CountAsync(item => item.CreditCardId == PostgresApiFixture.LegacyCardId));
    }
}
