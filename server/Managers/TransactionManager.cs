using LiteDB;

namespace server.Managers
{
    public class TransactionManager : ITransactionManager
    {
        private readonly ILiteDatabase _db;

        public TransactionManager(IDbHolder dbHolder)
        {
            _db = dbHolder.DbAccountService;
        }

        public void BeginTrans()
        {
            _db.BeginTrans();
        }

        public void Commit()
        {
            _db.Commit();
        }

        public void Rollback()
        {
            _db.Rollback();
        }
    }
}
