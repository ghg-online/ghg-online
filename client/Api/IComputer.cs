namespace client.Api
{
    public interface IComputer
    {
        string Name { get; }
        Guid OwnerAccountId { get; }
        Guid RootDirectoryId { get; }
        IDirectory RootDirectory { get; }

        string FromIdToPath(Guid id);
        Task<string> FromIdToPathAsync(Guid id);
        bool IsDirectory(Guid id);
        Task<bool> IsDirectoryAsync(Guid id);
    }
}