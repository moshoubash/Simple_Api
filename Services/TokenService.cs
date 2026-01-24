using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace ecommerce_back.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(_configuration["JWT:TokenExpirationMinutes"] != null ? Convert.ToDouble(_configuration["JWT:TokenExpirationMinutes"]) : 30),
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];

            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateLifetime = false, 
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]))
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken securityToken);

            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}