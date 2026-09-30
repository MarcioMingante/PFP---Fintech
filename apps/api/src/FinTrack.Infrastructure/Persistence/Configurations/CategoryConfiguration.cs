using FinTrack.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
  public void Configure(EntityTypeBuilder<Category> builder)
  {
    builder.ToTable("categories");

    builder.HasKey(category => category.Id);

    builder.Property(category => category.Id)
      .HasColumnName("id")
      .ValueGeneratedOnAdd();

    builder.Property(category => category.Name)
      .HasColumnName("name")
      .HasMaxLength(100)
      .IsRequired();
  }
}
