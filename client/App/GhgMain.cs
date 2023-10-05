using client.Api;
using SysColor = System.Drawing.Color;

namespace client.App
{
    public class GhgMain
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
            Console.WriteLine("");
            while (true)
            {
                Console.SetForegroundColor(SysColor.Yellow);
                Console.Write("> ");
                Console.ResetColor();
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;
                Console.WriteLine(input);
            }
        }
    }
}
