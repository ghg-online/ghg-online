namespace server.Managers
{
    public interface IAccountManager
    {
        void CreateAccount(string username, string password, Entities.Account.RoleCode role);
        void DeleteAccount(string username);
        public bool ExistsUsername(string username);
        string? VerifyPasswordAndGenerateToken(string username, string password); // return null if failed
        Entities.Account VerifyTokenAndGetAccount(string token);
    }
}
