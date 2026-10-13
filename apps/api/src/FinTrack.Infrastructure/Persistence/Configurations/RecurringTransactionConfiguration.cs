using FinTrack.Domain.Accounts;
using FinTrack.Domain.Categories;
using FinTrack.Domain.Recurring;
using FinTrack.Domain.Transactions;
using FinTrack.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.ToTable("recurring_transactions", table =>
        {
            table.HasCheckConstraint("ck_recurring_amount", "amount > 0");
            table.HasCheckConstraint("ck_recurring_type", "type IN ('Income', 'Expense')");
            table.HasCheckConstraint("ck_recurring_dates", "end_date IS NULL OR end_date >= start_date");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.AccountId).HasColumnName("account_id");
        builder.Property(item => item.CategoryId).HasColumnName("category_id");
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(10);
        builder.Property(item => item.StartDate).HasColumnName("start_date");
        builder.Property(item => item.EndDate).HasColumnName("end_date");
        builder.Property(item => item.NextOccurrenceDate).HasColumnName("next_occurrence_date");
        builder.Property(item => item.IsActive).HasColumnName("is_active");
        builder.HasIndex(item => new { item.UserId, item.IsActive, item.NextOccurrenceDate });
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(item => item.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", table =>
        {
            table.HasCheckConstraint("ck_transactions_amount", "amount > 0");
            table.HasCheckConstraint("ck_transactions_type", "type IN ('Income', 'Expense')");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.AccountId).HasColumnName("account_id");
        builder.Property(item => item.CategoryId).HasColumnName("category_id");
        builder.Property(item => item.RecurringTransactionId).HasColumnName("recurring_transaction_id");
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(10);
        builder.Property(item => item.OccurredOn).HasColumnName("occurred_on");
        builder.HasIndex(item => new { item.RecurringTransactionId, item.OccurredOn }).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.OccurredOn });
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(item => item.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RecurringTransaction>().WithMany().HasForeignKey(item => item.RecurringTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
