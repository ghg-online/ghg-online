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
        private readonly ILiteCollection<ActivationCode> _activationCodes;

        public ActivationCodeManager(IDbHolder dbHolder)
        {
            _activationCodes = dbHolder.ActivationCodes;
        }

        public string CreateCode()
        {
            ActivationCode activationCode = new();
            _activationCodes.Insert(activationCode);
            return activationCode.Code;
        }

        public void UseCode(string Code)
        {
            ActivationCode activationCode = _activationCodes.FindOne(x => x.Code == Code);
            _activationCodes.Delete(activationCode.Id);
        }

        public bool VerifyCode(string Code)
        {
            return _activationCodes.Exists(x => x.Code == Code);
        }
    }
}
