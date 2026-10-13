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

  public DbSet<Account> Accounts => Set<Account>();

  public DbSet<Category> Categories => Set<Category>();

  public DbSet<User> Users => Set<User>();
  public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
  public Guid? CurrentUserId => currentUser?.UserId;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<FinTrack.Domain.CreditCards.CreditCard>().HasQueryFilter(card => CurrentUserId != null && card.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Purchases.Purchase>().HasQueryFilter(purchase => CurrentUserId != null && purchase.UserId == CurrentUserId);
    modelBuilder.Entity<FinTrack.Domain.Purchases.Installment>().HasQueryFilter(installment => Purchases.Any(purchase => purchase.Id == installment.PurchaseId));
    modelBuilder.Entity<FinTrack.Domain.Invoices.CreditCardInvoice>().HasQueryFilter(invoice => CurrentUserId != null && invoice.UserId == CurrentUserId);
    modelBuilder.Entity<Account>().HasQueryFilter(account => CurrentUserId != null && account.UserId == CurrentUserId);
    modelBuilder.Entity<Category>().HasQueryFilter(category => CurrentUserId != null && category.UserId == CurrentUserId);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);
  }
}
