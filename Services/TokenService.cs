using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyTasks.Exceptions;
using MyTasks.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyTasks.Services
{
    public class TokenService(IConfiguration _config) : ITokenService
    {
        // JsonWebTokenHandler is thread-safe and meant to be reused rather than
        // instantiated per call.
        private static readonly JsonWebTokenHandler _handler = new();

        public GeneratedToken GenerateAccessToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]
                ?? throw new ConfigurationException("Jwt:Key is not configured.");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var minutes = jwtSection.GetValue<int?>("AccessTokenMinutes") ?? 15;

            var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Expires = expiresAt,
                SigningCredentials = credentials,
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                    [ClaimTypes.NameIdentifier] = user.Id.ToString(),
                    [ClaimTypes.Name] = user.Username,
                    [ClaimTypes.Email] = user.Email,
                    [ClaimTypes.Role] = user.Role.ToString(),
                    [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
                }
            };

            // CreateToken returns the signed, encoded JWT string directly - no
            // separate "build token object then serialize" step needed.
            var value = _handler.CreateToken(descriptor);

            return new GeneratedToken(value, expiresAt);
        }

        public GeneratedToken GenerateRefreshToken()
        {
            var jwtSection = _config.GetSection("Jwt");
            var days = jwtSection.GetValue<int?>("RefreshTokenDays") ?? 7;

            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var value = Convert.ToBase64String(randomBytes);
            var expiresAt = DateTime.UtcNow.AddDays(days);

            return new GeneratedToken(value, expiresAt);
        }

        public string HashRefreshToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(bytes);
        }
    }
}
