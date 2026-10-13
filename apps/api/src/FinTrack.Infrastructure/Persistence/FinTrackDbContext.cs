using FinTrack.Domain.Accounts;
using FinTrack.Domain.Users;
using FinTrack.Application.Authentication;
using FinTrack.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence;

public sealed class FinTrackDbContext(
  DbContextOptions<FinTrackDbContext> options, ICurrentUser? currentUser = null) : DbContext(options)
{
  public DbSet<Account> Accounts => Set<Account>();

  public DbSet<Category> Categories => Set<Category>();

  public DbSet<User> Users => Set<User>();
  public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
  public Guid? CurrentUserId => currentUser?.UserId;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Account>().HasQueryFilter(account => CurrentUserId != null && account.UserId == CurrentUserId);
    modelBuilder.Entity<Category>().HasQueryFilter(category => CurrentUserId != null && category.UserId == CurrentUserId);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);
  }
}
