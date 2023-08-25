using server.Entities;
using static server.Entities.Account;
using server.Services.Authenticate;

namespace server.Services.Authorize
{
    public static class IsAbleToExtensions
    {
        public static bool IsAbleTo(this (Account.RoleCode, string) actor, string action, string? resource1)
        {
            switch (action)
            {
                case "GenerateActivationCode":
                    if (actor.Item1 == RoleCode.Admin)
                        return true;
                    else
                        return false;
                case "DeleteAccount":
                case "ChangeUsername":
                case "ChangePassword":
                    if (actor.Item1 == RoleCode.Admin)
                        return true;
                    else
                        if (resource1 == actor.Item2)
                        return true;
                    else
                        return false;
                default:
                    throw new Exception($"Unknown action: {action}");
            }
        }

        public static bool IsAbleTo(this Account account, string action, string? resource1)
            => (account.Role, account.Username).IsAbleTo(action, resource1);

        public static bool IsAbleTo(this TokenInfo tokenInfo, string action, string? resource1)
            => (tokenInfo.Role, tokenInfo.Username).IsAbleTo(action, resource1);
    }
}
