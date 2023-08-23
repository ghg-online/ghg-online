using System.Security.Cryptography;

namespace server.Services.Authenticate
{
    public interface IRsaPubkeyManager
    {

        public RSA GetPubkey();
    }
}
