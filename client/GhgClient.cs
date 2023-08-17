using Grpc.Net.Client;
using server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    public class GhgClient
    {
        private readonly GrpcChannel channel;
        private readonly Account.AccountClient accountClient;

        public GhgClient(string url)
        {
            channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceToUseHttp1_1() });
            accountClient= new Account.AccountClient(channel);
        }

        public void Register(string activation_code, string username, string password)
        {
            var request = new RegisterRequest { Username = username, Password = password, ActivationCode = activation_code };
            Console.WriteLine($"Client request: {request}");
            var response = accountClient.RegisterAsync(request).ResponseAsync;
            Console.WriteLine($"Server reply: {response.Result}");
        }

        public  void Login(string username, string password)
        {
            var request = new LoginRequest { Username = username, Password = password };
            Console.WriteLine($"Client request: {request}");
            var response = accountClient.LoginAsync(request).ResponseAsync;
            Console.WriteLine($"Server reply: {response.Result}");
        }
    }
}
