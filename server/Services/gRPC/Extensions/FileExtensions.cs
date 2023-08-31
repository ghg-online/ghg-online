namespace server.Services.gRPC.Extensions
{
    using File = server.Entities.File;
    using FileInfo = server.Protos.FileInfo;

    public static class FileExtensions
    {
        public static FileInfo ToFileInfo(this File file)
        {
            var fileInfo = new FileInfo()
            {
                Id = file.Id.ToByteString(),
                Name = file.Name,
                Parent = file.Parent.ToByteString(),
                Type = file.Type.ToString(),
                // Flag enum can be converted to string
                // see: https://learn.microsoft.com/zh-cn/dotnet/api/system.enum.tostring?view=net-7.0
                // also see: https://learn.microsoft.com/zh-cn/dotnet/standard/base-types/enumeration-format-strings
            };
            return fileInfo;
        }
    }
}
