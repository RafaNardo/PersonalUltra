using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PersonalUltra.Api.Infrastructure;

/// <summary>
/// Development-only, signed bearer sessions. This is deliberately not an
/// authentication mechanism for production; the handler rejects it outside
/// Development and official authentication will replace it in M2.
/// </summary>
public sealed class DemoSessionTokenService(IConfiguration configuration, TimeProvider clock)
{
    private const string Prefix = "svr-demo";

    public string Create(Guid memberId)
    {
        var expiresAt = clock.GetUtcNow().AddDays(14).ToUnixTimeSeconds();
        var payload = $"{Prefix}.{memberId:D}.{expiresAt}";
        return $"{payload}.{Signature(payload)}";
    }

    public bool TryValidate(string token, out Guid memberId)
    {
        memberId = Guid.Empty;
        var parts = token.Split('.', StringSplitOptions.None);
        if (parts.Length != 4 || parts[0] != Prefix || !Guid.TryParse(parts[1], out var parsedMemberId)
            || !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt))
            return false;

        var payload = string.Join('.', parts.Take(3));
        if (!FixedTimeEquals(parts[3], Signature(payload)) || expiresAt < clock.GetUtcNow().ToUnixTimeSeconds()) return false;

        memberId = parsedMemberId;
        return true;
    }

    private string Signature(string payload)
    {
        var secret = configuration["DevAuth:Token"];
        if (string.IsNullOrWhiteSpace(secret)) return string.Empty;
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
