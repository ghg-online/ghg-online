/*  IMPORTANT:
 *  
 *      Database service should only call methods of low level database services,
 *      such as LiteDatabase, SqlConnection, etc.
 *      
 *      The reason is to avoid deadlock, and to make the code more readable by
 *      ensuring database service is only an interface to the database.
 *      
 *      Put this notice to all database services.
 *      
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
