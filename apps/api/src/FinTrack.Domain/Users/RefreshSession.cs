namespace FinTrack.Domain.Users;

public sealed class RefreshSession
{
    private RefreshSession() { }
    public RefreshSession(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuário obrigatório.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool Revoked { get; private set; }
}
