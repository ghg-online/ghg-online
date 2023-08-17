using LiteDB;

namespace server.Managers
{
    public interface IDbHolder
    {
        ILiteDatabase DbAccountService { get; }
    }
}
