using server;
using Grpc.Core;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;
using server.Entities;
using static server.Entities.AccountLog;

namespace server.Services;

public class AccountService : Account.AccountBase
{

    private readonly ILogger<AccountService> _logger;
    private readonly string _connection_string;
    private readonly string _jwt_secret;

    public AccountService(ILogger<AccountService> logger, IConfiguration configuration)
    {
        _logger = logger;

        _jwt_secret = configuration.GetSection("jwt").GetValue<string>("jwt-secret");
        string dbPath = Path.GetFullPath(configuration.GetSection("LiteDB").GetValue<string>("Path"));

        _connection_string = MakeConnectionString(dbPath, "account_service.db");

        using var db = new LiteDatabase(_connection_string);
        db.GetCollection<Entities.ActivationCode>("activationCodes")
            .Insert(new Entities.ActivationCode());
    }

    private static string MakeConnectionString(string path, string filename)
    {
        return $"Filename={Path.Combine(path, filename)};Connection=Shared";
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        using var db = new LiteDatabase(_connection_string);
        var accountLogs = db.GetCollection<Entities.AccountLog>("account_logs");
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
        var accounts = db.GetCollection<Entities.Account>("accounts");
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
        using var db = new LiteDatabase(_connection_string);
        var accountLogs = db.GetCollection<Entities.AccountLog>("account_logs");
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
        var accounts = db.GetCollection<Entities.Account>("accounts");
        var activationCodes = db.GetCollection<Entities.ActivationCode>("activationCodes");

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
        db.BeginTrans();
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
            db.Rollback();
            _logger.LogError(e, "Unhandled exception while register a new user {user}", account);
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Internal error: unhandled exception" });
        }
        db.Commit();
        return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
    }
}
