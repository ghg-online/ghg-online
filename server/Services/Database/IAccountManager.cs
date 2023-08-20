using Grpc.Core;
using server.Entities;

namespace server.Services.Database
{
    public interface IAccountManager
    {
        void CreateAccount(string username, string password, Entities.Account.RoleCode role);
        void DeleteAccount(string username);
        void ChangeUsername(string username, string password, string newUsername);
        void ChangePassword(string username, string newPassword);
        bool ExistsUsername(string username);
        Account QueryAccount(string username);
    }
}
