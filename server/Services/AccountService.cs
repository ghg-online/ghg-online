using Grpc.Core;
using server;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;

namespace server.Services;

public class AccountService : Account.AccountBase
{
    public const string _password_sha256_secret = "_password_sha256_secret_ghg_online";
    public const string _db_filename = "account.db";

    private readonly ILogger<AccountService> _logger;

    public AccountService(ILogger<AccountService> logger)
    {
        _logger = logger;

        using var db = new LiteDatabase(_db_filename);
        var activationCodes = db.GetCollection<ActivationCode>("activationCodes");
        if (activationCodes.Count() == 0)
        {
            activationCodes.Insert(new ActivationCode { Code = "test-activation-code" });
        }

    }

    public class Account
    {
        public enum StatusCode
        {
            Activated = 0,
            Deleted = 1,
        }
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; } // using HmacSHA256 Base64
        public StatusCode Status { get; set; }
        public static string HashCode(string origin)
        {
            byte[] keyByte = Encoding.GetEncoding("utf-8").GetBytes(_password_sha256_secret);
            byte[] messageBytes = Encoding.GetEncoding("utf-8").GetBytes(origin);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
    }

    public class ActivationCode
    {
        [BsonId]
        public string? Code { get; set; }
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty password!" });
        }
        using var db = new LiteDatabase(_db_filename);
        var accounts = db.GetCollection<Account>("accounts");
        var account = accounts.FindOne(x => x.Username == request.Username);
        if (account == null)
        {
            return Task.FromResult(new LoginResponse { Success = false, Message = "Username not exists!" });
        }
        else
        {
            if (account.PasswordHash != Account.HashCode(request.Password))
            {
                return Task.FromResult(new LoginResponse { Success = false, Message = "Password incorrect!" });
            }
            else
            {
                _logger.LogInformation($"Login success: {request.Username}");
                // --todo-- generate token
                string token = "test-token";
                return Task.FromResult(new LoginResponse { Success = true, Message = "Login success!", JwtToken = token });
            }
        }
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty password!" });
        }
        if (string.IsNullOrEmpty(request.ActivationCode))
        {
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty activation code!" });
        }

        using var db = new LiteDatabase(_db_filename);
        var accounts = db.GetCollection<Account>("accounts");
        var activationCodes = db.GetCollection<ActivationCode>("activationCodes");

        if (accounts.Exists(x => x.Username == request.Username))
        {
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Username already exists!" });
        }

        if (!activationCodes.Exists(x => x.Code == request.ActivationCode))
        {
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Invalid activation code!" });
        }
        else
        {
            activationCodes.Delete(request.ActivationCode);
            accounts.Insert(new Account
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PasswordHash = Account.HashCode(request.Password),
                Status = Account.StatusCode.Activated,
            });
            _logger.LogInformation($"Register success: {request.Username}");
            return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
        }
    }
}
