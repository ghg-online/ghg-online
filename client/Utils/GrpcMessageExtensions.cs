using server.Services.gRPC.Extensions;
using static server.Protos.FileSystem;
using File = client.Api.File;
using FileInfo = server.Protos.FileInfo;
using Directory = client.Api.Directory;
using DirectoryInfo = server.Protos.DirectoryInfo;
using server.Protos;
using Computer = client.Api.Computer;
using static server.Protos.Computer;

namespace client.Utils
{
    public static class GrpcMessageExtensions
    {
        public static File ToFile(this FileInfo fileInfo, FileSystemClient client, Guid computer)
        {
            return new File(
                client,
                computer,
                fileInfo.Id.ToGuid(),
                fileInfo.Parent.ToGuid(),
                fileInfo.Name,
                (File.TypeCode)Enum.Parse(typeof(File.TypeCode), fileInfo.Type)
            );
        }

        public static Directory ToDirectory(this DirectoryInfo directoryInfo, FileSystemClient client, Guid computer)
        {
            return new Directory(
                client,
                computer,
                directoryInfo.Id.ToGuid(),
                directoryInfo.Parent.ToGuid(),
                directoryInfo.Name == string.Empty ? null : directoryInfo.Name
            );
        }

        public static Computer ToComputer(
            this ComputerInfo computerInfo,
            ComputerClient computerClient,
            FileSystemClient fileSystemClient)
        {
            return new Computer(
                computerClient,
                computerInfo.Id.ToGuid(),
                computerInfo.Name,
                computerInfo.Owner.ToGuid(),
                computerInfo.RootDirectory.ToGuid(),
                fileSystemClient
            );
        }
    }
}
