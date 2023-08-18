namespace server.Entities
{
    public class AccountLog
    {
        public enum AccountLogType
        {
            Login,
            Logout,
            Register,
            Delete,
            ChangePassword,
            ChangeUsername,
            GenerateActivationCode,
        }
        public AccountLogType Type { get; set; }
        public DateTime Time { get; set; }
        public string? Ip { get; set; }
        public string? UserName { get; set; }
        public bool Success { get; set; }
        public string? Appendix { get; set; }
    }
}
