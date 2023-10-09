using client.Api.Abstraction;
using client.Api.Entity;
using client.Api.GrpcMiddleware;
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
        private readonly ResourcePool pool;
        private readonly FileSystemClient client;
        private readonly IGrpcMiddleware GrpcMiddleware;

        private readonly Guid computerId;
        private readonly Guid directoryId;

        private readonly Task basicInfoTask;
        private Guid parentId;
        private string? name; // null only if this is root directory

        private readonly Task childDirsTask;
        private List<DirectoryInfo> childDirInfos;
        private List<IDirectory> childDirs; // this is null event after childDirsTask is completed
                                            // it's not null until ChildDirectories is called

        private readonly Task childFilesTask;
        private List<FileInfo> childFileInfos;
        private List<IFile> childFiles; // this is null event after childFilesTask is completed
                                        // it's not null until ChildFiles is called

        public Directory(ResourcePool pool, FileSystemClient client, Guid computerId, Guid directoryId)
        {
            pool.ThrowIfAlreadyExists(directoryId);

            this.pool = pool;
            this.client = client;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
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
        }

        public Directory(ResourcePool pool, FileSystemClient client, DirectoryInfoEntity info, Guid computerId)
        {
            pool.ThrowIfAlreadyExists(info.DirectoryId);

            this.pool = pool;
            this.client = client;
            GrpcMiddleware = pool.GhgApi.GrpcMiddleware;
            this.computerId = computerId;
            directoryId = info.DirectoryId;
            basicInfoTask = Task.CompletedTask;
            parentId = info.ParentId;
            name = info.Name;
            childDirsTask = SyncChildDirectories();
            childDirInfos = null!;
            childDirs = null!;
            childFilesTask = SyncChildFiles();
            childFileInfos = null!;
            childFiles = null!;
        }

        public void UpdateCache(DirectoryInfoEntity info)
        {
            if (info.DirectoryId != directoryId)
                throw new IllegalUpdateException(typeof(Directory), DirectoryId, info.DirectoryId);
            parentId = info.ParentId;
            name = info.Name;
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
                var respond = await GrpcMiddleware.InvokeAsync(client.GetDirectoryInfoAsync, request);
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
                var respond = await GrpcMiddleware.InvokeAsync(client.ListFilesAsync, request);
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
                var respond = await GrpcMiddleware.InvokeAsync(client.ListDirectoriesAsync, request);
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

        public IDirectory? Parent
        {
            get
            {
                if (parentId == Guid.Empty)
                    return null;
                return pool.GetDirectory(ComputerId, ParentId);
            }
        }

        public IComputer? Computer
        {
            get
            {
                if (computerId == Guid.Empty)
                    return null;
                return pool.GetComputer(ComputerId);
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
                var respond = await GrpcMiddleware.InvokeAsync(client.CreateDirectoryAsync, request);
                var newDirectory = pool.GetDirectory(ComputerId, respond.Directory.ToGuid());
                childDirs.Add(newDirectory);
                _ = SyncAll();
                return newDirectory;
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
                var respond = await GrpcMiddleware.InvokeAsync(client.CreateDataFileAsync, request);
                var newFile = pool.GetFile(ComputerId, respond.File.ToGuid());
                childFiles.Add(newFile);
                _ = SyncAll();
                return newFile;
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
                await GrpcMiddleware.InvokeAsync(client.DeleteDirectoryAsync, request);
                Parent?.ChildDirectories.Remove(this);
                Parent?.SyncAll();
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
                await GrpcMiddleware.InvokeAsync(client.RenameDirectoryAsync, request);
                this.name = name;
                _ = SyncAll();
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
                childFiles ??= childFileInfos!.Select(
                    info => pool.GetFileWithUpdate(ComputerId, info.Id.ToGuid(), info.ToEntity())
                ).ToList();
                return childFiles;
            }
        }

        public List<IDirectory> ChildDirectories
        {
            get
            {
                childDirsTask.WaitWithoutAggregateException();
                childDirs ??= childDirInfos!.Select(
                    info => pool.GetDirectoryWithUpdate(ComputerId, info.Id.ToGuid(), info.ToEntity())
                ).ToList();
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
                var respond = await GrpcMiddleware.InvokeAsync(client.FromPathToIdAsync, request);
                if (respond.IsDirectory == false)
                    throw new NotFoundException();
                return pool.GetDirectory(ComputerId, respond.Id.ToGuid());
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
                var respond = await GrpcMiddleware.InvokeAsync(client.FromPathToIdAsync, request);
                if (respond.IsDirectory == true)
                    throw new NotFoundException();
                return pool.GetFile(ComputerId, respond.Id.ToGuid());
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
