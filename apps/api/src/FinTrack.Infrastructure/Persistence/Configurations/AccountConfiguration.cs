using FinTrack.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
  public void Configure(EntityTypeBuilder<Account> builder)
  {
    builder.ToTable("accounts");

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
