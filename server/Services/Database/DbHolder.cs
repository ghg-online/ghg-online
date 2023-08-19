/*  IMPORTANT:
 *      Do not call any method of a database service from another database service,
 *      because it might cause a deadlock.
 *      
 *      There are several different databases, and each database has its own lock.
 *      If database A calls a method of database B, and database B calls a method of database A,
 *      a deadlock might occur.
 *      
 *      Put this notice to all database services.
 */

using LiteDB;
using Microsoft.Extensions.Logging;
using server.Services;

namespace server.Services.Database
{
    public class DbHolder : IDbHolder, IDisposable
    {
        private readonly ILogger _logger;
        private readonly LiteDatabase _db_account_service;
        private readonly LiteDatabase _db_account_service_log;
        private static string? _db_account_service_connection_string = null;
        private static string? _db_account_service_log_connection_string = null;

        public DbHolder(ILogger<DbHolder> logger, IConfiguration configuration)
        {
            _logger = logger;

            if (_db_account_service_connection_string == null)
            {
                string dbPath = Path.GetFullPath(configuration.GetSection("LiteDB").GetValue<string>("Path"));
                _db_account_service_connection_string = MakeConnectionString(dbPath, "account_service.db");
                logger.LogInformation("{str}", _db_account_service_connection_string);
                if (!File.Exists(Path.Combine(dbPath, "account_service.db")))
                {
                    CreateDbAccountService();
                }
            }
            _db_account_service = new LiteDatabase(_db_account_service_connection_string);

            if (_db_account_service_log_connection_string == null)
            {
                string dbPath = Path.GetFullPath(configuration.GetSection("LiteDB").GetValue<string>("Path"));
                _db_account_service_log_connection_string = MakeConnectionString(dbPath, "account_service.log.db");
                logger.LogInformation("{str}",_db_account_service_log_connection_string);
                if (!File.Exists(Path.Combine(dbPath, "account_service.log.db")))
                {
                    CreateDbAccountServiceLog();
                }
            }
            _db_account_service_log = new LiteDatabase(_db_account_service_log_connection_string);
        }

        private static string MakeConnectionString(string path, string filename)
        {
            // return $"Filename={Path.Combine(path, filename)};Connection=Shared";
            return $"Filename={Path.Combine(path, filename)};Connection=Direct";
        }

        public ILiteDatabase DbAccountService
        {
            get
            {
                return _db_account_service;
            }
        }

        public ILiteDatabase DbAccountServiceLog
        {
            get
            {
                return _db_account_service_log;
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
                var col = db.GetCollection<Entities.Account>();

                col.EnsureIndex(x => x.Username);

                col.Insert(new Entities.Account
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    PasswordHash = Entities.Account.HashCode("admin"),
                    Status = Entities.Account.StatusCode.Activated,
                    Role = Entities.Account.RoleCode.Admin
                });

                var col2 = db.GetCollection<Entities.ActivationCode>();
                col2.EnsureIndex(x => x.Code, true);
            }
        }

        private void CreateDbAccountServiceLog()
        {
            _logger.LogInformation("Creating account_service.log.db");
            using (var db = new LiteDatabase(_db_account_service_log_connection_string))
            {
                var col = db.GetCollection<Entities.AccountLog>();
                col.Insert(new Entities.AccountLog
                {
                    Type = Entities.AccountLog.AccountLogType.Information,
                    Time = DateTime.Now,
                    Ip = null,
                    UserName = null,
                    Success = true,
                    Appendix = "Account service log created",
                });
            }
        }
    }
}
