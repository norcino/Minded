using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MindedExample.Application.Common;
using MindedExample.Domain;

namespace MindedExample.Infrastructure.Persistence.Security
{
    /// <summary>
    /// Infrastructure implementation of <see cref="IJwtTokenService"/> that generates
    /// signed JWT access tokens for authenticated users.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        /// <summary>Initializes a new <see cref="JwtTokenService"/>.</summary>
        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public string CreateAccessToken(User user)
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
                claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

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
