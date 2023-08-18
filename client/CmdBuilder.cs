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
            var usernameArgument = new Argument<string>("user", "The username");
            var passwordArgument = new Argument<string>("password", "The password");
            var loginCommand = new Command("login", "Login with an existing account")
            {
                usernameArgument,
                passwordArgument,
            };
            Handler.SetHandler(loginCommand, ghgClient.Login, usernameArgument, passwordArgument);
            return loginCommand;
        }

        private Command BuildRegister()
        {
            var activationCodeArgument = new Argument<string>("code", "The activation code");
            var usernameArgument = new Argument<string>("user", "The username");
            var passwordArgument = new Argument<string>("password", "The password");
            var registerCommand = new Command("register", "Register a new account")
            {
                usernameArgument,
                passwordArgument,
                activationCodeArgument,
            };
            Handler.SetHandler(registerCommand, ghgClient.Register, usernameArgument, passwordArgument, activationCodeArgument);
            return registerCommand;
        }

        private Command BuildGencode()
        {
            var numberArgument = new Argument<int>("num", "The number of codes to generate");
            numberArgument.SetDefaultValue(1);
            var gencodeCommand = new Command("gencode", "Generate new activation codes")
            {
                numberArgument,
            };
            Handler.SetHandler(gencodeCommand, ghgClient.GenerateActivationCode, numberArgument);
            return gencodeCommand;
        }

        private Command BuildChangepwd()
        {
            var targetUserOption = new Option<string>("--usr", "The user whose password will be changed");
            targetUserOption.SetDefaultValue(ghgClient.Username);
            var newPasswordArgument = new Argument<string>("new", "The new password");
            var changepwdCommand = new Command("changepwd", "Change the password of an account")
            {
                targetUserOption,
                newPasswordArgument,
            };
            Handler.SetHandler(changepwdCommand, ghgClient.ChangePassword, targetUserOption, newPasswordArgument);
            return changepwdCommand;
        }

        private Command BuildChangeusr()
        {
            var targetUserOption = new Option<string>("--usr", "The user whose password will be changed");
            targetUserOption.SetDefaultValue(ghgClient.Username);
            var newUsernameArgument = new Argument<string>("new", "The new username");
            var changeusrCommand = new Command("changeusr", "Change the username of an account")
            {
                targetUserOption,
                newUsernameArgument,
            };
            Handler.SetHandler(changeusrCommand, ghgClient.ChangeUsername, targetUserOption, newUsernameArgument);
            return changeusrCommand;
        }

        private Command BuildDelete()
        {
            var targetUserOption = new Option<string>("--usr", "The user to be deleted");
            targetUserOption.SetDefaultValue(ghgClient.Username);
            var deleteCommand = new Command("delete", "Delete an account")
            {
                targetUserOption,
            };
            Handler.SetHandler(deleteCommand, ghgClient.DeleteAccount, targetUserOption);
            return deleteCommand;
        }

        public Parser Build()
        {
            var rootCommand = new RootCommand("A client for the GHG online service");
            rootCommand.Name = ">";
            rootCommand.AddCommand(BuildLogin());
            rootCommand.AddCommand(BuildRegister());
            rootCommand.AddCommand(BuildGencode());
            rootCommand.AddCommand(BuildChangepwd());
            rootCommand.AddCommand(BuildChangeusr());
            rootCommand.AddCommand(BuildDelete());

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
