using server;
using server.Entities;
using server.Helpers;

using Grpc.Core;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;

namespace server.Services;

public class AccountService : Account.AccountBase
{
    private readonly string _jwt_secret;
    private readonly ILogger<AccountService> _logger;
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<Entities.Account> _accounts;
    private readonly ILiteCollection<Entities.ActivationCode> _activationCodes;
    private readonly IAccountLogger _accountLogger;

    public AccountService(ILogger<AccountService> logger, IConfiguration configuration, IDbHolder dbHolder,
        IAccountLogger accountLogger)
    {
        _db = dbHolder.LiteDatabase;
        _logger = logger;
        _accounts = _db.GetCollection<Entities.Account>("accounts");
        _activationCodes = _db.GetCollection<Entities.ActivationCode>("activationCodes");
        _jwt_secret = configuration.GetSection("jwt").GetValue<string>("jwt-secret");
        _accountLogger = accountLogger;
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        const Entities.AccountLog.AccountLogType accountLogType = Entities.AccountLog.AccountLogType.Login;
        if (string.IsNullOrEmpty(request.Username))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Empty username");
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Empty password");
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty password!" });
        }
        _accounts.EnsureIndex(x => x.Username);
        var account = _accounts.FindOne(x => x.Username == request.Username);
        if (account == null || account.PasswordHash != Entities.Account.HashCode(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid username or password");
            return Task.FromResult(new LoginResponse { Success = false, Message = "Invalid username or password!" });
        }
        else
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, true, "Login success");
            string token = account.GenerateToken(_jwt_secret);
            return Task.FromResult(new LoginResponse { Success = true, Message = "Login success!", JwtToken = token });
        }
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        const Entities.AccountLog.AccountLogType accountLogType = Entities.AccountLog.AccountLogType.Register;
        if (string.IsNullOrEmpty(request.Username))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Empty username");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Empty password");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty password!" });
        }
        if (string.IsNullOrEmpty(request.ActivationCode))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Empty activation code");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty activation code!" });
        }

        if (!_activationCodes.Exists(x => x.Code == request.ActivationCode))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid activation code");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Invalid activation code!" });
        }

        if (_accounts.Exists(x => x.Username == request.Username))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Username already exists");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Username already exists!" });
        }

        Entities.Account account = new Entities.Account
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = Entities.Account.HashCode(request.Password),
            Status = Entities.Account.StatusCode.Activated,
        };
        _db.BeginTrans();
        try
        {
            _activationCodes.Delete(request.ActivationCode);
            _accounts.Insert(account);
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, true, "Register success");
        }
        catch (Exception e)
        {
            _db.Rollback();
            _logger.LogError(e, "Unhandled exception while register a new user {user}", account);
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Internal error: unhandled exception" });
        }
        _db.Commit();
        return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
    }
}
