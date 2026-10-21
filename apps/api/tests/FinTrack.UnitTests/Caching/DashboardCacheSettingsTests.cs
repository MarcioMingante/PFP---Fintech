using FinTrack.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;

namespace FinTrack.UnitTests.Caching;

public sealed class DashboardCacheSettingsTests
{
    [Fact]
    public void Default_DisablesCacheAndSetsBoundedTtl()
    {
        var settings = DashboardCacheSettings.Load(new ConfigurationBuilder().Build());
        Assert.False(settings.Enabled);
        Assert.Equal(60, settings.TtlSeconds);
        Assert.Null(settings.ConnectionString);
    }
    [Theory]
    [InlineData("0")]
    [InlineData("3601")]
    public void InvalidTtl_IsRejected(string ttl)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DashboardCache:TtlSeconds"] = ttl }).Build();
        Assert.Throws<InvalidOperationException>(() => DashboardCacheSettings.Load(config));
    }
    [Fact]
    public void EnabledCache_RequiresConnectionAndKeysSeparateUserDateRevision()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DashboardCache:Enabled"] = "true" }).Build();
        Assert.Throws<InvalidOperationException>(() => DashboardCacheSettings.Load(config));
        var user = Guid.NewGuid(); var date = new DateOnly(2024, 3, 31);
        var key = RedisDashboardCache.Key(user, date, 1);
        Assert.NotEqual(key, RedisDashboardCache.Key(Guid.NewGuid(), date, 1));
        Assert.NotEqual(key, RedisDashboardCache.Key(user, date.AddDays(1), 1));
        Assert.NotEqual(key, RedisDashboardCache.Key(user, date, 2));
    }
}
