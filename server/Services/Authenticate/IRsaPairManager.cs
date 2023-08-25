using System.Security.Cryptography;

namespace server.Services.Authenticate
{
    public interface IRsaPairManager : IRsaPubkeyManager
    {
        public RSA GetPrikey();
    }
}
