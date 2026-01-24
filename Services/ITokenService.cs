using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Models;
using System.Security.Claims;

namespace ecommerce_back.Services
{
    public interface ITokenService
    {
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken);
        public string GenerateAccessToken(IEnumerable<Claim> claims);
        public string GenerateRefreshToken();
    }
}