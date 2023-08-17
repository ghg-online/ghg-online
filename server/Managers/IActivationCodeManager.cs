namespace server.Managers
{
    public interface IActivationCodeManager
    {
        public class CodeNotExistsException : Exception { 
            CodeNotExistsException() : base("An activation code that not exists is tried to be used") { }
        }
        public string CreateCode();
        public bool VerifyCode(string Code);
        public void UseCode(string Code);
    }
}
