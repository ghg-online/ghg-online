// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using server;
using System.Net.Http;


// from http://duoduokou.com/csharp/64085357176954393069.html
// disable ssl certificate validation
var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
var httpClient = new HttpClient(httpClientHandler);

// connect to server, port = 2333, using https without ssl certificate validation
var channel = GrpcChannel.ForAddress("https://127.0.0.1:2333", new GrpcChannelOptions() { HttpClient = httpClient });
var client = new Account.AccountClient(channel);

var response1 = await client.RegisterAsync(new RegisterRequest { Username = "user", Password = "pass", ActivationCode = "test-activation-code" }).ResponseAsync;
Console.WriteLine($"Server reply: {response1}");

var response2 = await client.LoginAsync(new LoginRequest { Username = "user", Password = "pass" }).ResponseAsync;
Console.WriteLine($"Server reply: {response2}");
