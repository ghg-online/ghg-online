using client.Api;
using client.App.Abstraction;
using SysColor = System.Drawing.Color;

namespace client.App
{
    public class GhgMain : Application
    {
        private readonly IGhgApi GhgApi;
        private readonly IConsole Console;

        public GhgMain(IGhgApi api)
        {
            GhgApi = api;
            Console = api.Console;
        }

        public void Run()
        {
            Console.WriteLine("Welcome to GHG Online!");
            Console.WriteLine($"Current username: {GhgApi.Username}");

            IComputer myComputer;
            try
            {
                myComputer = GhgApi.MyComputer;
            }
            catch (NotFoundException)
            {
                Console.SetForegroundColor(SysColor.Red);
                Console.WriteLine("Fail to load your personal computer");
                Console.ResetColor();
                return;
            }
            Console.WriteLine($"Computer Name: {myComputer.Name}");
            IDirectory currentDir = myComputer.RootDirectory;
            Console.WriteLine("");

            while (true)
            {
                Console.SetForegroundColor(SysColor.Yellow);
                Console.Write($"[root@{currentDir.Name}]# ");
                Console.ResetColor();
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;
                Console.WriteLine(input);
            }
        }
    }
}
