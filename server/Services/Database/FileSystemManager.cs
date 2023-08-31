namespace server.Services.Database
{
    using LiteDB;
    using Directory = server.Entities.Directory;
    using File = server.Entities.File;

    public class FileSystemManager : IFileSystemManager
    {
        readonly ILiteCollection<Directory> directories;
        readonly ILiteCollection<File> files;

        public FileSystemManager(IDbHolder dbHolder)
        {
            directories = dbHolder.Directories;
            files = dbHolder.Files;
        }

        public Guid CreateDirectory(Guid computer, Guid parent, string newDirectoryName)
        {
            var dir = new Entities.Directory()
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                Computer = computer,
                Parent = parent,
                Name = newDirectoryName,
            };
            directories.Insert(dir);
            return dir.Id;
        }

        public Guid CreateFile(Guid computer, Guid parent, string newFileName, Entities.File.TypeCode type)
        {
            var file = new File()
            {
                Id = Guid.NewGuid(),
                IsDeleted = false,
                Computer = computer,
                Name = newFileName,
                Parent = parent,
                Type = type,
            };
            files.Insert(file);
            return file.Id;
        }

        public void DeleteDirectory(Guid computer, Guid directoryId)
        {
            var dir = directories.FindOne(x => x.Computer == computer && x.Id == directoryId && x.IsDeleted == false);
            dir.IsDeleted = true;
            directories.Update(dir);
        }

        public void DeleteFile(Guid computer, Guid fileId)
        {
            var file = files.FindOne(x => x.Computer == computer && x.Id == fileId && x.IsDeleted == false);
            file.IsDeleted = true;
            files.Update(file);
        }

        public bool ExistsDirectory(Guid computer, Guid directory)
            => directories.Exists(x => x.Computer == computer && x.Id == directory && x.IsDeleted == false);

        public bool ExistsDirectory(Guid computer, Guid parent, string name)
            => directories.Exists(x => x.Computer == computer && x.Parent == parent && x.Name == name && x.IsDeleted == false);

        public bool ExistsFile(Guid computer, Guid file)
            => files.Exists(x => x.Computer == computer && x.Id == file && x.IsDeleted == false);

        public bool ExistsFile(Guid computer, Guid parent, string name)
            => files.Exists(x => x.Computer == computer && x.Parent == parent && x.Name == name && x.IsDeleted == false);

        public Directory GetDirectoryById(Guid computer, Guid directoryId)
            => directories.FindOne(x => x.Computer == computer && x.Id == directoryId && x.IsDeleted == false);

        public Directory GetDirectoryByName(Guid computer, Guid parent, string name)
            => directories.FindOne(x => x.Computer == computer && x.Parent == parent && x.Name == name && x.IsDeleted == false);

        public File GetFileById(Guid computer, Guid file)
            => files.FindOne(x => x.Computer == computer && x.Id == file && x.IsDeleted == false);

        public File GetFileByName(Guid computer, Guid parent, string name)
            => files.FindOne(x => x.Computer == computer && x.Parent == parent && x.Name == name && x.IsDeleted == false);

        public bool IsDirectoryEmpty(Guid computer, Guid directory)
            => false == directories.Exists(x => x.Computer == computer && x.Parent == directory && x.IsDeleted == false)
                && false == files.Exists(x => x.Computer == computer && x.Parent == directory && x.IsDeleted == false);

        public IEnumerable<Entities.Directory> ListDirectories(Guid computer, Guid parent)
            => directories.Find(x => x.Computer == computer && x.Parent == parent && x.IsDeleted == false);

        public IEnumerable<Entities.File> ListFiles(Guid computer, Guid parent)
            => files.Find(x => x.Computer == computer && x.Parent == parent && x.IsDeleted == false);

        public void RenameDirectory(Guid computer, Guid directoryId, string newName)
        {
            var dir = directories.FindOne(x => x.Computer == computer && x.Id == directoryId && x.IsDeleted == false);
            dir.Name = newName;
            directories.Update(dir);
        }

        public void RenameFile(Guid computer, Guid fileId, string newName)
        {
            var file = files.FindOne(x => x.Computer == computer && x.Id == fileId && x.IsDeleted == false);
            file.Name = newName;
            files.Update(file);
        }
    }
}
