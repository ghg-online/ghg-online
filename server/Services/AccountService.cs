using server;
using server.Entities;
using server.Managers;

using Grpc.Core;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;
using Microsoft.AspNetCore.Authorization;

namespace server.Services;

public sealed class AccountService : Account.AccountBase
{
    private readonly ITransactionManager _transactionManager;
    private readonly IActivationCodeManager _activationCodeManager;
    private readonly IAccountManager _accountManager;
    private readonly IAccountLogger _accountLogger;

    public AccountService(ITransactionManager transactionManager, IActivationCodeManager activationCodeManager, IAccountManager accountManager, IAccountLogger accountLogger)
    {
        _transactionManager = transactionManager;
        _activationCodeManager = activationCodeManager;
        _accountManager = accountManager;
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

        string? token = _accountManager.VerifyPasswordAndGenerateToken(request.Username, request.Password);
        if (token is null)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid username or password");
            return Task.FromResult(new LoginResponse { Success = false, Message = "Invalid username or password!" });
        }
        else
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, true, "Login success");
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

        _transactionManager.BeginTrans();

        if (_activationCodeManager.VerifyCode(request.ActivationCode) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid activation code");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Invalid activation code!" });
        }

        if (_accountManager.ExistsUsername(request.Username))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Username already exists");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Username already exists!" });
        }

        _activationCodeManager.UseCode(request.ActivationCode);
        _accountManager.CreateAccount(request.Username, request.Password, Entities.Account.RoleCode.User);
        _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, true, "Register success");

        _transactionManager.Commit();

        return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
    }

    public override Task<GenerateActivationCodeResponse> GenerateActivationCode(GenerateActivationCodeRequest request, ServerCallContext context)
    {
        const Entities.AccountLog.AccountLogType accountLogType = Entities.AccountLog.AccountLogType.GenerateActivationCode;

        // Authentication is required
        var account = _accountManager.VerifyTokenAndGetAccount(context);
        if (account.IsAbleTo("GenerateActivationCode") == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, "Permission denied");
            return Task.FromResult(new GenerateActivationCodeResponse { Success = false, Message = "Permission denied!" });
        }

        if (request.Number <= 0)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, null, false, $"Number = {request.Number}");
            return Task.FromResult(new GenerateActivationCodeResponse { Success = false, Message = "Invalid number!" });
        }
        if (request.Number > 100)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, null, false, $"Number = {request.Number}");
            return Task.FromResult(new GenerateActivationCodeResponse { Success = false, Message = "Number of codes generated should < 100" });
        }

        StringBuilder stringBuilder = new();
        for (int i = 0; i < request.Number - 1; i++)
        {
            stringBuilder.Append(_activationCodeManager.CreateCode());
            stringBuilder.Append(Environment.NewLine);
        }
        stringBuilder.Append(_activationCodeManager.CreateCode());

        return Task.FromResult(new GenerateActivationCodeResponse { Success = true, Message = "Generate activation code success!", ActivationCode = stringBuilder.ToString() });
    }
}
