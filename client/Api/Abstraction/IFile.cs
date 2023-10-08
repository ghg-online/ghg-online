using client.Api.Entity;

namespace client.Api.Abstraction
{
    public interface IFile : ISyncable
    {
        Guid ComputerId { get; }
        Guid FileId { get; }
        string Name { get; }
        Guid ParentId { get; }
        File.TypeCode Type { get; }
        IDirectory Parent { get; }

        void Modify(byte[] data);
        Task ModifyAsync(byte[] data);
        byte[] Read();
        Task<byte[]> ReadAsync();
        void Rename(string name);
        Task RenameAsync(string name);
        Task SyncBasicInfo();
        void UpdateCache(FileInfoEntity info);
    }
}