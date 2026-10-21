using FinTrack.Application.Dashboard;

namespace FinTrack.Infrastructure.Caching;

public interface IDashboardCache
{
    bool Enabled { get; }
    Task<DashboardSummaryDto?> GetAsync(Guid userId, DateOnly date, long revision, CancellationToken cancellationToken);
    Task SetAsync(Guid userId, DateOnly date, long revision, DashboardSummaryDto summary, CancellationToken cancellationToken);
}
public sealed class DisabledDashboardCache : IDashboardCache
{
    public bool Enabled => false;
    public Task<DashboardSummaryDto?> GetAsync(Guid userId, DateOnly date, long revision, CancellationToken cancellationToken)
        => Task.FromResult<DashboardSummaryDto?>(null);
    public Task SetAsync(Guid userId, DateOnly date, long revision, DashboardSummaryDto summary, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
