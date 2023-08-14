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
    private readonly string _db_filename_accounts;
    private readonly string _db_filename_activation_codes;
    private readonly string _db_filename_account_logs;
    private readonly string _jwt_secret;

    public AccountService(ILogger<AccountService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var dbConfig = configuration.GetSection("LiteDB");
        var dbPath = dbConfig.GetValue<string>("Path");
        _db_filename_accounts = Path.Combine(dbPath, "accounts.db");
        _db_filename_activation_codes = Path.Combine(dbPath, "activation_codes.db");
        _db_filename_account_logs = Path.Combine(dbPath, "account_logs.db");

        _jwt_secret = configuration.GetSection("jwt").GetValue<string>("jwt-secret");
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        using var dbAccountLogs = new LiteDatabase(_db_filename_account_logs);
        var accountLogs = dbAccountLogs.GetCollection<Entities.AccountLog>("account_logs");
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

        using var dbAccounts = new LiteDatabase(_db_filename_accounts);
        var accounts = dbAccounts.GetCollection<Entities.Account>("accounts");
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
        using var dbAccountLogs = new LiteDatabase(_db_filename_account_logs);
        var accountLogs = dbAccountLogs.GetCollection<Entities.AccountLog>("account_logs");
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

        using var dbAccount = new LiteDatabase(_db_filename_accounts);
        using var dbActivationCodes = new LiteDatabase(_db_filename_activation_codes);
        var accounts = dbAccount.GetCollection<Entities.Account>("accounts");
        var activationCodes = dbActivationCodes.GetCollection<Entities.ActivationCode>("activationCodes");

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
        else
        {
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
            activationCodes.Delete(request.ActivationCode);
            accounts.Insert(new Entities.Account
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PasswordHash = Entities.Account.HashCode(request.Password),
                Status = Entities.Account.StatusCode.Activated,
            });
            return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
        }
    }
}
