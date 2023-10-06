using client.Api;
using System.CommandLine.IO;
using System.Drawing;
using ICommandLineConsole = System.CommandLine.IConsole;

namespace client.App.Adapter
{
    public class CommandLineConsoleAdapter : ICommandLineConsole
    {
        public CommandLineConsoleAdapter(IGhgApi api)
        {
            Out = new CommandLineStandardStreamWriterAdapter(api, false);
            Error = new CommandLineStandardStreamWriterAdapter(api, true);
        }

        public IStandardStreamWriter Out { get; }

        public bool IsOutputRedirected => true;

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected => true;

        public bool IsInputRedirected => true;
    }

    public class CommandLineStandardStreamWriterAdapter : IStandardStreamWriter
    {
        private readonly IGhgApi GhgApi;
        private readonly bool ForError;

        public CommandLineStandardStreamWriterAdapter(IGhgApi api, bool forError)
        {
            GhgApi = api;
            ForError = forError;
        }

        public void Write(string? value)
        {
            if (value == null)
                return;
            if (ForError)
                GhgApi.Console.SetForegroundColor(Color.Red);
            GhgApi.Console.Write(value);
            if (ForError)
                GhgApi.Console.ResetColor();
        }
    }
}
