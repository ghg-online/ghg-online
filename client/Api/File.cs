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

        FileSystemClient client;
        private Guid computerId;
        private Guid fileId;

        private Task basicInfoTask;
        private Guid parentId;
        private string name;
        private TypeCode type;

        public File(FileSystemClient client, Guid computerId, Guid fileId)
        {
            this.client = client;
            this.computerId = computerId;
            this.fileId = fileId;
            basicInfoTask = Synchronize();
            parentId = Guid.Empty;
            name = string.Empty;
            type = 0;
            ResourcePool.Instance.Register(fileId, this);
        }

        public File(FileSystemClient client, Guid computerId, Guid fileId, Guid parent, string name, TypeCode type)
        {
            this.client = client;
            this.computerId = computerId;
            this.fileId = fileId;
            basicInfoTask = Task.CompletedTask;
            this.parentId = parent;
            this.name = name;
            this.type = type;
            ResourcePool.Instance.Register(fileId, this);
        }

        public async Task SyncAll()
        {
            await Synchronize();
        }

        public async Task Synchronize()
        {
            var request = new GetFileInfoRequest
            {
                Computer = computerId.ToByteString(),
                File = fileId.ToByteString(),
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.GetFileInfoAsync, request);
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

        private IDirectory? parent = null;
        public IDirectory Parent
        {
            get
            {
                parent ??= new Directory(client, computerId, ParentId);
                return parent;
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
                await VisualGrpc.InvokeAsync(client.DeleteFileAsync, request);
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
                await VisualGrpc.InvokeAsync(client.RenameFileAsync, request);
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
                await VisualGrpc.InvokeAsync(client.ModifyDataFileAsync, request);
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
                var respond = await VisualGrpc.InvokeAsync(client.ReadDataFileAsync, request);
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
