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
        public static void Run(string? url)
        {
            if (url == null)
            {
                Console.Write("Server url: ");
                url = Console.ReadLine();
                if (url == null)
                    throw new ArgumentNullException(nameof(url));
            }
            GhgClient ghgClient = new(url);
            Parser parser = new CmdBuilder(ghgClient).Build();
            while (true)
            {
                Console.Write("GHG > ");
                string line = Console.ReadLine() ?? "exit";
                if (line == "exit")
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                parser.InvokeAsync(line).Wait();
            }
        }
    }
}
