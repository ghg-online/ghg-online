namespace server.Services.Database
{
    public class TransactionController : ITransactionController
    {
        readonly IDbHolder dbHolder;

        public TransactionController(IDbHolder dbHolder)
        {
            this.dbHolder = dbHolder;
        }

        public bool BeginTrans()
        {
            return dbHolder.BeginTrans();
        }

        public bool Commit()
        {
            return dbHolder.Commit();
        }

        public bool Rollback()
        {
            return dbHolder.Rollback();
        }
    }
}
