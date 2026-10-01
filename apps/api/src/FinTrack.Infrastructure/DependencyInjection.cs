using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Application.Accounts;
using FinTrack.Infrastructure.Persistence.Repositories;

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

    services.AddScoped<IAccountRepository, AccountRepository>();

    return services;
  }
}
