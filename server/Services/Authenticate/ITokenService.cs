using server.Entities;

namespace server.Services.Authenticate
{
    public interface ITokenService
    {
        public TokenInfo ConvertToTokenInfo(Account account);
        public string SignToken(TokenInfo tokenInfo);
        public TokenInfo VerifyTokenAndGetInfo(string token);
    }
}
