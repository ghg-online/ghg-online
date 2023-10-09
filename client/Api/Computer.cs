using client.Api.Abstraction;
using client.Api.Entity;
using client.Api.GrpcMiddleware;
using client.Utils;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.Computer;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class Computer : IComputer
    {
        private readonly ResourcePool pool;
        private readonly ComputerClient computerClient;
        private readonly FileSystemClient fileSystemClient;
        private readonly IGrpcMiddleware GrpcMiddleware;

        private readonly Guid computerId;

        private readonly Task basicInfoTask;
        private string? name = null;
        private Guid ownerAccountId;
        private Guid rootDirectoryId;


        public Computer(ResourcePool pool, ComputerClient client,
            FileSystemClient fileSystemClient, Guid computerId)
        {
            pool.ThrowIfAlreadyExists(computerId);

            this.pool = pool;
            computerClient = client;
            this.fileSystemClient = fileSystemClient;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
            this.computerId = computerId;
            basicInfoTask = SyncBasicInfo();
        }

        public Computer(ResourcePool pool, ComputerClient client,
                       FileSystemClient fileSystemClient, ComputerInfoEntity info)
        {
            pool.ThrowIfAlreadyExists(info.ComputerId);

            this.pool = pool;
            computerClient = client;
            this.fileSystemClient = fileSystemClient;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
            computerId = info.ComputerId;
            basicInfoTask = Task.CompletedTask;
            name = info.Name;
            ownerAccountId = info.OwnerAccountId;
            rootDirectoryId = info.RootDirectoryId;
        }

        public void UpdateCache(ComputerInfoEntity info)
        {
            if (info.ComputerId != computerId)
                throw new IllegalUpdateException(typeof(Computer), ComputerId, info.ComputerId);
            name = info.Name;
            ownerAccountId = info.OwnerAccountId;
            rootDirectoryId = info.RootDirectoryId;
        }

        public Guid ComputerId => computerId;
        public string Name
        {
            get
            {
                basicInfoTask.Wait();
                return name!;
            }
        }
        public Guid OwnerAccountId
        {
            get
            {
                basicInfoTask.Wait();
                return ownerAccountId;
            }
        }
        public Guid RootDirectoryId
        {
            get
            {
                basicInfoTask.Wait();
                return rootDirectoryId;
            }
        }

        public IDirectory RootDirectory
        {
            get
            {
                return pool.GetDirectory(ComputerId, RootDirectoryId);
            }
        }

        public async Task SyncAll()
        {
            await SyncBasicInfo();
        }

        public async Task SyncBasicInfo()
        {
            var request = new GetComputerInfoRequest
            {
                ComputerId = computerId.ToByteString(),
            };
            try
            {
                var respond = await GrpcMiddleware.InvokeAsync(computerClient.GetComputerInfoAsync, request);
                name = respond.Info.Name;
                ownerAccountId = respond.Info.Owner.ToGuid();
                rootDirectoryId = respond.Info.RootDirectory.ToGuid();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public async Task<bool> IsDirectoryAsync(Guid id)
        {
            var request = new FromIdToPathRequest
            {
                Computer = computerId.ToByteString(),
                Id = id.ToByteString()
            };
            try
            {
                var respond = await GrpcMiddleware.InvokeAsync(fileSystemClient.FromIdToPathAsync, request);
                return respond.IsDirectory;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DataLoss)
            {
                throw new DamagedFileSystemStructureException(e.Message);
            }
        }

        public bool IsDirectory(Guid id)
        {
            return IsDirectoryAsync(id).GetResultWithoutAggregateException();
        }

        public async Task<string> FromIdToPathAsync(Guid id)
        {
            var request = new FromIdToPathRequest
            {
                Computer = computerId.ToByteString(),
                Id = id.ToByteString()
            };
            try
            {
                var respond = await GrpcMiddleware.InvokeAsync(fileSystemClient.FromIdToPathAsync, request);
                return respond.Path;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DataLoss)
            {
                throw new DamagedFileSystemStructureException(e.Message);
            }
        }

        public string FromIdToPath(Guid id)
        {
            return FromIdToPathAsync(id).GetResultWithoutAggregateException();
        }
    }
}
