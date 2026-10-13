using FinTrack.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

internal sealed class DashboardCacheRevisionConfiguration : IEntityTypeConfiguration<DashboardCacheRevision>
{
    public void Configure(EntityTypeBuilder<DashboardCacheRevision> builder)
    {
        builder.ToTable("dashboard_cache_revisions", table => table.HasCheckConstraint("ck_dashboard_cache_revision", "revision > 0"));
        builder.HasKey(item => item.UserId);
        builder.Property(item => item.UserId).HasColumnName("user_id");
        builder.Property(item => item.Revision).HasColumnName("revision");
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
