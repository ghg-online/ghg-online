namespace server.Services.Database
{
    public interface ITransactionManager
    {
        void BeginTrans();
        void Commit();
        void Rollback();
    }
}
