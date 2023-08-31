namespace server.Services.gRPC.Extensions
{
    using Directory = server.Entities.Directory;
    using DirectoryInfo = server.Protos.DirectoryInfo;

    public static class DirectoryExtensions
    {
        public static DirectoryInfo ToDirectoryInfo(this Directory directory)
        {
            var dirInfo = new DirectoryInfo()
            {
                Id = directory.Id.ToByteString(),
                Name = directory.Name,
                Parent = directory.Parent.ToByteString(),
            };
            return dirInfo;
        }
    }
}
