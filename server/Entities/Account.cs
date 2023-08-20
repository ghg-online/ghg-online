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
        public string Username { get; set; } // Username should be a unique string consists of 6-32 alphas(a-zA-Z), digits(0-9), underscores(_), and hyphens(-)
        public RoleCode Role { get; set; }
        public StatusCode Status { get; set; }
        public string PasswordHash { get; set; } // using SHA256 Base64

        public Account(Guid id, string username, RoleCode role, StatusCode status, string passwordHash)
        {
            Id = id;
            Username = username;
            Role = role;
            Status = status;
            PasswordHash = passwordHash;
        }

        public static string HashCode(string password, string username)
        {
            byte[] saltByte = Encoding.GetEncoding("utf-8").GetBytes("GHG-ONLINE-SALT" + username);
            byte[] passwordBytes = Encoding.GetEncoding("utf-8").GetBytes(password);
            using var sha256 = new HMACSHA256(saltByte);
            byte[] hashmessage = sha256.ComputeHash(passwordBytes);
            return Convert.ToBase64String(hashmessage);
        }

        public static bool IsUsernameValid(string? username)
        {
            if (String.IsNullOrEmpty(username)) return false;
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_-]{6,32}$");
        }
    }
}
