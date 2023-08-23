using Microsoft.IdentityModel.Tokens;
using static server.Entities.Account;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace server.Services.Authenticate
{
    public class TokenInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public RoleCode Role { get; set; }

        public TokenInfo(Guid id, string username, RoleCode role)
        {
            Id = id;
            Username = username;
            Role = role;
        }
    }
}
