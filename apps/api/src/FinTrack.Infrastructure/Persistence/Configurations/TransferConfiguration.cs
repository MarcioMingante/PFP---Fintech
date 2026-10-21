using FinTrack.Domain.Transfers;
using FinTrack.Domain.Accounts;
using FinTrack.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", table =>
        {
            table.HasCheckConstraint("ck_transfers_amount", "amount > 0");
            table.HasCheckConstraint("ck_transfers_accounts", "source_account_id <> destination_account_id");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.SourceAccountId).HasColumnName("source_account_id");
        builder.Property(item => item.DestinationAccountId).HasColumnName("destination_account_id");
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(item => item.OccurredOn).HasColumnName("occurred_on");
        builder.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key");
        builder.HasIndex(item => new { item.UserId, item.IdempotencyKey }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(item => item.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(item => item.DestinationAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
