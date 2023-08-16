// .\client.exe login --url https://localhost:2333 --usr user --pwd 123456
// .\client.exe register --url https://localhost:2333 --usr user --pwd 123456 --code 2a01cf54-65bb-4ba8-99a9-71b1f4cc74e2

using Grpc.Net.Client;
using server;
using System.Net.Http;
using System.Threading.Channels;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

static void Register(string activation_code, string username, string password, string url)
{
    var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceToUseHttp1_1() });
    var client = new Account.AccountClient(channel);

    var request = new RegisterRequest { Username = username, Password = password, ActivationCode = activation_code };
    Console.WriteLine($"Client request: {request}");
    var response = client.RegisterAsync(request).ResponseAsync;
    Console.WriteLine($"Server reply: {response.Result}");
}

static void Login(string username, string password, string url)
{
    var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceToUseHttp1_1() });
    var client = new Account.AccountClient(channel);

    var request = new LoginRequest { Username = username, Password = password };
    Console.WriteLine($"Client request: {request}");
    var response = client.LoginAsync(request).ResponseAsync;
    Console.WriteLine($"Server reply: {response.Result}");
}

var urlOption = new Option<string>("--url", "The url of the remote server");
urlOption.SetDefaultValue("https://grpc.ghg.org.cn");

var activationCodenewOption = new Option<string>("--code", "The activation code") { IsRequired = true };
var usernameOption = new Option<string>("--usr", "The username") { IsRequired = true };
var passwordOption = new Option<string>("--pwd", "The password") { IsRequired = true };
var registerCommand = new Command("register", "Register a new account")
{
    activationCodenewOption,
    usernameOption,
    passwordOption,
};
Handler.SetHandler(registerCommand, Register, activationCodenewOption, usernameOption, passwordOption, urlOption);

var loginCommand = new Command("login", "Login with an existing account")
{
    usernameOption,
    passwordOption,
};
Handler.SetHandler(loginCommand, Login, usernameOption, passwordOption, urlOption);

var rootCommand = new RootCommand("A client for the GHG online service");
rootCommand.AddGlobalOption(urlOption);
rootCommand.AddCommand(registerCommand);
rootCommand.AddCommand(loginCommand);

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
return await parser.InvokeAsync(args);
