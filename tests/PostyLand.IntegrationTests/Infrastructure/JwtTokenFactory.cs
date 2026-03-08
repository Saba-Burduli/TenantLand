using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PostyLand.Domain.Enums;

namespace PostyLand.IntegrationTests.Infrastructure;

public static class JwtTokenFactory
{
    public static string Create(
        string signingKey,
        string issuer,
        string audience,
        Guid userId,
        Guid tenantId,
        RoleStatus role,
        string? scope)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("UserId", userId.ToString()),
            new("TenantId", tenantId.ToString()),
            new("Role", role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(scope))
        {
            claims.Add(new Claim("Scope", scope));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
