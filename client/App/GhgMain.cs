using client.Api;
using client.Api.Abstraction;
using client.App.Abstraction;
using client.App.Adapter;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using IConsole = client.Api.Abstraction.IConsole;
using SysColor = System.Drawing.Color;

namespace client.App
{
    public class GhgMain : Application
    {
        private readonly IGhgApi GhgApi;
        private readonly IConsole Console;
        private IDirectory currentDir = null!;

        public GhgMain(IGhgApi api)
        {
            GhgApi = api;
            Console = api.Console;
        }

        public void Run()
        {
            Console.WriteLine("Welcome to GHG Online!");
            Console.WriteLine($"Current username: \t{GhgApi.Username}");

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
            Console.WriteLine($"Computer Name: \t\t{myComputer.Name}");
            currentDir = myComputer.RootDirectory;
            Console.WriteLine("");

            var parser = BuildParser();
            var consoleAdapter = new CommandLineConsoleAdapter(GhgApi);
            while (true)
            {
                Console.SetForegroundColor(SysColor.Yellow);
                Console.Write(GetPrompt());
                Console.ResetColor();
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;
                parser.InvokeAsync(input, consoleAdapter);
            }
        }

        public void DoLs(bool longFormat = false)
        {
            var dirs = currentDir.ChildDirectories;
            var files = currentDir.ChildFiles;
            dirs.Sort((x, y) => x.Name!.CompareTo(y.Name));
            // Ignore nullability because null directory name is only for root directory,
            // and currentDir.ChildDirectories never contains root directory.
            files.Sort((x, y) => x.Name.CompareTo(y.Name));
            if (dirs.Count == 0 && files.Count == 0)
                return;
            if (longFormat)
            {
                foreach (var dir in dirs)
                {
                    Console.WriteLine($"{dir.Name,-20} <dir>");
                }
                foreach (var file in files)
                {
                    Console.WriteLine($"{file.Name,-20}");
                }
            }
            else
            {
                foreach (var dir in dirs)
                {
                    Console.Write($"{dir.Name} ");
                }
                Console.SetForegroundColor(SysColor.White);
                foreach (var file in files)
                {
                    Console.WriteLine($"{file.Name} ");
                }
                Console.WriteLine("");
                Console.ResetColor();
            }
        }

        public void DoCd(string path)
        {
            try
            {
                currentDir = currentDir.SeekDirectory(path);
            }
            catch (NotFoundException)
            {
                Console.WriteLine($"Directory \"{path}\" do not exists!");
            }
        }

        private string GetPrompt()
        {
            return $"[root@{currentDir.Name ?? "/"}]# ";
        }

        private Parser BuildParser()
        {
            var builder = new CommandLineBuilder();
            builder.UseTypoCorrections();
            builder.UseParseErrorReporting();
            builder.UseHelp();
            builder.Command.Name = "$";
            builder.Command.AddCommand(BuildCommandLs());
            builder.Command.AddCommand(BuildCommandCd());
            return builder.Build();
        }

        private Command BuildCommandLs()
        {
            var cmd = new Command("ls", "List files and directories");
            var optionLongFormat = new Option<bool>("-l", "List in long format");
            cmd.AddOption(optionLongFormat);
            Handler.SetHandler(cmd, DoLs, optionLongFormat);
            return cmd;
        }

        private Command BuildCommandCd()
        {
            var cmd = new Command("cd", "Change directory");
            var argumentPath = new Argument<string>("path", "Path to change");
            cmd.AddArgument(argumentPath);
            Handler.SetHandler(cmd, DoCd, argumentPath);
            return cmd;
        }
    }
}
