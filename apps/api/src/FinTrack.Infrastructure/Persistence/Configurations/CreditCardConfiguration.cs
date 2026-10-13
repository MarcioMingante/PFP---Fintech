using FinTrack.Domain.CreditCards;
using FinTrack.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards", table =>
        {
            table.HasCheckConstraint("ck_credit_cards_limit", "\"limit\" > 0");
            table.HasCheckConstraint("ck_credit_cards_closing_day", "closing_day BETWEEN 1 AND 31");
            table.HasCheckConstraint("ck_credit_cards_due_day", "due_day BETWEEN 1 AND 31");
        });
        builder.HasKey(card => card.Id);
        builder.Property(card => card.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(card => card.UserId).HasColumnName("user_id").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(card => card.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(card => card.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(card => card.Limit).HasColumnName("limit").HasPrecision(18, 2);
        builder.Property(card => card.ClosingDay).HasColumnName("closing_day");
        builder.Property(card => card.DueDay).HasColumnName("due_day");
    }
}
