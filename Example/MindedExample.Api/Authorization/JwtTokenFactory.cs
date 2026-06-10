using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MindedExample.Domain;

namespace MindedExample.Api.Authorization
{
    /// <summary>
    /// Creates JWT access tokens for authenticated users.
    /// </summary>
    public class JwtTokenFactory
    {
        private readonly IConfiguration _configuration;

        public JwtTokenFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? "MindedExample";
            var audience = jwtSection["Audience"] ?? "MindedExample.Frontend";
            var signingKey = jwtSection["SigningKey"] ?? "ThisIsADevelopmentOnlySigningKeyPleaseChange";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("tenant_role", user.TenantRole ?? TenantMemberRoles.Member),
                new Claim("is_global_admin", user.IsGlobalAdmin.ToString().ToLowerInvariant())
            };

            if (user.TenantId.HasValue)
            {
                claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
