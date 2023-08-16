using LiteDB;

namespace server.Helpers
{
    public interface IDbHolder
    {
        ILiteDatabase LiteDatabase { get; }
    }
}
