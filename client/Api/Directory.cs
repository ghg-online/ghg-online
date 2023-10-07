using client.Gui;
using client.Utils;
using Grpc.Core;
using server.Protos;
using server.Services.gRPC.Extensions;
using static server.Protos.Computer;
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
        private string? name; // null only if this is root directory

        private Task childDirsTask;
        private List<DirectoryInfo> childDirInfos;
        private List<IDirectory> childDirs; // this is null event after childDirsTask is completed
                                            // it's not null until ChildDirectories is called

        private Task childFilesTask;
        private List<FileInfo> childFileInfos;
        private List<IFile> childFiles; // this is null event after childFilesTask is completed
                                        // it's not null until ChildFiles is called

        public Directory(FileSystemClient client, Guid computerId, Guid directoryId)
        {
            this.client = client;
            this.computerId = computerId;
            this.directoryId = directoryId;
            basicInfoTask = SyncBasicInfo();
            parentId = Guid.Empty;
            name = string.Empty;
            childDirsTask = SyncChildDirectories();
            childDirInfos = null!;
            childDirs = null!;
            childFilesTask = SyncChildFiles();
            childFileInfos = null!;
            childFiles = null!;
            ResourcePool.Instance.Register(directoryId, this);
        }

        public Directory(FileSystemClient client, Guid computerId, Guid directoryId, Guid parent, string? name)
        {
            this.client = client;
            this.computerId = computerId;
            this.directoryId = directoryId;
            basicInfoTask = Task.CompletedTask;
            this.parentId = parent;
            this.name = name;
            childDirsTask = SyncChildDirectories();
            childDirInfos = null!;
            childDirs = null!;
            childFilesTask = SyncChildFiles();
            childFileInfos = null!;
            childFiles = null!;
            ResourcePool.Instance.Register(directoryId, this);
            childDirsTask.ContinueWith((task) => { _ = ChildDirectories; });
        }

        public async Task SyncAll()
        {
            Task syncBasicInfoTask = SyncBasicInfo();
            Task syncChildDirsTask = SyncChildDirectories();
            Task syncChildFilesTask = SyncChildFiles();
            await Task.WhenAll(syncBasicInfoTask, syncChildDirsTask, syncChildFilesTask);
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
                childFileInfos = respond.Infos.ToList();
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
                childDirInfos = respond.Infos.ToList();
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
                basicInfoTask.WaitWithoutAggregateException();
                return parentId;
            }
        }

        private IDirectory? parent = null;
        public IDirectory? Parent
        {
            get
            {
                if (parentId == Guid.Empty)
                    return null;
                if (parent == null)
                {
                    if ((parent = ResourcePool.Instance.Get<IDirectory>(parentId)) != null)
                        parent.SyncAll();
                    else
                        parent = new Directory(client, computerId, parentId);
                }
                return parent;
            }
        }

        private IComputer? computer = null;
        public IComputer? Computer
        {
            get
            {
                if (computerId == Guid.Empty)
                    return null;
                if (computer == null)
                {
                    if ((computer = ResourcePool.Instance.Get<IComputer>(computerId)) != null)
                        computer.SyncAll();
                    else
                        computer = new Computer(new ComputerClient(ConnectionInfo.GrpcChannel), ComputerId);
                }
                return computer;
            }
        }

        public string? Name
        {
            get
            {
                basicInfoTask.WaitWithoutAggregateException();
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
            return CreateDirectoryAsync(name).GetResultWithoutAggregateException();
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
            return CreateDataFileAsync(name, data).GetResultWithoutAggregateException();
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
            DeleteAsync(recursive).WaitWithoutAggregateException();
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
            RenameAsync(name).WaitWithoutAggregateException();
        }

        public List<IFile> ChildFiles
        {
            get
            {
                childFilesTask.WaitWithoutAggregateException();
                childFiles ??= childFileInfos!.Select(info => (IFile)info.ToFile(client, computerId)).ToList();
                return childFiles;
            }
        }

        public List<IDirectory> ChildDirectories
        {
            get
            {
                childDirsTask.WaitWithoutAggregateException();
                childDirs ??= childDirInfos!.Select(info => (IDirectory)info.ToDirectory(client, computerId)).ToList();
                return childDirs;
            }
        }

        public async Task<IDirectory> SeekDirectoryAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (path.StartsWith("/"))
                if (Computer != null)
                {
                    string after = path[1..];
                    if (string.IsNullOrEmpty(after))
                        return Computer.RootDirectory;
                    else
                        return await Computer.RootDirectory.SeekDirectoryAsync(path[1..]);
                }
            if (false == path.Contains('/'))
            {
                if (path == ".")
                    return this;
                else if (path == "..")
                    return Parent ?? throw new NotFoundException();
                if (childDirsTask.IsCompletedSuccessfully)
                {
                    return ChildDirectories.FirstOrDefault(dir => dir?.Name == path, null)
                        ?? throw new NotFoundException();
                }
            }
            else
            {
                if (childDirsTask.IsCompletedSuccessfully)
                {
                    string firstDirName = path[..path.IndexOf('/')];
                    IDirectory dir;
                    if (firstDirName == ".")
                        dir = this;
                    else if (firstDirName == "..")
                        dir = Parent ?? throw new NotFoundException();
                    else
                        dir = ChildDirectories.FirstOrDefault(dir => dir?.Name == firstDirName, null)
                            ?? throw new NotFoundException();
                    string after = path[(path.IndexOf('/') + 1)..];
                    if (string.IsNullOrEmpty(after))
                        return dir;
                    else
                        return await dir.SeekDirectoryAsync(after);
                }
            }

            var request = new FromPathToIdRequest
            {
                Computer = computerId.ToByteString(),
                StartDirectory = directoryId.ToByteString(),
                Path = path,
            };
            try
            {
                var respond = await VisualGrpc.InvokeAsync(client.FromPathToIdAsync, request);
                if (respond.IsDirectory == false)
                    throw new NotFoundException();
                return ResourcePool.Instance.Get<IDirectory>(respond.Id.ToGuid())
                    ?? new Directory(client, computerId, respond.Id.ToGuid());
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

        public IDirectory SeekDirectory(string path)
        {
            return SeekDirectoryAsync(path).GetResultWithoutAggregateException();
        }

        public async Task<IFile> SeekFileAsync(string path)
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
                if (respond.IsDirectory == true)
                    throw new NotFoundException();
                return ResourcePool.Instance.Get<IFile>(respond.Id.ToGuid())
                    ?? new File(client, computerId, respond.Id.ToGuid());
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

        public IFile SeekFile(string path)
        {
            return SeekFileAsync(path).GetResultWithoutAggregateException();
        }
    }
}
