using FinTrack.Domain.CreditCards;
using FinTrack.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases", table =>
        {
            table.HasCheckConstraint("ck_purchases_amount", "\"Amount\" > 0");
            table.HasCheckConstraint("ck_purchases_count", "\"InstallmentCount\" BETWEEN 1 AND 120");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Description).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.HasIndex(item => new { item.CreditCardId, item.IdempotencyKey }).IsUnique();
        builder.HasOne<CreditCard>().WithMany().HasForeignKey(item => new { item.CreditCardId, item.UserId })
            .HasPrincipalKey(item => new { item.Id, item.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Installments).WithOne().HasForeignKey(item => item.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Installments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("installments", table =>
        {
            table.HasCheckConstraint("ck_installments_amount", "\"Amount\" > 0");
            table.HasCheckConstraint("ck_installments_number", "\"Number\" BETWEEN 1 AND 120");
            table.HasCheckConstraint("ck_installments_dates", "\"DueDate\" > \"ClosingDate\"");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.HasIndex(item => new { item.PurchaseId, item.Number }).IsUnique();
    }
}
