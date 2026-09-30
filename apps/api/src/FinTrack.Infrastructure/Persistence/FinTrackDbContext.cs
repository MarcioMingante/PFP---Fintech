using FinTrack.Domain.Accounts;
using FinTrack.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence;

public sealed class FinTrackDbContext(
  DbContextOptions<FinTrackDbContext> options) : DbContext(options)
{
  public DbSet<Account> Accounts => Set<Account>();

  public DbSet<Category> Categories => Set<Category>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);
  }
}
