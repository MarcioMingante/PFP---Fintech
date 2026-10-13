namespace FinTrack.Application.Authentication;

public sealed record Credentials(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthTokens(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);
public enum AuthStatus { Success, Invalid, Conflict }
public sealed record AuthResult(AuthStatus Status, AuthTokens? Tokens = null);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(Credentials credentials, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(Credentials credentials, CancellationToken cancellationToken);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
