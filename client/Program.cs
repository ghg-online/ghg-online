// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using server;
using System.Net.Http;
using System.Threading.Channels;

string remote_url = "https://test-ghg-online-root-qqvduikhbx.cn-hongkong.fcapp.run";
GrpcChannel channel;
if (args.Length == 0)
{
    Console.WriteLine("Usage: [--local-server | --remote-server]");
    return -1;
}
else if (args[0] == "--local-server")
    channel = GrpcChannel.ForAddress("https://127.0.0.1:2333",
        new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerDisableSslCertificateValidation() });
else if (args[0] == "--remote-server")
    channel = GrpcChannel.ForAddress(remote_url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceToUseHttp1_1() });
// channel = GrpcChannel.ForAddress(remote_url, new GrpcChannelOptions { HttpClient = new HttpClient { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower } });
// channel = GrpcChannel.ForAddress(remote_url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceVersionPolicy(HttpVersionPolicy.RequestVersionOrLower) });
else
{
    Console.WriteLine("Usage: [--local-server | --remote-server]");
    return -1;
}
var client = new Account.AccountClient(channel);

var response1 = await client.RegisterAsync(new RegisterRequest { Username = "user", Password = "pass", ActivationCode = "test-activation-code" }).ResponseAsync;
Console.WriteLine($"Server reply: {response1}");

var response2 = await client.LoginAsync(new LoginRequest { Username = "user", Password = "pass" }).ResponseAsync;
Console.WriteLine($"Server reply: {response2}");

return 0;
