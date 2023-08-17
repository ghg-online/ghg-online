using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace server.Entities
{
    public class Account
    {
        public enum StatusCode
        {
            Activated = 0,
            Deleted = 1,
        }
        public enum RoleCode
        {
            Admin = 0,
            User = 1
        }

        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; } // using HmacSHA256 Base64
        public StatusCode Status { get; set; }
        public RoleCode Role { get; set; }

        public const string _password_sha256_secret = "_password_sha256_secret_ghg_online";

        public static string HashCode(string origin)
        {
            byte[] keyByte = Encoding.GetEncoding("utf-8").GetBytes(_password_sha256_secret);
            byte[] messageBytes = Encoding.GetEncoding("utf-8").GetBytes(origin);
            using var hmacsha256 = new HMACSHA256(keyByte);
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashmessage);
        }

        public string GenerateToken(string secret)
        {
            if(String.IsNullOrEmpty(Username)) throw new Exception("Generate token from an Account object whose username is null");
            if(String.IsNullOrEmpty(secret)) throw new Exception("Generate token with an empty secret");
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim("Id", Id.ToString()),
                    new Claim("Username", Username),
                    new Claim("Role", Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static JwtSecurityToken ValidateToken(string secret, string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
            }, out SecurityToken validatedToken);

            return (JwtSecurityToken)validatedToken;
        }

        public static void ValidateAndReadToken(string secret, string token, out string id, out string username, out string role)
        {
            var validatedToken = ValidateToken(secret, token);
            id = validatedToken.Claims.First(x => x.Type == "id").Value;
            username = validatedToken.Claims.First(x => x.Type == "username").Value;
            role = validatedToken.Claims.First(x => x.Type == "role").Value;
        }
    }
}
