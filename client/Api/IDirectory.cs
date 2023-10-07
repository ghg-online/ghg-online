namespace client.Api
{
    public interface IDirectory : ISyncable
    {
        List<IDirectory> ChildDirectories { get; }
        List<IFile> ChildFiles { get; }
        Guid ComputerId { get; }
        Guid DirectoryId { get; }
        string? Name { get; } // null if root
        Guid ParentId { get; }
        IDirectory? Parent { get; }
        IComputer? Computer { get; }

        IFile CreateDataFile(string name, byte[] data);
        Task<IFile> CreateDataFileAsync(string name, byte[] data);
        IDirectory CreateDirectory(string name);
        Task<IDirectory> CreateDirectoryAsync(string name);
        void Delete(bool recursive = false);
        Task DeleteAsync(bool recursive = false);
        void Rename(string name);
        Task RenameAsync(string name);
        IDirectory SeekDirectory(string path);
        Task<IDirectory> SeekDirectoryAsync(string path);
        IFile SeekFile(string path);
        Task<IFile> SeekFileAsync(string path);
        Task SyncBasicInfo();
        Task SyncChildDirectories();
        Task SyncChildFiles();
    }
}