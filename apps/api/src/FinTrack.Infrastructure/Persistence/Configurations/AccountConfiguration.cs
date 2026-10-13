using FinTrack.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
  public void Configure(EntityTypeBuilder<Account> builder)
  {
    builder.ToTable("accounts");

    builder.Property(item => item.UserId).HasColumnName("user_id");
    builder.HasOne<FinTrack.Domain.Users.User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);

    builder.HasKey(account => account.Id);

    builder.Property(account => account.Id)
      .HasColumnName("id")
      .ValueGeneratedOnAdd();

    builder.Property(account => account.Name)
      .HasColumnName("name")
      .HasMaxLength(100)
      .IsRequired();
  }
}
