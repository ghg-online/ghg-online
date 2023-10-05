using client.Api;
using client.App.Abstraction;

namespace client.App
{
    public class ApplicationWrapper : Application
    {
        private GhgApi GhgApi;
        private Application Application;

        public ApplicationWrapper(GhgApi ghgApi, Application application)
        {
            GhgApi = ghgApi;
            Application = application;
        }

        public void Run()
        {
            try
            {
                Application.Run();
            }
            catch (Exception e)
            {
                GhgApi.Console.ResetColor();
                GhgApi.Console.WriteLine(e.Message);
                GhgApi.Console.WriteLine(e.StackTrace!);
            }
            GhgApi.Console.ResetColor();
            GhgApi.Console.WriteLine("\n[ Main console application exited ]");
        }
    }
}
