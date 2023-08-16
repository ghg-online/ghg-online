using LiteDB;
using server.Services;

namespace server.Helpers
{
    public class DbHolder : IDbHolder
    {
        private readonly LiteDatabase _db;
        public DbHolder(ILogger<DbHolder> logger, IConfiguration configuration)
        {
            string dbPath = Path.GetFullPath(configuration.GetSection("LiteDB").GetValue<string>("Path"));
            string _connection_string = MakeConnectionString(dbPath, "account_service.db");
            logger.LogInformation("LiteDB Connection string: {str}", _connection_string);
            _db = new LiteDatabase(_connection_string);
        }

        private static string MakeConnectionString(string path, string filename)
        {
            return $"Filename={Path.Combine(path, filename)};Connection=Shared";
        }

        public ILiteDatabase LiteDatabase
        {
            get
            {
                return _db;
            }
        }
    }
}
