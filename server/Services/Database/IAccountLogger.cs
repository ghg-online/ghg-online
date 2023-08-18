using LiteDB;
using static server.Entities.AccountLog;

namespace server.Services.Database
{
    public interface IAccountLogger
    {
        void WriteLog(AccountLogType type, string? ip, string? userName, bool success, string? appendix);
    }
}
