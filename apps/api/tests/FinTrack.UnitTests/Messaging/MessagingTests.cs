using FinTrack.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinTrack.UnitTests.Messaging;

public sealed class MessagingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Registration_OnlyStartsWorkerWhenEnabled(bool enabled)
    {
        var services = new ServiceCollection();
        services.AddMessaging(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Enabled"] = enabled.ToString(), ["ConnectionStrings:RabbitMq"] = "amqp://test:password@localhost:5672/"
        }).Build());
        Assert.Equal(enabled, services.Any(item => item.ServiceType == typeof(IHostedService) && item.ImplementationType == typeof(MessagingWorker)));
    }
    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost")]
    [InlineData("invalid")]
    public void EnabledWorker_RequiresValidBrokerUri(string? uri)
    {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddMessaging(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Messaging:Enabled"] = "true", ["ConnectionStrings:RabbitMq"] = uri }).Build()));
    }
}
