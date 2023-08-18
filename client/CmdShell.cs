using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    internal class CmdShell
    {
        public static string GetUrlFromConsole()
        {
            string? url = null;
            while (url == null)
            {
                Console.Write("Server url: ");
                url = Console.ReadLine();
                if (url == null)
                    throw new ArgumentNullException(nameof(url));
                if (System.Uri.IsWellFormedUriString(url, System.UriKind.Absolute) == false)
                {
                    url = null;
                    Console.WriteLine("Invalid url!");
                }
            }
            return url;
        }

        public static void Run(string? url)
        {
            GhgClient ghgClient = new(url ?? GetUrlFromConsole());
            while (true)
            {
                if(ghgClient.Username != null)
                {
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(ghgClient.Username);
                    Console.ResetColor();
                    Console.Write("] > ");
                }
                else
                {
                    Console.Write("> ");
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                Parser parser = new CmdBuilder(ghgClient).Build(); // This must be done every time because the parser is stateful
                string line = Console.ReadLine() ?? "exit";
                Console.ResetColor();
                if (line == "exit")
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                parser.InvokeAsync(line).Wait();
            }
        }
    }
}
