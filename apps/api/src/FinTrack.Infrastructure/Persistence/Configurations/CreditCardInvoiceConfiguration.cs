using FinTrack.Domain.CreditCards;
using FinTrack.Domain.Invoices;
using FinTrack.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class CreditCardInvoiceConfiguration : IEntityTypeConfiguration<CreditCardInvoice>
{
    public void Configure(EntityTypeBuilder<CreditCardInvoice> builder)
    {
        builder.ToTable("credit_card_invoices", table =>
            table.HasCheckConstraint("ck_credit_card_invoices_dates", "due_date > closing_date"));
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).HasColumnName("id");
        builder.Property(invoice => invoice.CreditCardId).HasColumnName("credit_card_id");
        builder.Property(invoice => invoice.UserId).HasColumnName("user_id");
        builder.Property(invoice => invoice.ClosingDate).HasColumnName("closing_date");
        builder.Property(invoice => invoice.DueDate).HasColumnName("due_date");
        builder.HasIndex(invoice => new { invoice.CreditCardId, invoice.ClosingDate, invoice.DueDate }).IsUnique()
            .HasDatabaseName("ux_credit_card_invoices_cycle");
        builder.HasOne<CreditCard>().WithMany().HasForeignKey(invoice => new { invoice.CreditCardId, invoice.UserId })
            .HasPrincipalKey(card => new { card.Id, card.UserId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany<Installment>().WithOne().HasForeignKey(item => item.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
