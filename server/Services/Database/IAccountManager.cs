using Grpc.Core;

namespace server.Services.Database
{
    public interface IAccountManager
    {
        void CreateAccount(string username, string password, Entities.Account.RoleCode role);
        void DeleteAccount(string username);
        void ChangeUsername(string username, string newUsername);
        void ChangePassword(string username, string newPassword);
        bool ExistsUsername(string username);

        string? VerifyPasswordAndGenerateToken(string username, string password); // return null if failed
        Entities.Account VerifyTokenAndGetAccount(string token);
        Entities.Account VerifyTokenAndGetAccount(ServerCallContext context);
        Entities.Account.Token VerifyTokenAndGetInfo(string token);
        Entities.Account.Token VerifyTokenAndGetInfo(ServerCallContext context);
    }
}
