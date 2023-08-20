using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace server.Services.Authenticate
{
    public class RsaPairManager : IRsaPairManager, IRsaPubkeyManager
    {
        readonly ILogger<RsaPairManager> logger;
        readonly string prikeyPath;
        readonly string pubkeyPath;
        readonly string jwkPath;
        RSA? rsaPrikey = null;
        RSA? rsaPubkey = null;

        public RsaPairManager(IConfiguration configuration, ILogger<RsaPairManager> logger)
        {
            string basePath = configuration["Data:BasePath"];
            string rsaPath = Path.Combine(basePath, configuration["Data:Directory:JwtRsa"]);
            if (Directory.Exists(rsaPath) == false) Directory.CreateDirectory(rsaPath);
            prikeyPath = Path.Combine(rsaPath, configuration["Data:Filename:JwtRsaPrikey"]);
            pubkeyPath = Path.Combine(rsaPath, configuration["Data:Filename:JwtRsaPubkey"]);
            jwkPath = Path.Combine(rsaPath, configuration["Data:Filename:Jwk"]);
            this.logger = logger;
        }

        public RSA GetPrikey()
        {
            if (rsaPrikey == null)
            {
                bool prikeyExists = File.Exists(prikeyPath);
                bool pubkeyExists = File.Exists(pubkeyPath);
                if (prikeyExists && pubkeyExists)
                {
                    rsaPrikey = RSA.Create();
                    rsaPrikey.ImportRSAPrivateKey(Convert.FromBase64String(File.ReadAllText(prikeyPath)), out _);
                }
                else if (!prikeyExists && !pubkeyExists)
                {
                    CreatePair();
                    CreateJwk();
                    return GetPrikey();
                }
                else if (prikeyExists && !pubkeyExists) throw new Exception("Prikey exists but pubkey not exists");
                else if (!prikeyExists && pubkeyExists) throw new Exception("Pubkey exists but prikey not exists");
                else throw new Exception("Logic error");
            }
            return rsaPrikey;
        }

        public RSA GetPubkey()
        {
            if (rsaPubkey == null)
            {
                bool prikeyExists = File.Exists(prikeyPath);
                bool pubkeyExists = File.Exists(pubkeyPath);
                if (prikeyExists && pubkeyExists)
                {
                    rsaPubkey = RSA.Create();
                    rsaPubkey.ImportRSAPublicKey(Convert.FromBase64String(File.ReadAllText(pubkeyPath)), out _);
                }
                else if (!prikeyExists && !pubkeyExists)
                {
                    CreatePair();
                    CreateJwk();
                    return GetPubkey();
                }
                else if (prikeyExists && !pubkeyExists) throw new Exception("Prikey exists but pubkey not exists");
                else if (!prikeyExists && pubkeyExists) throw new Exception("Pubkey exists but prikey not exists");
                else throw new Exception("Logic error");
            }
            return rsaPubkey;
        }

        private void CreatePair()
        {
            logger.LogInformation("Creating RSA key pair");
            RSA rsa = RSA.Create(2048);
            byte[] prikey = rsa.ExportRSAPrivateKey();
            byte[] pubkey = rsa.ExportRSAPublicKey();
            File.WriteAllText(prikeyPath, Convert.ToBase64String(prikey));
            File.WriteAllText(pubkeyPath, Convert.ToBase64String(pubkey));
        }

        private void CreateJwk()
        {
            logger.LogInformation("Exporting JWK to {path}", jwkPath);
            RSA pubkey = GetPubkey();
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(pubkey));
            var jwkJson = new
            {
                e = jwk.E,
                kty = "RSA",
                alg = "RS256",
                n = jwk.N,
            };
            File.WriteAllText(jwkPath,
                JsonSerializer.Serialize(jwkJson, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
