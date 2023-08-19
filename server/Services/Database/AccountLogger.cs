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
