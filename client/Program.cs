// .\client.exe login --url https://localhost:2333 --usr user --pwd 123456
// .\client.exe register --url https://localhost:2333 --usr user --pwd 123456 --code 2a01cf54-65bb-4ba8-99a9-71b1f4cc74e2

using Grpc.Net.Client;
using server;
using System.Net.Http;
using System.Threading.Channels;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using client;

var rootCommand = new RootCommand("A client for the GHG online service");

var urlOption = new Option<string>("--url", "The url of the remote server");
urlOption.AddAlias("-u");

rootCommand.AddGlobalOption(urlOption);
rootCommand.SetHandler(CmdShell.Run, urlOption);

var builder = new CommandLineBuilder(rootCommand);
builder.UseHelp().UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination();
var parser = builder.Build();
return parser.Invoke(args);
