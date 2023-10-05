using client.Gui;
using client.Utils;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.FileSystem;
using DirectoryInfo = server.Protos.DirectoryInfo;
using FileInfo = server.Protos.FileInfo;

namespace client.Api
{
    public class Directory : IDirectory
    {
        private FileSystemClient client;
        private Guid computerId;
        private Guid directoryId;

        private Task basicInfoTask;
        private Guid parentId;
        private string name;

        private Task childDirsTask;
        private List<DirectoryInfo> childDirs;

        private Task childFilesTask;
        private List<FileInfo> childFiles;

        public Directory(FileSystemClient client, Guid computerId, Guid directoryId)
        {
            this.client = client;
            this.computerId = computerId;
            this.directoryId = directoryId;
            basicInfoTask = SyncBasicInfo();
            parentId = Guid.Empty;
            name = string.Empty;
            childDirsTask = SyncChildDirectories();
            childDirs = new List<DirectoryInfo>();
            childFilesTask = SyncChildFiles();
            childFiles = new List<FileInfo>();
        }

        public Directory(FileSystemClient client, Guid computerId, Guid directoryId, Guid parent, string name)
        {
            this.client = client;
            this.computerId = computerId;
            this.directoryId = directoryId;
            basicInfoTask = Task.CompletedTask;
            this.parentId = parent;
            this.name = name;
            childDirsTask = SyncChildDirectories();
            childDirs = new List<DirectoryInfo>();
            childFilesTask = SyncChildFiles();
            childFiles = new List<FileInfo>();
        }

        public async Task SyncBasicInfo()
        {
            var request = new GetDirectoryInfoRequest
            {
                Computer = computerId.ToByteString(),
                Directory = directoryId.ToByteString(),
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.GetDirectoryInfoAsync, request);
                parentId = respond.Info.Parent.ToGuid();
                name = respond.Info.Name;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public async Task SyncChildFiles()
        {
            var request = new ListFilesRequest
            {
                Computer = computerId.ToByteString(),
                Directory = directoryId.ToByteString(),
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.ListFilesAsync, request);
                childFiles = respond.Infos.ToList();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public async Task SyncChildDirectories()
        {
            var request = new ListDirectoriesRequest
            {
                Computer = computerId.ToByteString(),
                Directory = directoryId.ToByteString(),
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.ListDirectoriesAsync, request);
                childDirs = respond.Infos.ToList();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
        }

        public Guid ComputerId => computerId;
        public Guid DirectoryId => directoryId;
        public Guid ParentId
        {
            get
            {
                basicInfoTask.Wait();
                return parentId;
            }
        }

        private IDirectory? parent;
        public IDirectory? Parent
        {
            get
            {
                if (parentId == Guid.Empty)
                    return null;
                parent ??= new Directory(client, computerId, parentId);
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

        public async Task<IDirectory> CreateDirectoryAsync(string name)
        {
            var request = new CreateDirectoryRequest
            {
                Computer = computerId.ToByteString(),
                Parent = directoryId.ToByteString(),
                DirectoryName = name,
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.CreateDirectoryAsync, request);
                return new Directory(client, computerId, respond.Directory.ToGuid());
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
                throw new AlreadyExistsException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                throw new InvalidNameException();
            }
        }

        public IDirectory CreateDirectory(string name)
        {
            return CreateDirectoryAsync(name).Result;
        }

        public async Task<IFile> CreateDataFileAsync(string name, byte[] data)
        {
            var request = new CreateDataFileRequest
            {
                Computer = computerId.ToByteString(),
                Parent = directoryId.ToByteString(),
                FileName = name,
                Data = data.ToByteString(),
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.CreateDataFileAsync, request);
                return new File(client, computerId, respond.File.ToGuid());
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
                throw new AlreadyExistsException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.InvalidArgument)
            {
                throw new InvalidNameException();
            }
        }

        public IFile CreateDataFile(string name, byte[] data)
        {
            return CreateDataFileAsync(name, data).Result;
        }

        public async Task DeleteAsync(bool recursive = false)
        {
            var request = new DeleteDirectoryRequest
            {
                Computer = computerId.ToByteString(),
                Directory = directoryId.ToByteString(),
                Recursive = recursive,
            };
            try
            {
                await VisualGrpc.InvokeAsync(client.DeleteDirectoryAsync, request);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.FailedPrecondition)
            {
                throw new DirectoryNotEmptyException();
            }
        }

        public void Delete(bool recursive = false)
        {
            DeleteAsync(recursive).Wait();
        }

        public async Task RenameAsync(string name)
        {
            var request = new RenameDirectoryRequest
            {
                Computer = computerId.ToByteString(),
                Directory = directoryId.ToByteString(),
                Name = name,
            };
            try
            {
                await VisualGrpc.InvokeAsync(client.RenameDirectoryAsync, request);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.AlreadyExists)
            {
                throw new AlreadyExistsException();
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new NotFoundException();
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

        public List<IFile> ChildFiles
        {
            get
            {
                childFilesTask.Wait();
                return childFiles.Select(info => (IFile)info.ToFile(client, computerId)).ToList();
            }
        }

        public List<IDirectory> ChildDirectories
        {
            get
            {
                childDirsTask.Wait();
                return childDirs.Select(info => (IDirectory)info.ToDirectory(client, computerId)).ToList();
            }
        }

        public async Task<Guid> FromPathToId(string path)
        {
            var request = new FromPathToIdRequest
            {
                Computer = computerId.ToByteString(),
                StartDirectory = directoryId.ToByteString(),
                Path = path,
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.FromPathToIdAsync, request);
                return respond.Id.ToGuid();
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
    }
}
