using Grpc.Core;
using LiteDB;
using server.Entities;

namespace server.Managers
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
            return _accounts.Exists(x => x.Username == username);
        }

        public void DeleteAccount(string username)
        {
            _accounts.Delete(_accounts.FindOne(x => x.Username == username).Id);
        }

        public string? VerifyPasswordAndGenerateToken(string username, string password)
        {
            Entities.Account account = _accounts.FindOne(x => x.Username == username);
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

        public Entities.Account VerifyTokenAndGetAccount(string token)
        {
            try
            {
                Entities.Account.ValidateAndReadToken(_jwt_secret, token, out string id_str, out string username, out string role_str);
                Guid id = Guid.Parse(id_str);
                Entities.Account.RoleCode role = (Entities.Account.RoleCode)Enum.Parse(typeof(Entities.Account.RoleCode), role_str);
                Entities.Account account = new()
                {
                    Id = id,
                    Username = username,
                    PasswordHash = null,
                    Status = Entities.Account.StatusCode.Activated,
                    Role = role
                };
                return account;
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
    }
}
