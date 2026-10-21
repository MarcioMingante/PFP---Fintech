using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinTrack.Application.Authentication;
using FinTrack.Domain.Users;
using FinTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace FinTrack.Infrastructure.Authentication;

internal sealed class AuthService(FinTrackDbContext db, JwtSettings settings) : IAuthService
{
    private static readonly PasswordHasher<User> Hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210000 }));
    private static readonly User DummyUser = new("unknown@fintrack.invalid");
    private static readonly string DummyHash = Hasher.HashPassword(DummyUser, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public async Task<AuthResult> RegisterAsync(Credentials credentials, CancellationToken cancellationToken)
    {
        var user = new User(credentials.Email);
        user.SetPasswordHash(Hasher.HashPassword(user, credentials.Password));
        var refresh = NewRefreshToken();
        var session = new RefreshSession(user.Id, Hash(refresh), DateTimeOffset.UtcNow.AddDays(settings.RefreshDays));
        db.Users.Add(user);
        db.RefreshSessions.Add(session);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_users_email" })
        { return new(AuthStatus.Conflict); }
        return new(AuthStatus.Success, Tokens(user, session, refresh));
    }

    public async Task<AuthResult> LoginAsync(Credentials credentials, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(credentials.Email) || credentials.Email.Length > 254 ||
            string.IsNullOrEmpty(credentials.Password) || credentials.Password.Length > 128)
            return new(AuthStatus.Invalid);
        var email = credentials.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        var verified = Hasher.VerifyHashedPassword(user ?? DummyUser, user?.PasswordHash ?? DummyHash, credentials.Password);
        if (user is null || verified == PasswordVerificationResult.Failed) return new(AuthStatus.Invalid);
        if (verified == PasswordVerificationResult.SuccessRehashNeeded)
            user.SetPasswordHash(Hasher.HashPassword(user, credentials.Password));
        var refresh = NewRefreshToken();
        var session = new RefreshSession(user.Id, Hash(refresh), DateTimeOffset.UtcNow.AddDays(settings.RefreshDays));
        db.RefreshSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new(AuthStatus.Success, Tokens(user, session, refresh));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken) || refreshToken.Length != 88) return new(AuthStatus.Invalid);
        var hash = Hash(refreshToken);
        var session = await db.RefreshSessions.AsNoTracking().SingleOrDefaultAsync(item => item.TokenHash == hash && !item.Revoked && item.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
        if (session is null) return new(AuthStatus.Invalid);
        var refresh = NewRefreshToken();
        // Compare-and-swap guarantees a refresh token can be consumed only once, including concurrent requests.
        var changed = await db.RefreshSessions.Where(item => item.Id == session.Id && item.TokenHash == hash && !item.Revoked && item.ExpiresAt > DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.TokenHash, Hash(refresh)), cancellationToken);
        if (changed != 1) return new(AuthStatus.Invalid);
        var user = await db.Users.SingleAsync(item => item.Id == session.UserId, cancellationToken);
        return new(AuthStatus.Success, Tokens(user, session, refresh));
    }

    public Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken) =>
        db.RefreshSessions.Where(item => item.Id == sessionId && item.UserId == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Revoked, true), cancellationToken);

    private AuthTokens Tokens(User user, RefreshSession session, string refresh)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(settings.AccessMinutes);
        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()), new Claim("sid", session.Id.ToString()),
            new Claim("email", user.Email), new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, refresh, session.ExpiresAt);
    }

    private static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
