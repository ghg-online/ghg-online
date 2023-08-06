// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using server;

// from http://duoduokou.com/csharp/64085357176954393069.html
// disable ssl certificate validation
var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
var httpClient = new HttpClient(httpClientHandler);

// connect to server, port = 2333, using https without ssl certificate validation
var channel = GrpcChannel.ForAddress("https://127.0.0.1:2333", new GrpcChannelOptions() { HttpClient = httpClient });

var client = new Greeter.GreeterClient(channel);
var response = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" }).ResponseAsync;
Console.WriteLine($"Server reply: {response.Message}");
