using client.Gui;
using client.Utils;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using System.Security.Cryptography.X509Certificates;
using static server.Protos.Computer;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class Computer : IComputer
    {
        private readonly ComputerClient computerClient;

        private Guid computerId;

        Task basicInfoTask;
        private string? name = null;
        private Guid ownerAccountId;
        private Guid rootDirectoryId;

        private readonly FileSystemClient fileSystemClient;

        public Computer(ComputerClient client, Guid computerId)
        {
            computerClient = client;
            this.computerId = computerId;
            basicInfoTask = SyncBasicInfo();
            fileSystemClient = new FileSystemClient(ConnectionInfo.GrpcChannel);
        }

        public Computer(
            ComputerClient client,
            Guid computerId,
            string name,
            Guid ownerAccountId,
            Guid rootDirectoryId,
            FileSystemClient fileSystemClient)
        {
            computerClient = client;
            this.computerId = computerId;
            basicInfoTask = Task.CompletedTask;
            this.name = name;
            this.ownerAccountId = ownerAccountId;
            this.rootDirectoryId = rootDirectoryId;
            this.fileSystemClient = fileSystemClient;
            ResourcePool.Instance.Register(computerId, this);
        }

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

        private IDirectory? rootDirectory = null;
        public IDirectory RootDirectory
        {
            get
            {
                if (rootDirectory == null)
                {
                    if ((rootDirectory = ResourcePool.Instance.Get<IDirectory>(RootDirectoryId)) != null)
                        rootDirectory.SyncAll();
                    rootDirectory = new Directory(fileSystemClient, computerId, RootDirectoryId);
                }
                return rootDirectory;
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
                var respond = await VisualGrpc.InvokeAsync(computerClient.GetComputerInfoAsync, request);
                name = respond.Info.Name;
                ownerAccountId = respond.Info.Owner.ToGuid();
                rootDirectoryId = respond.Info.RootDirectory.ToGuid();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public IDirectory GetDirectoryById(Guid directoryId)
        {
            return new Directory(fileSystemClient, computerId, directoryId);
        }

        public IFile GetFileById(Guid fileId)
        {
            return new File(fileSystemClient, computerId, fileId);
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
                var respond = await VisualGrpc.InvokeAsync(fileSystemClient.FromIdToPathAsync, request);
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
                var respond = await VisualGrpc.InvokeAsync(fileSystemClient.FromIdToPathAsync, request);
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
