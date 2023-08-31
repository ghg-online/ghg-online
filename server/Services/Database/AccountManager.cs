/*  IMPORTANT:
 *  
 *      Database service should only call methods of low level database services,
 *      such as LiteDatabase, SqlConnection, etc.
 *      
 *      The reason is to avoid deadlock, and to make the code more readable by
 *      ensuring database service is only an interface to the database.
 *      
 *      Put this notice to all database services.
 *      
 */

using LiteDB;
using server.Entities;

namespace server.Services.Database
{
    public class AccountManager : IAccountManager
    {
        private readonly ILiteCollection<Entities.Account> _accounts;

        public AccountManager(IDbHolder dbHolder)
        {
            _accounts = dbHolder.Accounts;
        }

        public void CreateAccount(string username, string password, Entities.Account.RoleCode role)
        {
            Entities.Account account = new(
                id: Guid.NewGuid(),
                username: username,
                passwordHash: Entities.Account.HashCode(password, username),
                status: Entities.Account.StatusCode.Activated,
                role: role
            );
            _accounts.Insert(account);
        }

        public bool ExistsUsername(string username)
        {
            return _accounts.Exists(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
        }

        public void DeleteAccount(string username)
        {
            Entities.Account account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            account.Status = Entities.Account.StatusCode.Deleted;
            _accounts.Update(account);
        }

        public void ChangeUsername(string username, string password, string newUsername)
        {
            var account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            account.Username = newUsername;
            account.PasswordHash = Entities.Account.HashCode(password, newUsername);
            _accounts.Update(account);
        }

        public void ChangePassword(string username, string newPassword)
        {
            var account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            account.PasswordHash = Entities.Account.HashCode(newPassword, username);
            _accounts.Update(account);
        }

        public Account QueryAccount(string username)
        {
            return _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
        }
    }
}
