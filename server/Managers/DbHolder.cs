using LiteDB;
using Microsoft.Extensions.Logging;
using server.Services;

namespace server.Managers
{
    public class DbHolder : IDbHolder, IDisposable
    {
        private readonly ILogger _logger;
        private readonly LiteDatabase _db_account_service;
        private static string? _db_account_service_connection_string;

        public DbHolder(ILogger<DbHolder> logger, IConfiguration configuration)
        {
            _logger = logger;
            if (_db_account_service_connection_string == null)
            {
                string dbPath = Path.GetFullPath(configuration.GetSection("LiteDB").GetValue<string>("Path"));
                _db_account_service_connection_string = MakeConnectionString(dbPath, "account_service.db");
                logger.LogInformation("DbAccountService: {str}", _db_account_service_connection_string);
                if (!File.Exists(Path.Combine(dbPath, "account_service.db")))
                {
                    CreateDbAccountService();
                }
            }
            _db_account_service = new LiteDatabase(_db_account_service_connection_string);
        }

        private static string MakeConnectionString(string path, string filename)
        {
            return $"Filename={Path.Combine(path, filename)};Connection=Shared";
        }

        public ILiteDatabase DbAccountService
        {
            get
            {
                return _db_account_service;
            }
        }

        public void Dispose()
        {
            _db_account_service.Dispose();
            GC.SuppressFinalize(this);
        }

        private void CreateDbAccountService()
        {
            _logger.LogInformation("Creating account_service.db and default admin");
            using (var db = new LiteDatabase(_db_account_service_connection_string))
            {
                var col = db.GetCollection<Entities.Account>("accounts");

                col.EnsureIndex(x => x.Username, true);

                col.Insert(new Entities.Account
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    PasswordHash = Entities.Account.HashCode("admin"),
                    Status = Entities.Account.StatusCode.Activated,
                    Role = Entities.Account.RoleCode.Admin
                });

                var col2 = db.GetCollection<Entities.AccountLog>("account_logs");

                var col3 = db.GetCollection<Entities.ActivationCode>("activation_codes");

                col3.EnsureIndex(x => x.Code, true);
            }
        }
    }
}
