using ecommerce.Context;
using ecommerce.Models;
using ecommerce_back.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net;

namespace ecommerce_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ITokenService tokenService, ApplicationDbContext context, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] JsonElement userJson)
        {
            if(!userJson.GetProperty("password").GetString().Equals(userJson.GetProperty("password_confirmation").GetString()))
            {
                return BadRequest("Password and password confirmation do not match");
            }

            var passwordRegex = new System.Text.RegularExpressions.Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
            
            if (!passwordRegex.IsMatch(userJson.GetProperty("password").GetString()))
            {
                return BadRequest("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number and one special character");
            }
            
            var user = new User
            {
                Name = userJson.GetProperty("name").GetString(),
                Email = userJson.GetProperty("email").GetString(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userJson.GetProperty("password").GetString()),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var isUserExist = _context.Users.Any(u => u.Email == user.Email);

            if (isUserExist)
            {
                return BadRequest("User already exist");
            }
            
            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement userJson)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == userJson.GetProperty("email").GetString());

            if (user == null)
            {
                return BadRequest("User not found");
            }

            if (!BCrypt.Net.BCrypt.Verify(userJson.GetProperty("password").GetString(), user.PasswordHash))
            {
                return BadRequest("Wrong password");
            }

            List<Claim> authClaims = [
                    new (ClaimTypes.Name, user.Email),
                    new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ];

            var token = _tokenService.GenerateAccessToken(authClaims);

            var refreshToken = _tokenService.GenerateRefreshToken();

            var tokenInfo = _context.TokenInfos.FirstOrDefault(a => a.Username == user.Email);

            if (tokenInfo == null)
            {
                var ti = new TokenInfo
                {
                    Username = user.Email,
                    RefreshToken = refreshToken,
                    ExpiredAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:TokenExpirationMinutes"] ?? "30"))
                };
                _context.TokenInfos.Add(ti);
            } 
            
            else
            {
                tokenInfo.RefreshToken = refreshToken;
                tokenInfo.ExpiredAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:TokenExpirationMinutes"] ?? "30"));
            }

            await _context.SaveChangesAsync();

            return Ok(new JsonResult(new
            {
                AccessToken = token,
                RefreshToken = refreshToken
            }));
        }
        
        [HttpGet("profile")]
        public async Task<IActionResult> Profile(string accessToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var userEmail = principal.Identity.Name;

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            return Ok(user);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(string accessToken, string refreshToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name;

            var tokenInfo = _context.TokenInfos.SingleOrDefault(u => u.Username == username);

            if (tokenInfo == null || tokenInfo.RefreshToken != refreshToken || tokenInfo.ExpiredAt <= DateTime.UtcNow)
            {
                return BadRequest("Invalid refresh token. Please login again.");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            tokenInfo.RefreshToken = newRefreshToken;
            tokenInfo.ExpiredAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:TokenExpirationMinutes"] ?? "30"));

            await _context.SaveChangesAsync();

            return Ok(new JsonResult(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            }));
        }
    
        [HttpPost("logout")]
        public IActionResult Logout([FromHeader] string Authorization)
        {
            var accessToken = Authorization.Substring("Bearer ".Length);
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity.Name;

            var tokenInfo = _context.TokenInfos.SingleOrDefault(u => u.Username == username);

            if (tokenInfo != null)
            {
                _context.TokenInfos.Remove(tokenInfo);
                _context.SaveChanges();
            }

            return Ok("Logged out successfully");
        }
    }
}