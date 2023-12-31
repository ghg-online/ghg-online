using server.Entities;

using Grpc.Core;
using System.Text;
using server.Services.Database;
using server.Protos;
using server.Services.Authenticate;
using server.Services.Authorize;

namespace server.Services.gRPC;

public sealed class AccountService : Protos.Account.AccountBase
{
    private readonly IActivationCodeManager _activationCodeManager;
    private readonly IAccountManager _accountManager;
    private readonly IAccountLogger _accountLogger;
    private readonly ITokenService _tokenService;
    private readonly AuthHelper _authHelper;

    public AccountService(IActivationCodeManager activationCodeManager, IAccountManager accountManager
        , IAccountLogger accountLogger, ITokenService tokenService, AuthHelper authHelper)
    {
        _activationCodeManager = activationCodeManager;
        _accountManager = accountManager;
        _accountLogger = accountLogger;
        _tokenService = tokenService;
        _authHelper = authHelper;
    }

    public override Task<PingRespond> Ping(PingRequest request, ServerCallContext context)
        => Task.FromResult(new PingRespond());

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

        if (_accountManager.ExistsUsername(request.Username))
        {
            var account = _accountManager.QueryAccount(request.Username);
            if (account.PasswordHash == Entities.Account.HashCode(request.Password, request.Username))
            {
                if (account.Status != Entities.Account.StatusCode.Activated)
                {
                    _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Not activated");
                    return Task.FromResult(new LoginResponse { Success = false, Message = "Not activated!" });
                }
                var tokenInfo = _tokenService.ConvertToTokenInfo(account);
                string token = _tokenService.SignToken(tokenInfo);
                _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, true, "Login success");
                return Task.FromResult(new LoginResponse { Success = true, Message = "Login success!", JwtToken = token });
            }
        }

        _accountLogger.WriteLog(accountLogType, context.Peer, request.Username, false, "Invalid username or password");
        return Task.FromResult(new LoginResponse { Success = false, Message = "Invalid username or password!" });
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
        _authHelper.EnsurePermission(context, out string username, action: "GenerateActivationCode");
        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.GenerateActivationCode;
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

        _accountLogger.WriteLog(accountLogType, context.Peer, username, true, $"Generate {request.Number} activation codes success");
        return Task.FromResult(new GenerateActivationCodeResponse { Success = true, Message = "Generate activation code success!", ActivationCode = stringBuilder.ToString() });
    }

    public override Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        _authHelper.EnsurePermission(context, out string username, password: request.Password
            , action: "ChangePassword", resource1: request.TargetUsername);

        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.ChangePassword;

        if (_accountManager.ExistsUsername(request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"User({request.TargetUsername}) not exists");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = $"User({request.TargetUsername}) not exists! Try login again" });
        }
        if (string.IsNullOrEmpty(request.NewPassword))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"Empty new password ({request.TargetUsername})");
            return Task.FromResult(new ChangePasswordResponse { Success = false, Message = $"Empty new password! ({request.TargetUsername})" });
        }

        _accountManager.ChangePassword(request.TargetUsername, request.NewPassword);
        _accountLogger.WriteLog(accountLogType, context.Peer, username, true, $"Change password for {request.TargetUsername} success");
        return Task.FromResult(new ChangePasswordResponse { Success = true, Message = $"Change password for {request.TargetUsername} success!" });
    }

    public override Task<ChangeUsernameResponse> ChangeUsername(ChangeUsernameRequest request, ServerCallContext context)
    {
        _authHelper.EnsurePermission(context, out string username, password: request.Password
            , action: "ChangeUsername", resource1: request.TargetUsername);

        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.ChangeUsername;

        if (string.IsNullOrEmpty(request.TargetUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"Empty username");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"Empty username!" });
        }
        if (string.IsNullOrEmpty(request.NewUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"Empty new username when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"Empty new username!" });
        }

        if (_accountManager.ExistsUsername(request.TargetUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"User not exists when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"User not exists! Try login again" });
        }
        if (_accountManager.ExistsUsername(request.NewUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"New username already exists when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"New username already exists!" });
        }

        if (Entities.Account.IsUsernameValid(request.NewUsername) == false)
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"New username invalid when changing username for {request.TargetUsername}");
            return Task.FromResult(new ChangeUsernameResponse { Success = false, Message = $"New username invalid!\nUsername should be a unique string consists of 6-32 alphas(a-zA-Z), digits(0-9), underscores(_), and hyphens(-)" });
        }

        _accountManager.ChangeUsername(request.TargetUsername, request.Password, request.NewUsername);
        _accountLogger.WriteLog(accountLogType, context.Peer, username, true, $"Change username success for user {request.TargetUsername}");
        return Task.FromResult(new ChangeUsernameResponse { Success = true, Message = $"Change username success!" });
    }

    public override Task<DeleteAccountResponse> DeleteAccount(DeleteAccountRequest request, ServerCallContext context)
    {
        _authHelper.EnsurePermission(context, out string username, password: request.Password
            , action: "DeleteAccount", resource1: request.TargetUsername);

        const AccountLog.AccountLogType accountLogType = AccountLog.AccountLogType.Delete;

        if (!_accountManager.ExistsUsername(request.TargetUsername))
        {
            _accountLogger.WriteLog(accountLogType, context.Peer, username, false, $"User({request.TargetUsername}) not exists");
            return Task.FromResult(new DeleteAccountResponse { Success = false, Message = $"User({request.TargetUsername}) not exists!" });
        }

        _accountManager.DeleteAccount(request.TargetUsername);
        _accountLogger.WriteLog(accountLogType, context.Peer, username, true, $"Delete user({request.TargetUsername}) success");
        return Task.FromResult(new DeleteAccountResponse { Success = true, Message = $"Delete user({request.TargetUsername}) success!" });
    }
}
