using client.Api.Abstraction;
using client.Api.Entity;
using client.Api.GrpcMiddleware;
using client.Gui;
using client.Utils;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class File : IFile
    {
        [System.Flags]
        public enum TypeCode
        {
            Readable = 1,
            Writable = 2,
            Executable = 4,
            Invokable = 8,
        }

        private readonly ResourcePool pool;
        private readonly FileSystemClient client;
        private readonly IGrpcMiddleware GrpcMiddleware;

        private readonly Guid computerId;
        private readonly Guid fileId;

        private readonly Task basicInfoTask;
        private Guid parentId;
        private string name;
        private TypeCode type;

        public File(ResourcePool pool, FileSystemClient client, Guid computerId, Guid fileId)
        {
            pool.ThrowIfAlreadyExists(fileId);

            this.pool = pool;
            this.client = client;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
            this.computerId = computerId;
            this.fileId = fileId;
            this.basicInfoTask = SyncBasicInfo();
            this.parentId = Guid.Empty;
            this.name = string.Empty;
            this.type = 0;
        }

        public File(ResourcePool pool, FileSystemClient client, FileInfoEntity info, Guid computerId)
        {
            pool.ThrowIfAlreadyExists(info.FileId);

            this.pool = pool;
            this.client = client;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
            this.computerId = computerId;
            this.fileId = info.FileId;
            this.basicInfoTask = Task.CompletedTask;
            this.parentId = info.ParentId;
            this.name = info.Name;
            this.type = info.TypeCode;
        }

        public void UpdateCache(FileInfoEntity info)
        {
            if (info.FileId != fileId)
                throw new IllegalUpdateException(typeof(File), FileId, info.FileId);

            this.parentId = info.ParentId;
            this.name = info.Name;
            this.type = info.TypeCode;
        }

        public async Task SyncAll()
        {
            await SyncBasicInfo();
        }

        public async Task SyncBasicInfo()
        {
            var request = new GetFileInfoRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
            };
            try
            {
                var respond = await GrpcMiddleware.InvokeAsync(client.GetFileInfoAsync, request);
                parentId = respond.Info.Parent.ToGuid();
                name = respond.Info.Name;
                type = (TypeCode)Enum.Parse(typeof(TypeCode), respond.Info.Type);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public Guid ComputerId => computerId;
        public Guid FileId => fileId;
        public Guid ParentId
        {
            get
            {
                basicInfoTask.Wait();
                return parentId;
            }
        }

        public IDirectory Parent
        {
            get
            {
                return pool.GetDirectory(ComputerId, ParentId);
            }
        }

        public string Name
        {
            get
            {
                basicInfoTask.Wait();
                return name;
            }
        }
        public TypeCode Type
        {
            get
            {
                basicInfoTask.Wait();
                return type;
            }
        }

        public async Task DeleteAsync()
        {
            var request = new DeleteFileRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
            };
            try
            {
                await GrpcMiddleware.InvokeAsync(client.DeleteFileAsync, request);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public void Delete()
        {
            DeleteAsync().Wait();
        }

        public async Task RenameAsync(string name)
        {
            var request = new RenameFileRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
                Name = name,
            };
            try
            {
                await GrpcMiddleware.InvokeAsync(client.RenameFileAsync, request);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
                throw new AlreadyExistsException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                throw new InvalidNameException();
            }
        }

        public void Rename(string name)
        {
            RenameAsync(name).Wait();
        }

        public async Task ModifyAsync(byte[] data)
        {
            var request = new ModifyDataFileRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
                Data = data.ToByteString(),
            };
            try
            {
                await GrpcMiddleware.InvokeAsync(client.ModifyDataFileAsync, request);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                throw new InvalidOperationException();
            }
        }

        public void Modify(byte[] data)
        {
            ModifyAsync(data).Wait();
        }

        public async Task<byte[]> ReadAsync()
        {
            var request = new ReadDataFileRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
            };
            try
            {
                var respond = await GrpcMiddleware.InvokeAsync(client.ReadDataFileAsync, request);
                return respond.Data.ToByteArray();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                throw new InvalidOperationException();
            }
        }

        public byte[] Read()
        {
            return ReadAsync().Result;
        }
    }
}
