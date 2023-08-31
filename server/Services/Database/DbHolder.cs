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

namespace server.Services.Database
{
    public class DbHolder : IDbHolder, IDisposable
    {
        private readonly ILiteDatabase db;
        private readonly ILogger<DbHolder> logger;

        private readonly ILiteCollection<Entities.Account> accounts;
        private readonly ILiteCollection<Entities.AccountLog> accountLogs;
        private readonly ILiteCollection<Entities.ActivationCode> activationCodes;
        private readonly ILiteCollection<Entities.Computer> computers;
        private readonly ILiteCollection<Entities.Directory> directories;
        private readonly ILiteCollection<Entities.File> files;
        private readonly ILiteCollection<Entities.FileData> fileData;

        // Configurations for LiteDB, see https://www.litedb.org/docs/connection-string/
        private readonly string db_filename;
        private readonly string db_connection;
        private readonly string db_password;
        private readonly string db_initial_size;
        private readonly string db_readonly;
        private readonly string db_collation; // see https://www.litedb.org/docs/collation/
        private readonly string db_upgrade;

        // Connection string for LiteDB
        private readonly string db_connection_string;

        public DbHolder(ILogger<DbHolder> logger, IConfiguration configuration)
        {
            this.logger = logger;
            db_filename = Path.Combine(configuration["Data:BasePath"]
                , configuration["Data:Directory:LiteDB"], configuration["Data:Filename:LiteDB"]);
            db_connection = configuration["Data:LiteDBConfig:Connection"];
            db_password = configuration["Data:LiteDBConfig:Password"];
            db_initial_size = configuration["Data:LiteDBConfig:InitialSize"];
            db_readonly = configuration["Data:LiteDBConfig:Readonly"];
            db_collation = configuration["Data:LiteDBConfig:Collation"];
            db_upgrade = configuration["Data:LiteDBConfig:Upgrade"];

            db_connection_string = $"Filename={db_filename};Connection={db_connection};Password={db_password};InitialSize={db_initial_size};ReadOnly={db_readonly};Upgrade={db_upgrade};Collation={db_collation}";

            if (false == File.Exists(db_filename))
            {
                CreateDb();
            }

            db = new LiteDatabase(db_connection_string);
            accounts = db.GetCollection<Entities.Account>();
            accountLogs = db.GetCollection<Entities.AccountLog>();
            activationCodes = db.GetCollection<Entities.ActivationCode>();
            computers = db.GetCollection<Entities.Computer>();
            directories = db.GetCollection<Entities.Directory>();
            files = db.GetCollection<Entities.File>();
            fileData = db.GetCollection<Entities.FileData>();
        }

        private void CreateDb()
        {
            using var db = new LiteDatabase(db_connection_string);
            CreateAccounts(db);
            CreateAccountLogs(db);
            CreateActivationCodes(db);
            CreateComputers(db);
            CreateDirectories(db);
            CreateFiles(db);
            CreateFileData(db);
        }

        private void CreateAccounts(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection Account and default admin");
            var col = db.GetCollection<Entities.Account>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Username, true);
            col.Insert(new Entities.Account
            (
                id: Guid.NewGuid(),
                username: "admin",
                passwordHash: Entities.Account.HashCode("admin", "admin"),
                status: Entities.Account.StatusCode.Activated,
                role: Entities.Account.RoleCode.Admin
            ));
        }

        private void CreateActivationCodes(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection ActivationCode");
            var col = db.GetCollection<Entities.ActivationCode>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Code, true);
        }

        private void CreateAccountLogs(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection AccountLog");
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

        private void CreateComputers(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection Computer");
            var col = db.GetCollection<Entities.Computer>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Owner, false);
        }

        private void CreateDirectories(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection Directory");
            var col = db.GetCollection<Entities.Directory>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Computer, false);
            col.EnsureIndex(x => x.Parent, false);
        }

        private void CreateFiles(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection File");
            var col = db.GetCollection<Entities.File>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Computer, false);
            col.EnsureIndex(x => x.Parent, false);
        }

        private void CreateFileData(ILiteDatabase db)
        {
            logger.LogInformation("Creating collection FileData");
            var col = db.GetCollection<Entities.FileData>();
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Computer, false);
        }

        public void Dispose()
        {
            db.Dispose();
            GC.SuppressFinalize(this);
        }

        public ILiteCollection<Entities.Account> Accounts => accounts;
        public ILiteCollection<Entities.AccountLog> AccountLogs => accountLogs;
        public ILiteCollection<Entities.ActivationCode> ActivationCodes => activationCodes;
        public ILiteCollection<Entities.Computer> Computers => computers;
        public ILiteCollection<Entities.Directory> Directories => directories;
        public ILiteCollection<Entities.File> Files => files;
        public ILiteCollection<Entities.FileData> FileData => fileData;

        public bool BeginTrans()
        {
            return db.BeginTrans();
        }

        public bool Commit()
        {
            return db.Commit();
        }

        public bool Rollback()
        {
            return db.Rollback();
        }
    }
}
