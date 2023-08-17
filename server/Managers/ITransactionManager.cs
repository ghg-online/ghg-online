namespace server.Managers
{
    public interface ITransactionManager
    {
        void BeginTrans();
        void Commit();
        void Rollback();
    }
}
