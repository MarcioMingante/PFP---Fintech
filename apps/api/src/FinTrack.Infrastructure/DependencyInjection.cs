using FinTrack.Infrastructure.Caching;
using StackExchange.Redis;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Application.Accounts;
using FinTrack.Infrastructure.Persistence.Repositories;
using FinTrack.Application.Categories;

namespace FinTrack.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration
  )
  {
    var connectionString = configuration.GetConnectionString("Database")
      ?? throw new InvalidOperationException("Connection string 'Database' não foi congigurada.");
    
    services.AddDbContext<FinTrackDbContext>(options =>
      options.UseNpgsql(connectionString));

    services.AddScoped<FinTrack.Application.CreditCards.ICreditCardRepository, CreditCardRepository>();
    services.AddScoped<FinTrack.Application.Purchases.IPurchaseRepository, PurchaseRepository>();
    services.AddScoped<FinTrack.Application.Invoices.IInvoiceRepository, InvoiceRepository>();
    services.AddScoped<RecurringTransactionRepository>();
    services.AddScoped<FinTrack.Application.Recurring.IRecurringTransactionRepository>(provider => provider.GetRequiredService<RecurringTransactionRepository>());
    services.AddScoped<DashboardRepository>();
    services.AddScoped<FinTrack.Application.Dashboard.IDashboardRepository, CachedDashboardRepository>();
    var cacheSettings = DashboardCacheSettings.Load(configuration);
    services.AddSingleton(cacheSettings);
    if (cacheSettings.Enabled)
    {
      services.AddSingleton<IConnectionMultiplexer>(_ =>
      {
        var options = ConfigurationOptions.Parse(cacheSettings.ConnectionString!);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 1000;
        options.ConnectRetry = 0;
        options.AsyncTimeout = 500;
        options.SyncTimeout = 500;
        options.BacklogPolicy = BacklogPolicy.FailFast;
        return ConnectionMultiplexer.Connect(options);
      });
      services.AddSingleton<IDashboardCache, RedisDashboardCache>();
    }
    else services.AddSingleton<IDashboardCache, DisabledDashboardCache>();
    services.AddScoped<FinTrack.Application.Transactions.ITransactionRepository, TransactionRepository>();
    services.AddScoped<FinTrack.Application.Transfers.ITransferRepository, TransferRepository>();
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<ICategoryRepository, CategoryRepository>();

    return services;
  }
}
