using LiteDB;

namespace server.Database
{
    public interface IDbHolder
    {
        LiteDatabase LiteDatabase { get; }
    }
}
