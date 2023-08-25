using Microsoft.IdentityModel.Tokens;
using server.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static server.Entities.Account;

namespace server.Services.Authenticate
{
    public class TokenService : ITokenService
    {
        readonly IRsaPairManager rsaPairManager;
        readonly JwtSecurityTokenHandler jwtSecurityTokenHandler;

        public TokenService(IRsaPairManager rsaPairManager, JwtSecurityTokenHandler jwtSecurityTokenHandler)
        {
            this.rsaPairManager = rsaPairManager;
            this.jwtSecurityTokenHandler = jwtSecurityTokenHandler;
        }

        public TokenInfo ConvertToTokenInfo(Account account) =>
            new(account.Id, account.Username, account.Role);

        public string SignToken(TokenInfo tokenInfo)
        {
            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: new[]{
                    new Claim("Id", tokenInfo.Id.ToString()),
                    new Claim("Username", tokenInfo.Username),
                    new Claim("Role", tokenInfo.Role.ToString())
                },
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: new SigningCredentials(
                    new RsaSecurityKey(rsaPairManager.GetPrikey()),
                    SecurityAlgorithms.RsaSha256Signature
                )
            );
            return jwtSecurityTokenHandler.WriteToken(token);
        }

        public TokenInfo VerifyTokenAndGetInfo(string token)
        {
            var rsaSecurityKey = new RsaSecurityKey(rsaPairManager.GetPubkey());
            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = rsaSecurityKey,
            };
            jwtSecurityTokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var claims = ((JwtSecurityToken)validatedToken).Payload.Claims;
            return new(
                id: Guid.Parse(claims.First(x => x.Type == "Id").Value),
                username: claims.First(x => x.Type == "Username").Value,
                role: (RoleCode)Enum.Parse(typeof(RoleCode), claims.First(x => x.Type == "Role").Value)
            );
        }
    }
}
