namespace client.Api
{
    public interface IComputer : ISyncable
    {
        string Name { get; }
        Guid OwnerAccountId { get; }
        Guid RootDirectoryId { get; }
        IDirectory RootDirectory { get; }

        string FromIdToPath(Guid id);
        Task<string> FromIdToPathAsync(Guid id);
        IDirectory GetDirectoryById(Guid directoryId);
        IFile GetFileById(Guid fileId);
        bool IsDirectory(Guid id);
        Task<bool> IsDirectoryAsync(Guid id);
    }
}