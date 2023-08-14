namespace server.Entities
{
    public class AccountLog
    {
        public enum AccountLogType
        {
            Login,
            Logout,
            Register,
            ChangePassword,
            ChangeUsername
        }

        public AccountLogType Type { get; set; }
        public DateTime Time { get; set; }
        public string? Ip { get; set; }
        public string? UserName { get; set; }
        public Guid UserId { get; set; }
        public bool Success { get; set; }
        public string? Appendix { get; set; }
    }
}
