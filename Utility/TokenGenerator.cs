using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;



namespace renjibackend.Utility
{
    public class TokenGenerator
    {

        private readonly IConfiguration _configuration; // This is for accessing key-value properties appsettings.json

        public TokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(string userID, string email, string name)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            string secretKey = jwtSettings["SecretKey"] ?? "";
            string issuer = jwtSettings["Issuer"] ?? "";
            string audience = jwtSettings["Audience"] ?? "";
            int expiry = int.Parse(jwtSettings["ExpiryMinutes"] ?? "0");

             
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)); // This is the key
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Signing method + key

            // Payload 
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userID),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };


            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: creds // this is used for locking and sealing the token
            );

            return new JwtSecurityTokenHandler().WriteToken(token); // Generates token and lock it using the key and creds

        }




    }
}
