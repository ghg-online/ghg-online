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
using static server.Entities.AccountLog;

namespace server.Services.Database
{
    public class AccountLogger : IAccountLogger
    {
        private readonly ILiteDatabase _db_lock; // used for locking
        private readonly ILiteCollection<AccountLog> _accountLogs;
        public AccountLogger(IDbHolder dbHolder)
        {
            _db_lock = dbHolder.DbAccountServiceLog;
            _accountLogs = dbHolder.DbAccountServiceLog.GetCollection<AccountLog>();
        }
        public void WriteLog(AccountLogType type, string? ip, string? userName, bool success, string? appendix)
        {
            lock (_db_lock) _accountLogs.Insert(new AccountLog
            {
                Type = type,
                Time = DateTime.Now,
                Ip = ip,
                UserName = userName,
                Success = success,
                Appendix = appendix
            });
        }
    }
}
