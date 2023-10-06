using client.Gui;
using client.Utils;
using Grpc.Core;
using server.Protos;
using static server.Protos.Account;
using static server.Protos.Computer;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class GhgApi : IGhgApi
    {
        public GhgApi(Stream consoleStream)
        {
            Console = new Console(consoleStream);
        }

        public IConsole Console { get; }

        public string Username
        {
            get
            {
                return ConnectionInfo.Username;
            }
        }

        private AccountClient? accountClient = null;
        public AccountClient AccountClient
        {
            get
            {
                accountClient ??= new AccountClient(ConnectionInfo.GrpcChannel);
                return accountClient;
            }
        }

        private ComputerClient? computerClient = null;
        public ComputerClient ComputerClient
        {
            get
            {
                computerClient ??= new ComputerClient(ConnectionInfo.GrpcChannel);
                return computerClient;
            }
        }

        private FileSystemClient? fileSystemClient = null;
        public FileSystemClient FileSystemClient
        {
            get
            {
                fileSystemClient ??= new FileSystemClient(ConnectionInfo.GrpcChannel);
                return fileSystemClient;
            }
        }

        private Computer? myComputer = null;
        public Computer MyComputer
        {
            get
            {
                if (myComputer == null)
                {
                    var request = new GetMyComputerRequest();
                    try
                    {
                        var response = VisualGrpc.Invoke(ComputerClient.GetMyComputerAsync, request);
                        myComputer = response.Info.ToComputer(FileSystemClient);
                    }
                    catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
                    {
                        throw new NotFoundException();
                    }
                }
                return myComputer;
            }
        }
    }
}
