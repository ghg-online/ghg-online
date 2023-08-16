using LiteDB;
using static server.Entities.AccountLog;

namespace server.Helpers
{
    public interface IAccountLogger
    {
        void WriteLog(AccountLogType type, string? ip, string? userName, bool success, string? appendix);
    }
}
