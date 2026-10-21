using Microsoft.Extensions.Configuration;

namespace FinTrack.Infrastructure.Caching;

public sealed record DashboardCacheSettings(bool Enabled, string? ConnectionString, int TtlSeconds)
{
    public static DashboardCacheSettings Load(IConfiguration configuration)
    {
        var enabled = configuration.GetValue("DashboardCache:Enabled", false);
        var connection = configuration.GetConnectionString("Redis");
        var ttl = configuration.GetValue("DashboardCache:TtlSeconds", 60);
        if (ttl is < 1 or > 3600) throw new InvalidOperationException("DashboardCache:TtlSeconds deve estar entre 1 e 3600.");
        if (enabled && string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:Redis para habilitar cache.");
        return new(enabled, connection, ttl);
    }
}
