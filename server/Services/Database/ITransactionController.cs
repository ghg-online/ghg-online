namespace server.Services.Database
{
    public interface ITransactionController
    {
        bool BeginTrans();
        bool Commit();
        bool Rollback();
    }
}
