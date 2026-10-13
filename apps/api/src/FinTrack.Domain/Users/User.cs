namespace FinTrack.Domain.Users;

public sealed class User
{
    private User() { }
    public User(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
    }
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public void SetPasswordHash(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        PasswordHash = hash;
    }
}
