using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RealTimeInventoryBilling.API.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public (string Token, DateTime ExpiresAt) GenerateToken(UserDto user)
        {
            var jwtKey = _config["Jwt:Key"] ?? "RealTimeInventoryBillingSystemSecretKey2026_SecureKey!CapStoneSuperSecretKey";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "InventoryBillingAPI";
            var jwtAudience = _config["Jwt:Audience"] ?? "InventoryBillingClients";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim("role", user.RoleName)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
