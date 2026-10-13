namespace FinTrack.Infrastructure.Persistence;

public sealed class DashboardCacheRevision
{
    public Guid UserId { get; private set; }
    public long Revision { get; private set; }
}
