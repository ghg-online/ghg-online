using Grpc.Core;
using LiteDB;
using server.Entities;
using System.Text.RegularExpressions;

namespace server.Services.Database
{
    public class AccountManager : IAccountManager
    {
        private readonly ILiteCollection<Entities.Account> _accounts;
        private readonly string _jwt_secret;

        public AccountManager(IDbHolder dbHolder, IConfiguration configuration)
        {
            _accounts = dbHolder.DbAccountService.GetCollection<Entities.Account>("accounts");
            _jwt_secret = configuration.GetSection("jwt").GetValue<string>("jwt-secret");
        }

        public void CreateAccount(string username, string password, Entities.Account.RoleCode role)
        {
            Entities.Account account = new()
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = Entities.Account.HashCode(password),
                Status = Entities.Account.StatusCode.Activated,
                Role = role
            };
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

        public string? VerifyPasswordAndGenerateToken(string username, string password)
        {
            Entities.Account account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            if (account == null
                || account.PasswordHash != Entities.Account.HashCode(password)
                || account.Status != Entities.Account.StatusCode.Activated)
            {
                return null;
            }
            else
            {
                return account.GenerateToken(_jwt_secret);
            }
        }

        public Entities.Account.Token VerifyTokenAndGetInfo(string token)
        {
            try
            {
                return Entities.Account.ValidateAndReadToken(_jwt_secret, token);
            }
            catch (Exception)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token. Please login again"));
            }
        }

        public Account.Token VerifyTokenAndGetInfo(ServerCallContext context)
        {
            return VerifyTokenAndGetInfo(context.RequestHeaders.GetValue("Authorization")?.Replace("Bearer ", "")
                ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication required")));
        }

        public Entities.Account VerifyTokenAndGetAccount(string token)
        {
            try
            {
                var token_info = Entities.Account.ValidateAndReadToken(_jwt_secret, token);
                return _accounts.FindOne(x => x.Id == token_info.Id && x.Status != Account.StatusCode.Deleted)
                    ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token. Please login again"));
            }
            catch (Exception)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token. Please login again"));
            }
        }

        public Entities.Account VerifyTokenAndGetAccount(ServerCallContext context)
        {
            return VerifyTokenAndGetAccount(context.RequestHeaders.GetValue("Authorization")?.Replace("Bearer ", "")
                ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication required")));
        }

        public void ChangeUsername(string username, string newUsername)
        {
            var account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            account.Username = newUsername;
            _accounts.Update(account);
        }

        public void ChangePassword(string username, string newPassword)
        {
            var account = _accounts.FindOne(x => x.Username == username && x.Status != Account.StatusCode.Deleted);
            account.PasswordHash = Entities.Account.HashCode(newPassword);
            _accounts.Update(account);
        }
    }
}
