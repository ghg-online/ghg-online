using client.Api.Abstraction;
using client.App.Abstraction;

namespace client.App
{
    public class ApplicationWrapper : Application
    {
        private IGhgApi GhgApi;
        private Application Application;

        public ApplicationWrapper(IGhgApi ghgApi, Application application)
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
                GhgApi.Console.WriteLine(e.ToString());
            }
            GhgApi.Console.ResetColor();
            GhgApi.Console.WriteLine("\n[ Main console application exited ]");
        }
    }
}
