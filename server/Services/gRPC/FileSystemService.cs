using Google.Protobuf;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using server.Protos;
using server.Services.Authorize;
using server.Services.Database;
using server.Services.gRPC.Extensions;
using System.Text;

namespace server.Services.gRPC
{
    using File = server.Entities.File;
    using Directory = server.Entities.Directory;
    using FileType = server.Entities.File.TypeCode;

    public class FileSystemService : FileSystem.FileSystemBase
    {
        readonly AuthHelper authHelper;
        readonly IComputerManager computerManager;
        readonly IFileSystemManager fileSystemManager;
        readonly IFileDataManager fileDataManager;
        readonly ITransactionController transCtrl;

        public FileSystemService(AuthHelper authHelper, IComputerManager computerManager, IFileSystemManager fileSystemManager
            , IFileDataManager fileDataManager, ITransactionController transCtrl)
        {
            this.authHelper = authHelper;
            this.computerManager = computerManager;
            this.fileSystemManager = fileSystemManager;
            this.fileDataManager = fileDataManager;
            this.transCtrl = transCtrl;
        }

        public override Task<CreateDataFileRespond> CreateDataFile(CreateDataFileRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            if (File.IsNameValid(request.FileName) == false)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file name"));
            }

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid parent = request.Parent.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, parent) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Parent directory not found"));
            if (fileSystemManager.ExistsFile(computer, parent, request.FileName))
                throw new RpcException(new Status(StatusCode.AlreadyExists, "File already exists"));
            var id = fileSystemManager.CreateFile(computer, parent, request.FileName, FileType.Readable | FileType.Writable);
            fileDataManager.Create(computer, id, request.Data.ToByteArray());
            transaction.Commit();
            return Task<CreateDataFileRespond>.FromResult(new CreateDataFileRespond { File = id.ToByteString() });
        }

        public override Task<CreateDirectoryRespond> CreateDirectory(CreateDirectoryRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            if (Directory.IsNameValid(request.DirectoryName) == false)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid directory name"));
            }

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid parent = request.Parent.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, parent) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Parent directory not found"));
            if (fileSystemManager.ExistsDirectory(computer, parent, request.DirectoryName))
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Directory already exists"));
            var id = fileSystemManager.CreateDirectory(computer, parent, request.DirectoryName);
            transaction.Commit();
            return Task<CreateDirectoryRespond>.FromResult(new CreateDirectoryRespond { Directory = id.ToByteString() });
        }

        public override Task<DeleteDirectoryRespond> DeleteDirectory(DeleteDirectoryRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid id = request.Directory.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, id) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Directory not found"));
            if (request.Recursive)
            {
                IEnumerable<Guid> childDirectories = new List<Guid>() { id };
                IEnumerable<Guid> childFiles = new List<Guid>();
                while (childDirectories.Any() || childFiles.Any())
                {
                    var newChildDirectories = new List<Guid>();
                    var newChildFiles = new List<Guid>();
                    foreach (var file in childFiles)
                    {
                        fileSystemManager.DeleteFile(computer, file);
                    }
                    foreach (var directory in childDirectories)
                    {
                        fileSystemManager.DeleteDirectory(computer, directory);
                        newChildDirectories.AddRange(fileSystemManager.ListDirectories(computer, directory).Select(x => x.Id));
                        newChildFiles.AddRange(fileSystemManager.ListFiles(computer, directory).Select(x => x.Id));
                    }
                    childDirectories = newChildDirectories;
                    childFiles = newChildFiles;
                }
                fileSystemManager.DeleteDirectory(computer, id);
                transaction.Commit();
                return Task<DeleteDirectoryRespond>.FromResult(new DeleteDirectoryRespond());
            }
            else
            {
                if (fileSystemManager.IsDirectoryEmpty(computer, id) == false)
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, "Directory is not empty"));
                fileSystemManager.DeleteDirectory(computer, id);
                transaction.Commit();
                return Task<DeleteDirectoryRespond>.FromResult(new DeleteDirectoryRespond());
            }
        }

        public override Task<DeleteFileRespond> DeleteFile(DeleteFileRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid id = request.File.ToGuid();
            var file = fileSystemManager.GetFileById(computer, id);
            if (file == null)
                throw new RpcException(new Status(StatusCode.NotFound, "File not found"));
            fileSystemManager.DeleteFile(computer, id);
            if (file.Type.HasFlag(FileType.Readable) && file.Type.HasFlag(FileType.Writable))
                fileDataManager.Delete(computer, id);
            transaction.Commit();
            return Task<DeleteFileRespond>.FromResult(new DeleteFileRespond());
        }

        public override Task<FromIdToPathRespond> FromIdToPath(FromIdToPathRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans(); // This operation is read-only, so no need to commit or rollback
            Guid computer = request.Computer.ToGuid();
            Guid id = request.Id.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, id))
            {
                StringBuilder stringBuilder = new();
                Directory dir = fileSystemManager.GetDirectoryById(computer, id)!;
                if (dir.Parent == Guid.Empty)
                    stringBuilder.Insert(0, "/");
                while (dir.Parent != Guid.Empty)
                {
                    stringBuilder.Insert(0, dir.Name);
                    stringBuilder.Insert(0, '/');
                    dir = fileSystemManager.GetDirectoryById(computer, dir.Parent)
                        ?? throw new RpcException(new Status(StatusCode.DataLoss, "Non-root directory's parent not found"));
                }
                return Task<FromIdToPathRespond>.FromResult(new FromIdToPathRespond { IsDirectory = true, Path = stringBuilder.ToString() });
            }
            else if (fileSystemManager.ExistsFile(computer, id))
            {
                StringBuilder stringBuilder = new();
                File file = fileSystemManager.GetFileById(computer, id)!;
                stringBuilder.Insert(0, file.Name);
                stringBuilder.Insert(0, '/');
                Directory dir = fileSystemManager.GetDirectoryById(computer, file.Parent)!;
                while (dir.Parent != Guid.Empty)
                {
                    stringBuilder.Insert(0, dir.Name);
                    stringBuilder.Insert(0, '/');
                    dir = fileSystemManager.GetDirectoryById(computer, dir.Parent)
                        ?? throw new RpcException(new Status(StatusCode.DataLoss, "Non-root directory's parent not found"));
                }
                return Task<FromIdToPathRespond>.FromResult(new FromIdToPathRespond { IsDirectory = false, Path = stringBuilder.ToString() });
            }
            else
            {
                throw new RpcException(new Status(StatusCode.NotFound, "File or directory not found"));
            }
        }

        public override Task<FromPathToIdRespond> FromPathToId(FromPathToIdRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid rootDir = computerManager.QueryComputerById(computer).RootDirectory;
            Guid startDir = request.StartDirectory.ToGuid();
            string[] path = request.Path.Split('/');
            if (fileSystemManager.ExistsDirectory(computer, request.StartDirectory.ToGuid()) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Start directory not found"));
            if (path.Length == 0) return Task.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = rootDir.ToByteString() });
            if (path.Length == 1)
            {
                if (string.IsNullOrEmpty(path[0])) // case ""
                    return Task<FromPathToIdRespond>.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = rootDir.ToByteString() });
                else // case "a"
                    return FindFileOrDirectory(computer, startDir, path[0]);
            }
            if (string.IsNullOrEmpty(path[0])) // case "/..."
            {
                // Special case "/" and "/a" is considered.
                Directory current = fileSystemManager.GetDirectoryById(computer, rootDir)
                    ?? throw new RpcException(new Status(StatusCode.DataLoss, "Root directory not exists"));
                for (int i = 1; i < path.Length - 1; i++)
                {
                    string name = path[i];
                    if (name == ".") // . means current dir
                        continue;
                    else if (name == "..") // .. means parent dir
                    {
                        if (current.Id == rootDir)
                            throw new RpcException(new Status(StatusCode.NotFound, "Root directory do not have parent"));
                        else
                            current = fileSystemManager.GetDirectoryById(computer, current.Parent)
                                ?? throw new RpcException(new Status(StatusCode.DataLoss, "Non-root directory's parent not found"));
                        continue;
                    }
                    else
                        current = fileSystemManager.GetDirectoryByName(computer, current.Id, name)
                            ?? throw new RpcException(new Status(StatusCode.NotFound, "Not found"));
                }
                string finalName = path[^1]; // last element
                if (string.IsNullOrEmpty(finalName)) // case "/a/"
                    return Task<FromPathToIdRequest>.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = current.Id.ToByteString() });
                else // case "/a/b"
                    return FindFileOrDirectory(computer, current.Id, finalName);
            }
            else // case "a/..."
            {
                Directory current = fileSystemManager.GetDirectoryById(computer, startDir)
                    ?? throw new RpcException(new Status(StatusCode.DataLoss, "Root directory not exists"));
                for (int i = 0; i < path.Length - 1; i++)
                {
                    string name = path[i];
                    if (name == ".") // . means current dir
                        continue;
                    else if (name == "..") // .. means parent dir
                    {
                        if (current.Id == rootDir)
                            throw new RpcException(new Status(StatusCode.NotFound, "Root directory do not have parent"));
                        else
                            current = fileSystemManager.GetDirectoryById(computer, current.Parent)
                                ?? throw new RpcException(new Status(StatusCode.DataLoss, "Non-root directory's parent not found"));
                        continue;
                    }
                    else
                        current = fileSystemManager.GetDirectoryByName(computer, current.Id, name)
                            ?? throw new RpcException(new Status(StatusCode.NotFound, "Not found"));
                }
                string finalName = path[^1]; // last element
                if (string.IsNullOrEmpty(finalName)) // case "/a/"
                    return Task<FromPathToIdRequest>.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = current.Id.ToByteString() });
                else // case "/a/b"
                    return FindFileOrDirectory(computer, current.Id, finalName);
            }

            Task<FromPathToIdRespond> FindFileOrDirectory(Guid computer, Guid directory, string name)
            {
                if (name == ".")
                    return Task<FromPathToIdRespond>.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = directory.ToByteString() });
                if (name == "..")
                {
                    if (directory == rootDir)
                        throw new RpcException(new Status(StatusCode.NotFound, "Root directory do not have parent"));
                    else
                    {
                        var baseDir = fileSystemManager.GetDirectoryById(computer, directory)!;
                        var targetDir = fileSystemManager.GetDirectoryById(computer, baseDir.Parent)
                            ?? throw new RpcException(new Status(StatusCode.DataLoss, "Non-root directory's parent not found"));
                        return Task.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = targetDir.Id.ToByteString() });
                    }
                }
                var file = fileSystemManager.GetFileByName(computer, directory, name);
                if (file is not null)
                    return Task<FromPathToIdRespond>.FromResult(new FromPathToIdRespond() { IsDirectory = false, Id = file.Id.ToByteString() });
                var dir = fileSystemManager.GetDirectoryByName(computer, directory, name);
                if (dir is not null)
                    return Task<FromPathToIdRespond>.FromResult(new FromPathToIdRespond() { IsDirectory = true, Id = dir.Id.ToByteString() });
                throw new RpcException(new Status(StatusCode.NotFound, "Not found"));
            }
        }

        public override Task<GetDirectoryInfoRespond> GetDirectoryInfo(GetDirectoryInfoRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans(); // This is a read-only operation
            Guid computer = request.Computer.ToGuid();
            Guid directory = request.Directory.ToGuid();
            var dir = fileSystemManager.GetDirectoryById(computer, directory)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "Directory not exists"));
            return Task<GetDirectoryInfoRespond>.FromResult(new GetDirectoryInfoRespond() { Info = dir.ToDirectoryInfo() });
        }

        public override Task<GetFileInfoRespond> GetFileInfo(GetFileInfoRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans(); // This is a read-only operation
            Guid computer = request.Computer.ToGuid();
            Guid file = request.File.ToGuid();
            var f = fileSystemManager.GetFileById(computer, file)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "File not exists"));
            return Task<GetFileInfoRespond>.FromResult(new GetFileInfoRespond() { Info = f.ToFileInfo() });
        }

        public override Task<ListDirectoriesRespond> ListDirectories(ListDirectoriesRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid directory = request.Directory.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, directory) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Directory not exists"));
            var dirs = fileSystemManager.ListDirectories(computer, directory);
            if (dirs.IsNullOrEmpty())
                return Task<ListDirectoriesRespond>.FromResult(new ListDirectoriesRespond() { Infos = { } });
            else
                return Task<ListDirectoriesRespond>.FromResult(new ListDirectoriesRespond() { Infos = { dirs.Select(x => x.ToDirectoryInfo()) } });
        }

        public override Task<ListFilesRespond> ListFiles(ListFilesRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid directory = request.Directory.ToGuid();
            if (fileSystemManager.ExistsDirectory(computer, directory) == false)
                throw new RpcException(new Status(StatusCode.NotFound, "Directory not exists"));
            var files = fileSystemManager.ListFiles(computer, directory);
            if (files.IsNullOrEmpty())
                return Task<ListFilesRespond>.FromResult(new ListFilesRespond() { Infos = { } });
            else
                return Task<ListFilesRespond>.FromResult(new ListFilesRespond() { Infos = { files.Select(x => x.ToFileInfo()) } });
        }

        public override Task<ModifyDataFileRespond> ModifyDataFile(ModifyDataFileRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid file = request.File.ToGuid();
            var f = fileSystemManager.GetFileById(computer, file);
            if (f == null)
                throw new RpcException(new Status(StatusCode.NotFound, "File not exists"));
            if (false == f.Type.HasFlag(File.TypeCode.Writable)
                || false == f.Type.HasFlag(File.TypeCode.Readable))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "File is not a data file"));
            fileDataManager.Modify(computer, file, request.Data.ToArray());
            transaction.Commit();
            return Task<ModifyDataFileRespond>.FromResult(new ModifyDataFileRespond());
        }

        public override Task<ReadDataFileRespond> ReadDataFile(ReadDataFileRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            using var transaction = transCtrl.BeginTrans(); // This is a read-only operation
            Guid computer = request.Computer.ToGuid();
            Guid file = request.File.ToGuid();
            var f = fileSystemManager.GetFileById(computer, file)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "File not exists"));
            if (false == f.Type.HasFlag(File.TypeCode.Readable))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "File is not a readable file"));
            return Task<ReadDataFileRespond>.FromResult(new ReadDataFileRespond() { Data = ByteString.CopyFrom(fileDataManager.Get(computer, file)) });
        }

        public override Task<RenameDirectoryRespond> RenameDirectory(RenameDirectoryRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            if (Directory.IsNameValid(request.Name) == false)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid directory name"));

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid directory = request.Directory.ToGuid();
            var dir = fileSystemManager.GetDirectoryById(computer, directory);
            if (dir == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Directory not exists"));
            if (fileSystemManager.GetDirectoryByName(computer, dir.Parent, request.Name) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Directory already exists"));
            fileSystemManager.RenameDirectory(computer, directory, request.Name);
            transaction.Commit();
            return Task<RenameDirectoryRespond>.FromResult(new RenameDirectoryRespond());
        }

        public override Task<RenameFileRespond> RenameFile(RenameFileRequest request, ServerCallContext context)
        {
            authHelper.EnsurePermissionForComputer(context, request.Computer.ToGuid());

            if (File.IsNameValid(request.Name) == false)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file name"));

            using var transaction = transCtrl.BeginTrans();
            Guid computer = request.Computer.ToGuid();
            Guid file = request.File.ToGuid();
            var f = fileSystemManager.GetFileById(computer, file);
            if (f == null)
                throw new RpcException(new Status(StatusCode.NotFound, "File not exists"));
            if (fileSystemManager.GetFileByName(computer, f.Parent, request.Name) != null)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "File already exists"));
            fileSystemManager.RenameFile(computer, file, request.Name);
            transaction.Commit();
            return Task<RenameFileRespond>.FromResult(new RenameFileRespond());
        }
    }
}
