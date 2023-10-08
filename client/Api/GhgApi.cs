using client.Api.Abstraction;
using client.Api.GrpcMiddleware;
using client.Gui;
using client.Utils;
using Grpc.Core;
using Grpc.Net.Client;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.Account;
using static server.Protos.Computer;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class GhgApi : IGhgApi
    {
        private readonly Stream consoleStream;

        private GhgApi(Stream consoleStream, ResourcePool resourcePool, GrpcChannel channel)
        {
            this.consoleStream = consoleStream;
            Console = new Console(consoleStream);
            ResourcePool = resourcePool;
            AccountClient = new AccountClient(channel);
            ComputerClient = new ComputerClient(channel);
            FileSystemClient = new FileSystemClient(channel);
        }

        public GhgApi(
            Stream consoleStream,
            ResourcePool resourcePool,
            GrpcMiddlewareCombiner grpcMiddlewareCombiner,
            IConsole console,
            AccountClient accountClient,
            ComputerClient computerClient,
            FileSystemClient fileSystemClient,
            IComputer? myComputer)
        {
            this.consoleStream = consoleStream;
            ResourcePool = resourcePool;
            this.grpcMiddlewareCombiner = grpcMiddlewareCombiner;
            Console = console;
            AccountClient = accountClient;
            ComputerClient = computerClient;
            FileSystemClient = fileSystemClient;
            this.myComputer = myComputer;
        }

        public static IGhgApi CreateInstanceWithGlobalConfiguration(Stream consoleStream)
        {
            var channel = ConnectionInfo.GrpcChannel;
            var resourcePool = new ResourcePool(channel);
            var api = new GhgApi(consoleStream, resourcePool, channel);
            resourcePool.LoadGhgApi(api);
            api.GrpcMiddlewareList.Add(new VisualGrpcAdapter());
            return api;
        }

        public IGhgApi Fork()
        {
            var api = new GhgApi(
                consoleStream,
                ResourcePool,
                grpcMiddlewareCombiner,
                Console,
                AccountClient,
                ComputerClient,
                FileSystemClient,
                myComputer);
            return api;
        }

        public ResourcePool ResourcePool { get; }

        private readonly GrpcMiddlewareCombiner grpcMiddlewareCombiner = new();
        public IGrpcMiddleware GrpcMiddleware
        {
            get
            {
                return grpcMiddlewareCombiner;
            }
        }
        public IList<IGrpcMiddleware> GrpcMiddlewareList
        {
            get
            {
                return grpcMiddlewareCombiner;
            }
        }

        public IConsole Console { get; }

        public string Username
        {
            get
            {
                return ConnectionInfo.Username;
            }
        }

        public AccountClient AccountClient { get; }

        public ComputerClient ComputerClient { get; }

        public FileSystemClient FileSystemClient { get; }

        private IComputer? myComputer = null;
        public IComputer MyComputer
        {
            get
            {
                if (myComputer == null)
                {
                    var request = new GetMyComputerRequest();
                    try
                    {
                        var response = GrpcMiddleware.Invoke(ComputerClient.GetMyComputerAsync, request);
                        myComputer = ResourcePool.GetComputerWithUpdate(response.Info.Id.ToGuid(), response.Info.ToEntity());
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
