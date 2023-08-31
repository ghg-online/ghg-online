namespace server.Services.Database
{
    public interface IActivationCodeManager
    {
        public string CreateCode();
        public bool VerifyCode(string Code);
        public void UseCode(string Code);
    }
}
