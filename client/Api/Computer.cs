using client.Gui;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class Computer : IComputer
    {
        private Guid computerId;
        private string name;
        private Guid ownerAccountId;
        private Guid rootDirectoryId;
        private FileSystemClient fileSystemClient;

        public Computer(
            Guid computerId,
            string name,
            Guid ownerAccountId,
            Guid rootDirectoryId,
            FileSystemClient fileSystemClient)
        {
            this.computerId = computerId;
            this.name = name;
            this.ownerAccountId = ownerAccountId;
            this.rootDirectoryId = rootDirectoryId;
            this.fileSystemClient = fileSystemClient;
        }

        public string Name => name;
        public Guid OwnerAccountId => ownerAccountId;
        public Guid RootDirectoryId => rootDirectoryId;

        private IDirectory? rootDirectory = null;
        public IDirectory RootDirectory
        {
            get
            {
                rootDirectory ??= new Directory(fileSystemClient, computerId, RootDirectoryId);
                return rootDirectory;
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
            return IsDirectoryAsync(id).Result;
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
            return FromIdToPathAsync(id).Result;
        }
    }
}
