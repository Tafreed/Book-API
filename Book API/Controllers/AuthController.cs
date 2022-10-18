using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Book_API.Controllers
{
    [Route("auth/v{version:apiVersion}")]
    [ApiVersion("2.0")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Get Bearer Token
        /// </summary>
        /// <param name="authenticationRequestBody"></param>
        /// <returns></returns>
        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate(AuthenticationRequestBody authenticationRequestBody)
        {
            var user = ValidateUserCredentials(
                authenticationRequestBody.UserName,
                authenticationRequestBody.Password);

            if(user == null)
                return Unauthorized();

            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Authentication:SecretKey"]));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimsFortoken = new List<Claim>();
            claimsFortoken.Add(new Claim("sub", user.UserId.ToString()));
            claimsFortoken.Add(new Claim("given_name", user.UserName.ToString()));

            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsFortoken,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                signingCredentials);

            var tokenToReturn = new JwtSecurityTokenHandler()
                .WriteToken(jwtSecurityToken);

            return Ok(tokenToReturn);
        }

        private ApiUser ValidateUserCredentials(string? userName, string? password)
        {
            return new ApiUser(1, userName ?? "Tafreed");
        }
    }

    public class AuthenticationRequestBody
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class ApiUser
    {
        public int UserId { get; set; }
        public string UserName { get; set; }

        public ApiUser(int userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }
}
