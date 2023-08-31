namespace server.Services.Database
{
    public interface IFileDataManager
    {
        public void Create(Guid computer, Guid fileId, byte[] data);
        public void Delete(Guid computer, Guid fileId);
        public void Modify(Guid computer, Guid fileId, byte[] data);
        public byte[] Get(Guid computer, Guid fileId);
    }
}
