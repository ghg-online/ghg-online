namespace client.Api
{
    public interface IFile
    {
        Guid ComputerId { get; }
        Guid FileId { get; }
        string Name { get; }
        Guid ParentId { get; }
        File.TypeCode Type { get; }
        IDirectory Parent { get; }

        void Delete();
        Task DeleteAsync();
        void Modify(byte[] data);
        Task ModifyAsync(byte[] data);
        byte[] Read();
        Task<byte[]> ReadAsync();
        void Rename(string name);
        Task RenameAsync(string name);
        Task Syncronize();
    }
}