/*  IMPORTANT:
 *      Do not call any method of a database service from another database service,
 *      because it might cause a deadlock.
 *      
 *      There are several different databases, and each database has its own lock.
 *      If database A calls a method of database B, and database B calls a method of database A,
 *      a deadlock might occur.
 *      
 *      Put this notice to all database services.
 */

using LiteDB;
using server.Entities;

namespace server.Services.Database
{
    public class ActivationCodeManager : IActivationCodeManager
    {
        private readonly ILiteDatabase _db_lock; // used for locking and transactions
        private readonly ILiteCollection<ActivationCode> _activationCodes;
        private readonly ILogger<ActivationCodeManager> _logger;

        public ActivationCodeManager(IDbHolder dbHolder, ILogger<ActivationCodeManager> logger)
        {
            _db_lock = dbHolder.DbAccountService;
            _activationCodes = dbHolder.DbAccountService.GetCollection<ActivationCode>();
            _logger = logger;
        }

        string IActivationCodeManager.CreateCode()
        {
            ActivationCode activationCode = new();
            lock (_db_lock) _activationCodes.Insert(activationCode);
            return activationCode.Code;
        }

        void IActivationCodeManager.UseCode(string Code)
        {
            lock (_db_lock)
            {
                _db_lock.BeginTrans();
                ActivationCode activationCode = _activationCodes.FindOne(x => x.Code == Code);
                if (activationCode == null)
                {
                    _logger.LogError("An activation code that not exists is tried to be used");
                    throw new Exception("An activation code that not exists is tried to be used");
                }
                _activationCodes.Delete(activationCode.Id);
                _db_lock.Commit();
            }
        }

        bool IActivationCodeManager.VerifyCode(string Code)
        {
            lock (_db_lock) return _activationCodes.Exists(x => x.Code == Code);
        }
    }
}
