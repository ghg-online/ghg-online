using client.Api.Entity;

namespace client.Api.Abstraction
{
    public interface IComputer : ISyncable
    {
        string Name { get; }
        Guid OwnerAccountId { get; }
        Guid RootDirectoryId { get; }
        IDirectory RootDirectory { get; }
        Guid ComputerId { get; }

        string FromIdToPath(Guid id);
        Task<string> FromIdToPathAsync(Guid id);
        bool IsDirectory(Guid id);
        Task<bool> IsDirectoryAsync(Guid id);
        void UpdateCache(ComputerInfoEntity info);
    }
}