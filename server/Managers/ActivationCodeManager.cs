using LiteDB;
using server.Entities;

namespace server.Managers
{
    public class ActivationCodeManager : IActivationCodeManager
    {
        private readonly ILiteCollection<ActivationCode> _activationCodes;
        private readonly ITransactionManager _transactionManager;
        private readonly ILogger<ActivationCodeManager> _logger;

        public ActivationCodeManager(IDbHolder dbHolder, ITransactionManager transactionManager, ILogger<ActivationCodeManager> logger)
        {
            _activationCodes = dbHolder.DbAccountService.GetCollection<ActivationCode>("activation_codes");
            _transactionManager = transactionManager;
            _logger = logger;
        }

        string IActivationCodeManager.CreateCode()
        {
            ActivationCode activationCode = new();
            _activationCodes.Insert(activationCode);
            return activationCode.Code;
        }

        void IActivationCodeManager.UseCode(string Code)
        {
            _transactionManager.BeginTrans();
            ActivationCode activationCode = _activationCodes.FindOne(x => x.Code == Code);
            if (activationCode == null)
            {
                _logger.LogError("An activation code that not exists is tried to be used");
                throw new Exception("An activation code that not exists is tried to be used");
            }
            _activationCodes.Delete(activationCode.Id);
            _transactionManager.Commit();
        }

        bool IActivationCodeManager.VerifyCode(string Code)
        {
            return _activationCodes.Exists(x => x.Code == Code);
        }
    }
}
