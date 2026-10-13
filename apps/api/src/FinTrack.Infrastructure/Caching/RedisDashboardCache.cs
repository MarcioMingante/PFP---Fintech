using System.Text.Json;
using FinTrack.Application.Dashboard;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FinTrack.Infrastructure.Caching;

public sealed class RedisDashboardCache(IConnectionMultiplexer redis, DashboardCacheSettings settings,
    ILogger<RedisDashboardCache> logger) : IDashboardCache
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public bool Enabled => true;
    public static string Key(Guid userId, DateOnly date, long revision)
        => $"fintrack:dashboard:v1:{userId:N}:{date:yyyy-MM-dd}:{revision}";

    public async Task<DashboardSummaryDto?> GetAsync(Guid userId, DateOnly date, long revision, CancellationToken cancellationToken)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key(userId, date, revision)).WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty) { logger.LogDebug("Dashboard cache miss."); return null; }
            var result = JsonSerializer.Deserialize<DashboardSummaryDto>((string)value!, Json);
            if (result?.AsOf != date || result.MonthlyHistory?.Count != 12 || result.Forecast?.Count != 3
                || result.Invoices is null || result.ExpensesByCategory is null) return null;
            logger.LogDebug("Dashboard cache hit.");
            return result;
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException) { logger.LogWarning("Redis indisponível; dashboard consultará PostgreSQL."); return null; }
        catch (JsonException) { logger.LogWarning("Entrada inválida no cache do dashboard; resultado será recalculado."); return null; }
    }
    public async Task SetAsync(Guid userId, DateOnly date, long revision, DashboardSummaryDto summary, CancellationToken cancellationToken)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(Key(userId, date, revision), JsonSerializer.Serialize(summary, Json),
                TimeSpan.FromSeconds(settings.TtlSeconds)).WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException) { logger.LogWarning("Redis indisponível; resultado do dashboard não foi armazenado."); }
    }
}
