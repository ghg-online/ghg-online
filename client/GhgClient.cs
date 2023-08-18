using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using server.Protos;

namespace client
{
    public class GhgClient
    {
        private readonly GrpcChannel channel;
        private readonly Account.AccountClient accountClient;
        private string? jwtToken = null;
        private string? username = null;
        private Grpc.Core.Metadata? header = null; // where jwt token is stored when sending request

        public string? Username { get => username; }

        public GhgClient(string url)
        {
            channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new client.HttpClientHandlerForceToUseHttp1_1() });
            accountClient = new Account.AccountClient(channel);

            // Send an empty request in order to establish connection, otherwise the first request will be very slow
            accountClient.LoginAsync(new LoginRequest { Username = "", Password = "" });
        }

        public void Register(string username, string password, string activation_code)
        {
            var request = new RegisterRequest
            {
                Username = username,
                Password = password,
                ActivationCode = activation_code,
            };
            var response = accountClient.Register(request);
            Console.WriteLine(response.Message);
        }

        public void Login(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
            };
            var response = accountClient.Login(request);
            if (response.Success)
            {
                jwtToken = response.JwtToken;
                this.username = username;
                header = new() { { "Authorization", $"Bearer {jwtToken}" } };
                Console.WriteLine("Token loaded, login success!");
            }
            else
            {
                Console.WriteLine(response.Message);
            }
        }

        public void GenerateActivationCode(int number)
        {
            var request = new GenerateActivationCodeRequest { Number = number };
            var respond = accountClient.GenerateActivationCode(request, header);
            if (respond.Success)
            {
                Console.WriteLine("Activation code generated:");
                Console.WriteLine(respond.ActivationCode);
            }
            else
            {
                Console.WriteLine(respond.Message);
            }
        }

        private static string GetPasswordFromConsole()
        {
            Console.Write("Your current password: ");
            return Console.ReadLine() ?? "";
        }

        public void ChangePassword(string target_username, string new_password)
        {
            var request = new ChangePasswordRequest
            {
                Password = GetPasswordFromConsole(),
                NewPassword = new_password,
                TargetUsername = target_username,
            };
            var respond = accountClient.ChangePassword(request, header);
            Console.WriteLine(respond.Message);
        }

        public void ChangeUsername(string target_username, string new_username)
        {
            string password = GetPasswordFromConsole();
            var request = new ChangeUsernameRequest
            {
                TargetUsername = target_username,
                Password = password,
                NewUsername = new_username,
            };
            var respond = accountClient.ChangeUsername(request, header);
            if (respond.Success)
            {
                Console.WriteLine("Username changed successfully! Login again to refresh token...");
                Login(new_username, password); // login again to update jwt token
            }
            else
            {
                Console.WriteLine(respond.Message);
            }
        }

        public void DeleteAccount(string target_username)
        {
            var request = new DeleteAccountRequest { Password = GetPasswordFromConsole(), TargetUsername = target_username };
            var respond = accountClient.DeleteAccount(request, header);
            if (respond.Success)
            {
                if (target_username == username)
                {
                    jwtToken = null;
                    username = null;
                    header = null;
                    Console.WriteLine("Account deleted, token cleared!");
                }
                else
                {
                    Console.WriteLine("Account deleted");
                }
            }
            else
            {
                Console.WriteLine(respond.Message);
            }
        }
    }
}
