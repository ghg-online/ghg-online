using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.CommandLine.Builder;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using server;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace client
{
    class CmdBuilder
    {
        private readonly GhgClient ghgClient;

        public CmdBuilder(GhgClient ghgClient)
        {
            this.ghgClient = ghgClient;
        }

        private Command BuildLogin()
        {
            var usernameOption = new Option<string>("--usr", "The username") { IsRequired = true };
            var passwordOption = new Option<string>("--pwd", "The password") { IsRequired = true };
            var loginCommand = new Command("login", "Login with an existing account")
            {
                usernameOption,
                passwordOption,
            };
            Handler.SetHandler(loginCommand, ghgClient.Login, usernameOption, passwordOption);
            return loginCommand;
        }

        private Command BuildRegister()
        {
            var activationCodenewOption = new Option<string>("--code", "The activation code") { IsRequired = true };
            var usernameOption = new Option<string>("--usr", "The username") { IsRequired = true };
            var passwordOption = new Option<string>("--pwd", "The password") { IsRequired = true };
            var registerCommand = new Command("register", "Register a new account")
            {
                activationCodenewOption,
                usernameOption,
                passwordOption,
            };
            Handler.SetHandler(registerCommand, ghgClient.Register, activationCodenewOption, usernameOption, passwordOption);
            return registerCommand;
        }

        public Parser Build()
        {
            var rootCommand = new RootCommand("A client for the GHG online service");
            rootCommand.AddCommand(BuildLogin());
            rootCommand.AddCommand(BuildRegister());

            var builder = new CommandLineBuilder(rootCommand);
            builder.UseHelp()
                   .UseEnvironmentVariableDirective()
                   .UseParseDirective()
                   .UseSuggestDirective()
                   .RegisterWithDotnetSuggest()
                   .UseTypoCorrections()
                   .UseParseErrorReporting()
                   .UseExceptionHandler()
                   .CancelOnProcessTermination();
            var parser = builder.Build();
            return parser;
        }
    }
}
