using Grpc.Core;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using server.Entities;
using server.Services.Authenticate;
using server.Services.Database;

namespace server.Services.Authorize
{
    public class AuthHelper
    {
        readonly ILogger<AuthHelper> logger;
        readonly IAccountManager accountManager;
        readonly ITokenService tokenService;

        public AuthHelper(IAccountManager accountManager, ITokenService tokenService, ILogger<AuthHelper> logger)
        {
            this.accountManager = accountManager;
            this.tokenService = tokenService;
            this.logger = logger;
        }

        public void EnsurePermission(string token, out string username, string action, string? resource1 = null)
        {
            if (string.IsNullOrEmpty(token))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Empty token"));
            TokenInfo tokenInfo;
            try
            {
                tokenInfo = tokenService.VerifyTokenAndGetInfo(token);
            }
            catch (SecurityTokenValidationException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token"));
            }
            catch (System.Exception)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Internal error"));
            }
            if (tokenInfo.IsAbleTo(action, resource1) == false)
                throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"));
            username = tokenInfo.Username;
        }

        public void EnsurePermission(ServerCallContext context, out string username, string action, string? resource1 = null, string? password = null)
        {
            string token = context.RequestHeaders.GetValue("Authorization")?.Replace("Bearer ", "")
                    ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication required"));
            if (password == null) EnsurePermission(token, out username, action, resource1);
            else
            {
                if (string.IsNullOrEmpty(token))
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Empty token"));
                TokenInfo tokenInfo;
                try
                {
                    tokenInfo = tokenService.VerifyTokenAndGetInfo(token);
                }
                catch (SecurityTokenValidationException)
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid token"));
                }
                catch (System.Exception)
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Internal error"));
                }
                Account account;
                try
                {
                    account = accountManager.QueryAccount(tokenInfo.Username);
                }
                catch (LiteDB.LiteException)
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Internal error"));
                }
                if (account.PasswordHash != Account.HashCode(password, tokenInfo.Username))
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid password"));
                if (tokenInfo.IsAbleTo(action, resource1) == false)
                    throw new RpcException(new Status(StatusCode.PermissionDenied, "Permission denied"));
                username = tokenInfo.Username;
            }
        }
    }
}
