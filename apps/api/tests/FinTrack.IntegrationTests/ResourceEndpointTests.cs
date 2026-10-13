using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.IntegrationTests.Fixtures;

namespace FinTrack.IntegrationTests;

public sealed class ResourceEndpointTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    private sealed record Resource(Guid Id, string Name);

    [PostgresTheory]
    [InlineData("/accounts")]
    [InlineData("/categories")]
    public async Task CreateReadUpdate_PersistsAcrossRequests(string route)
    {
        using var client = fixture.CreateClient();
        using var created = await client.PostAsJsonAsync(route, new { Name = "  Original  " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var resource = (await created.Content.ReadFromJsonAsync<Resource>())!;
        Assert.NotEqual(Guid.Empty, resource.Id);
        Assert.Equal("Original", resource.Name);
        Assert.Equal($"{route}/{resource.Id}", created.Headers.Location?.OriginalString);

        // A new client/request uses a different scoped DbContext: verify database persistence.
        using var reader = fixture.CreateClient();
        Assert.Equal(resource, await reader.GetFromJsonAsync<Resource>($"{route}/{resource.Id}"));
        Assert.Contains(resource, (await reader.GetFromJsonAsync<Resource[]>(route))!);

        using var updated = await client.PutAsJsonAsync($"{route}/{resource.Id}", new { Name = "  Atualizado  " });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(new Resource(resource.Id, "Atualizado"), await reader.GetFromJsonAsync<Resource>($"{route}/{resource.Id}"));
    }

    [PostgresTheory]
    [InlineData("/accounts")]
    [InlineData("/categories")]
    public async Task InvalidCreateAndUpdate_ReturnValidationProblemWithoutChangingData(string route)
    {
        using var client = fixture.CreateClient();
        var before = (await client.GetFromJsonAsync<Resource[]>(route))!;
        using var invalidCreate = await client.PostAsJsonAsync(route, new { Name = " " });
        await AssertNameValidationAsync(invalidCreate);
        Assert.Equal(before.Length, (await client.GetFromJsonAsync<Resource[]>(route))!.Length);

        using var created = await client.PostAsJsonAsync(route, new { Name = "Preservado" });
        var resource = (await created.Content.ReadFromJsonAsync<Resource>())!;
        using var invalidUpdate = await client.PutAsJsonAsync($"{route}/{resource.Id}", new { Name = " " });
        await AssertNameValidationAsync(invalidUpdate);
        Assert.Equal(resource, await client.GetFromJsonAsync<Resource>($"{route}/{resource.Id}"));
    }

    [PostgresTheory]
    [InlineData("/accounts")]
    [InlineData("/categories")]
    public async Task MissingResource_GetAndUpdateReturnNotFound(string route)
    {
        using var client = fixture.CreateClient();
        var url = $"{route}/{Guid.NewGuid()}";
        using var get = await client.GetAsync(url);
        using var update = await client.PutAsJsonAsync(url, new { Name = "Válido" });
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [PostgresFact]
    public async Task DeleteAccount_RemovesPersistedAccountAndRepeatedDeleteReturnsNotFound()
    {
        using var client = fixture.CreateClient();
        using var created = await client.PostAsJsonAsync("/accounts", new { Name = "Excluir" });
        var resource = (await created.Content.ReadFromJsonAsync<Resource>())!;
        var url = $"/accounts/{resource.Id}";
        using var deleted = await client.DeleteAsync(url);
        using var get = await client.GetAsync(url);
        using var repeated = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<Resource[]>("/accounts"))!, item => item.Id == resource.Id);
    }

    private static async Task AssertNameValidationAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("name", out var errors));
        Assert.NotEmpty(errors.EnumerateArray());
    }
}
