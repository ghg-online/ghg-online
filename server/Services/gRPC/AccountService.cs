using server.Entities;

using Grpc.Core;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using LiteDB;
using Microsoft.AspNetCore.Authorization;
using server.Services.Database;
using server.Protos;

namespace server.Services.gRPC;

public sealed class AccountService : Protos.Account.AccountBase
{
    private readonly IActivationCodeManager _activationCodeManager;
    private readonly IAccountManager _accountManager;
    private readonly IAccountLogger _accountLogger;

    public AccountService(IActivationCodeManager activationCodeManager, IAccountManager accountManager, IAccountLogger accountLogger)
    {
        _activationCodeManager = activationCodeManager;
        _accountManager = accountManager;
        _accountLogger = accountLogger;
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.Login;
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
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.Register;
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
        if (Entities.Account.IsUsernameValid(request.Username) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid username");
            return Task.FromResult(new RegisterResponse { Success = false, Message = "Invalid username!\nUsername should be a unique string consists of 6-32 alphas(a-zA-Z), digits(0-9), underscores(_), and hyphens(-)" });
        }
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

        return Task.FromResult(new RegisterResponse { Success = true, Message = "Register success!" });
    }

    public override Task<GenerateActivationCodeResponse> GenerateActivationCode(GenerateActivationCodeRequest request, ServerCallContext context)
    {
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.GenerateActivationCode;

        // Authentication is required
        var token = _accountManager.VerifyTokenAndGetInfo(context);
        if (token.IsAbleTo("GenerateActivationCode") == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, token.Username, false, "Permission denied");
            return Task.FromResult(new GenerateActivationCodeResponse { Success = false, Message = "Permission denied!" });
        }

        if (request.Number <= 0)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, null, false, $"Number = {request.Number}");
            return Task.FromResult(new GenerateActivationCodeResponse { Success = false, Message = "Invalid number! You can only generate 1-100 codes one time" });
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

        _accountLogger.WriteLog(accountLogType, context.Peer, token.Username, true, $"Generate {request.Number} activation codes success");
        return Task.FromResult(new GenerateActivationCodeResponse { Success = true, Message = "Generate activation code success!", ActivationCode = stringBuilder.ToString() });
    }

    public override Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.ChangePassword;

        // Authentication is required
        var account = _accountManager.VerifyTokenAndGetAccount(context);
        if (account.PasswordHash != Entities.Account.HashCode(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, "Old password incurrect");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = "Old password incurrect!" });
        }
        if (account.IsAbleTo("ChangePassword", request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, "Permission denied");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = "Permission denied!" });
        }

        if (_accountManager.ExistsUsername(request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"User({request.TargetUsername}) not exists");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = $"User({request.TargetUsername}) not exists! Try login again" });
        }
        if (string.IsNullOrEmpty(request.NewPassword))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"Empty new password ({request.TargetUsername})");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = $"Empty new password! ({request.TargetUsername})" });
        }

        _accountManager.ChangePassword(request.TargetUsername, request.NewPassword);
        _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, true, $"Change password for {request.TargetUsername} success");
        return Task.FromResult(new ChangePasswordResponse { Success = true, Message = $"Change password for {request.TargetUsername} success!" });
    }

    public override Task<ChangeUsernameResponse> ChangeUsername(ChangeUsernameRequest request, ServerCallContext context)
    {
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.ChangeUsername;

        // Authentication is required
        var account = _accountManager.VerifyTokenAndGetAccount(context);
        if (account.PasswordHash != Entities.Account.HashCode(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, "Password incurrect");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = "Password incurrect!" });
        }
        if (account.IsAbleTo("ChangeUsername", request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"Permission denied when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"Permission denied" });
        }

        if (string.IsNullOrEmpty(request.TargetUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"Empty username");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"Empty username!" });
        }
        if (string.IsNullOrEmpty(request.NewUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"Empty new username when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"Empty new username!" });
        }

        if (_accountManager.ExistsUsername(request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"User not exists when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"User not exists! Try login again" });
        }
        if (_accountManager.ExistsUsername(request.NewUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"New username already exists when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"New username already exists!" });
        }

        if (Entities.Account.IsUsernameValid(request.NewUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"New username invalid when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"New username invalid!\nUsername should be a unique string consists of 6-32 alphas(a-zA-Z), digits(0-9), underscores(_), and hyphens(-)" });
        }

        _accountManager.ChangeUsername(request.TargetUsername, request.NewUsername);
        _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, true, $"Change username success for user {request.TargetUsername}");
        return Task.FromResult(new ChangeUsernameResponse { Success = true, Message = $"Change username success!" });
    }

    public override Task<DeleteAccountResponse> DeleteAccount(DeleteAccountRequest request, ServerCallContext context)
    {
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.Delete;

        // Authentication is required
        var account = _accountManager.VerifyTokenAndGetAccount(context);
        if (account.PasswordHash != Entities.Account.HashCode(request.Password))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, "Password incurrect");
            return Task.FromResult(new DeleteAccountResponse { Success = false, Message = "Password incurrect!" });
        }
        if (account.IsAbleTo("DeleteAccount", request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"Permission denied for deleting user({request.TargetUsername})");
            return Task.FromResult(new DeleteAccountResponse { Success = false, Message = $"Permission denied for deleting user({request.TargetUsername}!" });
        }

        if (!_accountManager.ExistsUsername(request.TargetUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, false, $"User({request.TargetUsername}) not exists");
            return Task.FromResult(new DeleteAccountResponse { Success = false, Message = $"User({request.TargetUsername}) not exists!" });
        }

        _accountManager.DeleteAccount(request.TargetUsername);
        _accountLogger.WriteLog(accountLogType, context.Peer, account.Username, true, $"Delete user({request.TargetUsername}) success");
        return Task.FromResult(new DeleteAccountResponse { Success = true, Message = $"Delete user({request.TargetUsername}) success!" });
    }
}
