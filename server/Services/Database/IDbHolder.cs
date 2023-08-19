using LiteDB;

namespace server.Services.Database
{
    public interface IDbHolder
    {
        ILiteDatabase DbAccountService { get; }
        ILiteDatabase DbAccountServiceLog { get; }
    }
}
