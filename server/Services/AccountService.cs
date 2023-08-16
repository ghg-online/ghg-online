using server;
using Grpc.Core;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;
using server.Entities;
using static server.Entities.AccountLog;
using server.Database;

namespace server.Services;

public class AccountService : Account.AccountBase
{

    private readonly ILogger<AccountService> _logger;
    private readonly string _jwt_secret;
    private readonly LiteDatabase _db;

    public AccountService(ILogger<AccountService> logger, IConfiguration configuration, IDbHolder dbHolder)
    {
        _logger = logger;
        _db = dbHolder.LiteDatabase;
        _jwt_secret = configuration.GetSection("jwt").GetValue<string>("jwt-secret");
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var accountLogs = _db.GetCollection<Entities.AccountLog>("account_logs");
        const Entities.AccountLog.AccountLogType accountLogType = Entities.AccountLog.AccountLogType.Login;
        if (string.IsNullOrEmpty(request.Username))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = ""
            });
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = ""
            });
            return Task.FromResult(new LoginResponse { Success = false, Message = "Empty password!" });
        }
        var accounts = _db.GetCollection<Entities.Account>("accounts");
        accounts.EnsureIndex(x => x.Username);
        var account = accounts.FindOne(x => x.Username == request.Username);
        if (account == null || account.PasswordHash != Entities.Account.HashCode(request.Password))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = account == null ? Guid.Empty : account.Id,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = ""
            });
            return Task.FromResult(new LoginResponse { Success = false, Message = "Invalid username or password!" });
        }
        else
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = account.Id,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = true,
                Appendix = ""
            });
            string token = account.GenerateToken(_jwt_secret);
            return Task.FromResult(new LoginResponse { Success = true, Message = "Login success!", JwtToken = token });
        }
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        var accountLogs = _db.GetCollection<Entities.AccountLog>("account_logs");
        const Entities.AccountLog.AccountLogType accountLogType = Entities.AccountLog.AccountLogType.Register;
        if (string.IsNullOrEmpty(request.Username))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty username!" });
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty password!" });
        }
        if (string.IsNullOrEmpty(request.ActivationCode))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Empty activation code!" });
        }
        var accounts = _db.GetCollection<Entities.Account>("accounts");
        var activationCodes = _db.GetCollection<Entities.ActivationCode>("activationCodes");

        if (!activationCodes.Exists(x => x.Code == request.ActivationCode))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Invalid activation code!" });
        }

        if (accounts.Exists(x => x.Username == request.Username))
        {
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = false,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
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
            activationCodes.Delete(request.ActivationCode);
            accounts.Insert(account);
            accountLogs.Insert(new Entities.AccountLog
            {
                Type = accountLogType,
                UserId = Guid.Empty,
                Time = DateTime.UtcNow,
                Ip = context.Peer,
                UserName = request.Username,
                Success = true,
                Appendix = $"ActivationCode:{request.ActivationCode}"
            });
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
