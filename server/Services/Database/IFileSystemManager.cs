namespace server.Services.Database
{
    using Directory = server.Entities.Directory;
    using File = server.Entities.File;

    public interface IFileSystemManager
    {
        public Guid CreateDirectory(Guid computer, Guid parent, string newDirectoryName);
        public Guid CreateFile(Guid computer, Guid parent, string newFileName, File.TypeCode type);
        public void DeleteDirectory(Guid computer, Guid directory);
        public void DeleteFile(Guid computer, Guid file);
        public void RenameDirectory(Guid computer, Guid directory, string newName);
        public void RenameFile(Guid computer, Guid file, string newName);

        public IEnumerable<Directory> ListDirectories(Guid computer, Guid parent);
        public IEnumerable<File> ListFiles(Guid computer, Guid parent);
        public Directory? GetDirectoryById(Guid computer, Guid directory);
        public Directory? GetDirectoryByName(Guid computer, Guid parent, string name);
        public File? GetFileById(Guid computer, Guid file);
        public File? GetFileByName(Guid computer, Guid parent, string name);
        public bool ExistsDirectory(Guid computer, Guid directory);
        public bool ExistsDirectory(Guid computer, Guid parent, string name);
        public bool ExistsFile(Guid computer, Guid file);
        public bool ExistsFile(Guid computer, Guid parent, string name);
        public bool IsDirectoryEmpty(Guid computer, Guid directory);
    }
}
