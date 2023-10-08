using server.Services.gRPC.Extensions;
using ComputerInfoMessage = server.Protos.ComputerInfo;
using FileInfoMessage = server.Protos.FileInfo;
using FileTypeCode = client.Api.File.TypeCode;
using DirInfoMessage = server.Protos.DirectoryInfo;
using client.Api.Entity;

namespace client.Utils
{
    public static class ToEntityExtensions
    {
        public static ComputerInfoEntity ToEntity(this ComputerInfoMessage message)
        {
            return new ComputerInfoEntity
            {
                ComputerId = message.Id.ToGuid(),
                Name = message.Name,
                OwnerAccountId = message.Owner.ToGuid(),
                RootDirectoryId = message.RootDirectory.ToGuid(),
            };
        }

        public static FileInfoEntity ToEntity(this FileInfoMessage message)
        {
            return new FileInfoEntity
            {
                FileId = message.Id.ToGuid(),
                Name = message.Name,
                ParentId = message.Parent.ToGuid(),
                TypeCode = (FileTypeCode)Enum.Parse(typeof(FileTypeCode), message.Type),
            };
        }

        public static DirectoryInfoEntity ToEntity(this DirInfoMessage message)
        {
            return new DirectoryInfoEntity
            {
                DirectoryId = message.Id.ToGuid(),
                Name = message.Name,
                ParentId = message.Parent.ToGuid(),
            };
        }
    }
}
