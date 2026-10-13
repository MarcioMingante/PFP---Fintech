using System.Diagnostics;
using System.Text.Json;
using FinTrack.Infrastructure.Messaging;
using FinTrack.Domain.Accounts;
using FinTrack.Domain.Users;
using FinTrack.Application.Authentication;
using FinTrack.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence;

public sealed class FinTrackDbContext(
  DbContextOptions<FinTrackDbContext> options, ICurrentUser? currentUser = null) : DbContext(options)
{
  public DbSet<FinTrack.Domain.CreditCards.CreditCard> CreditCards => Set<FinTrack.Domain.CreditCards.CreditCard>();

  public DbSet<FinTrack.Domain.Purchases.Purchase> Purchases => Set<FinTrack.Domain.Purchases.Purchase>();

  public DbSet<FinTrack.Domain.Invoices.CreditCardInvoice> CreditCardInvoices => Set<FinTrack.Domain.Invoices.CreditCardInvoice>();

  public DbSet<FinTrack.Domain.Recurring.RecurringTransaction> RecurringTransactions => Set<FinTrack.Domain.Recurring.RecurringTransaction>();
  public DbSet<FinTrack.Domain.Transactions.Transaction> Transactions => Set<FinTrack.Domain.Transactions.Transaction>();

  public DbSet<DashboardCacheRevision> DashboardCacheRevisions => Set<DashboardCacheRevision>();

  public DbSet<Account> Accounts => Set<Account>();

  public DbSet<Category> Categories => Set<Category>();

  public DbSet<User> Users => Set<User>();
  public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
  public Guid? CurrentUserId => currentUser?.UserId;

  private Guid[] ChangedFinancialOwners() => ChangeTracker.Entries()
    .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
    .Select(entry => entry.Entity switch
    {
      Account account => account.UserId,
      Category category => category.UserId,
      FinTrack.Domain.CreditCards.CreditCard card => card.UserId,
      FinTrack.Domain.Purchases.Purchase purchase => purchase.UserId,
      FinTrack.Domain.Purchases.Installment => CurrentUserId,
      FinTrack.Domain.Invoices.CreditCardInvoice invoice => invoice.UserId,
      FinTrack.Domain.Recurring.RecurringTransaction recurring => recurring.UserId,
      FinTrack.Domain.Transactions.Transaction transaction => transaction.UserId,
      _ => (Guid?)null
    }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().Order().ToArray();

  private TransactionCreated[] NewEvents() => ChangeTracker.Entries<FinTrack.Domain.Transactions.Transaction>()
    .Where(entry => entry.State == EntityState.Added).Select(entry => TransactionCreated.From(entry.Entity)).ToArray();

  public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    var owners = ChangedFinancialOwners();
    var events = NewEvents();
    if (owners.Length == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    await using var transaction = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;
    var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    foreach (var owner in owners)
      await Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO dashboard_cache_revisions (user_id, revision) VALUES ({owner}, 1)
        ON CONFLICT (user_id) DO UPDATE SET revision = dashboard_cache_revisions.revision + 1
        """, cancellationToken);
    foreach (var item in events)
      await Database.ExecuteSqlInterpolatedAsync($"INSERT INTO messaging_outbox (event_id, payload, trace_parent, trace_state) VALUES ({item.EventId}, {JsonSerializer.Serialize(item)}::jsonb, {(Activity.Current != null && Activity.Current.IdFormat == ActivityIdFormat.W3C ? Activity.Current.Id : null)}, {Activity.Current?.TraceStateString}) ON CONFLICT DO NOTHING", cancellationToken);
    if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    return result;
  }

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    var owners = ChangedFinancialOwners();
    var events = NewEvents();
    if (owners.Length == 0) return base.SaveChanges(acceptAllChangesOnSuccess);
    using var transaction = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
    var result = base.SaveChanges(acceptAllChangesOnSuccess);
    foreach (var owner in owners)
      Database.ExecuteSqlInterpolated($"""
        INSERT INTO dashboard_cache_revisions (user_id, revision) VALUES ({owner}, 1)
        ON CONFLICT (user_id) DO UPDATE SET revision = dashboard_cache_revisions.revision + 1
        """);
    foreach (var item in events)
      Database.ExecuteSqlInterpolated($"INSERT INTO messaging_outbox (event_id, payload, trace_parent, trace_state) VALUES ({item.EventId}, {JsonSerializer.Serialize(item)}::jsonb, {(Activity.Current != null && Activity.Current.IdFormat == ActivityIdFormat.W3C ? Activity.Current.Id : null)}, {Activity.Current?.TraceStateString}) ON CONFLICT DO NOTHING");
    transaction?.Commit();
    return result;
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<FinTrack.Domain.CreditCards.CreditCard>().HasQueryFilter(card => CurrentUserId != null && card.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Purchases.Purchase>().HasQueryFilter(purchase => CurrentUserId != null && purchase.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Purchases.Installment>().HasQueryFilter(installment => Purchases.Any(purchase => purchase.Id == installment.PurchaseId));
    modelBuilder.Entity<FinTrack.Domain.Invoices.CreditCardInvoice>().HasQueryFilter(invoice => CurrentUserId != null && invoice.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Recurring.RecurringTransaction>().HasQueryFilter(item => CurrentUserId != null && item.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Transactions.Transaction>().HasQueryFilter(item => CurrentUserId != null && item.UserId == CurrentUserId);
    modelBuilder.Entity<DashboardCacheRevision>().HasQueryFilter(item => CurrentUserId != null && item.UserId == CurrentUserId);
    modelBuilder.Entity<Account>().HasQueryFilter(account => CurrentUserId != null && account.UserId == CurrentUserId);
    modelBuilder.Entity<Category>().HasQueryFilter(category => CurrentUserId != null && category.UserId == CurrentUserId);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);
  }
}
