namespace client.Api
{
    public class GhgApi : IGhgApi
    {
        public GhgApi(Stream consoleStream)
        {
            Console = new Console(consoleStream);
        }

        public IConsole Console { get; }

        public string Username
        {
            get
            {
                return ConnectionInfo.Username;
            }
        }
    }
}
