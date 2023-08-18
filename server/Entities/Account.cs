using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;

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
        public string? Username { get; set; } // Username should be a unique string consists of 6-32 alphas(a-zA-Z), digits(0-9), underscores(_), and hyphens(-)
        public string? PasswordHash { get; set; } // using HmacSHA256 Base64
        public StatusCode Status { get; set; }
        public RoleCode Role { get; set; }

        public static bool IsUsernameValid(string? username)
        {
            if (String.IsNullOrEmpty(username)) return false;
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_-]{6,32}$");
        }

        public bool IsAbleTo(string action, string? target1 = null)
        {
            switch (action)
            {
                case "GenerateActivationCode":
                    if (Role == RoleCode.Admin)
                        return true;
                    else
                        return false;
                case "DeleteAccount":
                case "ChangeUsername":
                case "ChangePassword":
                    if (Role == RoleCode.Admin)
                        return true;
                    else
                        if (target1 == Username)
                        return true;
                    else
                        return false;
                default:
                    throw new Exception($"Unknown action: {action}");
            }
        }

        // This secret is used to generate password hash, NOT USED TO GENERATE TOKEN !!!
        public const string _password_sha256_secret = "_password_sha256_secret_ghg_online";

        public static string HashCode(string origin)
        {
            byte[] keyByte = Encoding.GetEncoding("utf-8").GetBytes(_password_sha256_secret);
            byte[] messageBytes = Encoding.GetEncoding("utf-8").GetBytes(origin);
            using var hmacsha256 = new HMACSHA256(keyByte);
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashmessage);
        }

        public class Token
        {
            public Guid Id { get; set; }
            public string Username { get; set; }
            public RoleCode Role { get; set; }

            public Token(Guid guid, string username, RoleCode role)
            {
                Id = guid;
                Username = username;
                Role = role;
            }

            public Token(JwtSecurityToken jwt)
            {
                Id = Guid.Parse(jwt.Claims.First(x => x.Type == "Id").Value);
                Username = jwt.Claims.First(x => x.Type == "Username").Value;
                Role = (RoleCode)Enum.Parse(typeof(RoleCode), jwt.Claims.First(x => x.Type == "Role").Value);
            }

            public string GenerateToken(string secret)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(secret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
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

            public static JwtSecurityToken ValidateToken(string secret, string token)
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

            public bool IsAbleTo(string action, string? target1 = null)
            {
                return new Account()
                {
                    Id = this.Id,
                    Username = this.Username,
                    Role = this.Role,
                }.IsAbleTo(action, target1);
            }
        }

        public string GenerateToken(string secret)
        {
            if (String.IsNullOrEmpty(Username)) throw new Exception("Generate token from an Account object whose username is null");
            if (String.IsNullOrEmpty(secret)) throw new Exception("Generate token with an empty secret");

            return new Token(Id, Username, Role).GenerateToken(secret);
        }

        public static Token ValidateAndReadToken(string secret, string token)
        {
            return new Token(Token.ValidateToken(secret, token));
        }
    }
}
