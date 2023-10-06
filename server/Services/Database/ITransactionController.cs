namespace server.Services.Database
{
    public interface ITransactionController
    {
        public interface ITransaction : IDisposable
        {
            void Commit();
            void Rollback();
        }
        ITransaction BeginTrans();
    }
}
