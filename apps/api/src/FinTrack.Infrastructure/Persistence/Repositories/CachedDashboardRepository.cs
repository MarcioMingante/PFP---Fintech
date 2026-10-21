using FinTrack.Application.Authentication;
using FinTrack.Application.Dashboard;
using FinTrack.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence.Repositories;

public sealed class CachedDashboardRepository(DashboardRepository source, FinTrackDbContext database,
    ICurrentUser user, IDashboardCache cache) : IDashboardRepository
{
    public async Task<DashboardSummaryDto> SummaryAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var userId = user.UserId ?? throw new UnauthorizedAccessException();
        if (!cache.Enabled) return await source.SummaryAsync(asOf, cancellationToken);
        // PostgreSQL is authoritative, including writes performed while Redis was offline.
        var revision = await database.DashboardCacheRevisions.AsNoTracking()
            .Where(item => item.UserId == userId).Select(item => item.Revision).SingleOrDefaultAsync(cancellationToken);
        var cached = await cache.GetAsync(userId, asOf, revision, cancellationToken);
        if (cached is not null) return cached;
        var summary = await source.SummaryAsync(asOf, cancellationToken);
        // A concurrent commit uses another revision and cannot see this older key.
        await cache.SetAsync(userId, asOf, revision, summary, cancellationToken);
        return summary;
    }
}
