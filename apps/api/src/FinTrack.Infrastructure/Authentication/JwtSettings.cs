using System.Text;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Infrastructure.Authentication;

public sealed record JwtSettings(string Issuer, string Audience, string SigningKey, int AccessMinutes, int RefreshDays)
{
    public static JwtSettings Load(IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"] ?? "";
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Configure Jwt:SigningKey com pelo menos 32 bytes.");
        var access = configuration.GetValue("Jwt:AccessMinutes", 15);
        var refresh = configuration.GetValue("Jwt:RefreshDays", 7);
        if (access is < 1 or > 60 || refresh is < 1 or > 30)
            throw new InvalidOperationException("Duração de token fora dos limites permitidos.");
        return new(configuration["Jwt:Issuer"] ?? "FinTrack", configuration["Jwt:Audience"] ?? "FinTrack.Client", key, access, refresh);
    }
}
