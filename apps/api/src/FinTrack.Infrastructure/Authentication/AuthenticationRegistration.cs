using System.Text;
using FinTrack.Application.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FinTrack.Infrastructure.Persistence;

namespace FinTrack.Infrastructure.Authentication;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddFinTrackAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = JwtSettings.Load(configuration);
        services.AddSingleton(settings);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = settings.Issuer,
                ValidateAudience = true, ValidAudience = settings.Audience,
                ValidateLifetime = true, RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero,
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var userId) ||
                        !Guid.TryParse(context.Principal?.FindFirst("sid")?.Value, out var sessionId))
                    { context.Fail("Token inválido."); return; }
                    var db = context.HttpContext.RequestServices.GetRequiredService<FinTrackDbContext>();
                    var active = await db.RefreshSessions.AnyAsync(session => session.Id == sessionId && session.UserId == userId && !session.Revoked && session.ExpiresAt > DateTimeOffset.UtcNow, context.HttpContext.RequestAborted);
                    if (!active) context.Fail("Sessão encerrada.");
                }
            };
        });
        services.AddAuthorization();
        return services;
    }
}
