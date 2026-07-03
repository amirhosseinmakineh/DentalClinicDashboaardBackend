using DentalDashboard.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace DentalDashboard.Security.Generator
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly IConfiguration configuration;

        public TokenGenerator(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string GenerateToken(User user, List<Role> roles)
        {
            var jwtSetting = configuration.GetSection("JwtSettings");
            var expiryMinutes = Convert.ToDouble(jwtSetting["ExpiryMinutes"] ?? "60");
            var secret = jwtSetting["SecretKey"];
            var audience = jwtSetting["Audience"];
            var issuer = jwtSetting["Issuer"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            if (string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("JwtSettings:Audience is not configured.");

            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("JwtSettings:Issuer is not configured.");

            var secretKey = Encoding.UTF8.GetBytes(secret);
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim("Id", user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("firstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("lastName", user.LastName),
                new Claim("FullName", fullName),
                new Claim("fullName", fullName),
                new Claim("PhoneNumber", user.PhoneNumber),
                new Claim("phoneNumber", user.PhoneNumber),
            };

            foreach (var role in roles.Select(x => x.RoleName).Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
                claims.Add(new Claim("roles", role));
            }

            claims.Add(new Claim("isCompleteProfile", user.IsCompleteProfile.ToString().ToLowerInvariant()));
            claims.Add(new Claim("IsCompleteProfile", user.IsCompleteProfile.ToString()));

            var consultantProfileId = user.ConsultantProfile?.Id;
            if (consultantProfileId is > 0)
            {
                var profileId = consultantProfileId.Value.ToString();
                claims.Add(new Claim("consultantProfileId", profileId));
                claims.Add(new Claim("ConsultantProfileId", profileId));
                claims.Add(new Claim("profileId", profileId));
                claims.Add(new Claim("ProfileId", profileId));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Audience = audience,
                Issuer = issuer,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),

                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(secretKey),
                    SecurityAlgorithms.HmacSha256
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var jwtSetting = configuration.GetSection("JwtSettings");

            var secret = jwtSetting["SecretKey"];
            var issuer = jwtSetting["Issuer"];
            var audience = jwtSetting["Audience"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            var secretKey = Encoding.UTF8.GetBytes(secret);

            var parameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = issuer,
                ValidAudience = audience,

                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(
                token,
                parameters,
                out SecurityToken validatedToken
            );

            return principal;
        }

    }
}
