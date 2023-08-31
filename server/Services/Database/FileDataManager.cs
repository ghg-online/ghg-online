using LiteDB;
using server.Entities;

namespace server.Services.Database
{
    public class FileDataManager : IFileDataManager
    {
        readonly ILiteCollection<FileData> files;

        public FileDataManager(IDbHolder dbHolder)
        {
            files = dbHolder.FileData;
        }

        public void Create(Guid computer, Guid fileId, byte[] data)
        {
            var fileData = new FileData()
            {
                Id = fileId,
                Computer = computer,
                Bytes = data,
            };
            files.Insert(fileData);
        }

        public void Delete(Guid computer, Guid fileId)
        {
            files.DeleteMany(x => x.Computer == computer && x.Id == fileId);
        }

        public byte[] Get(Guid computer, Guid fileId)
        {
            return files.FindOne(x => x.Computer == computer && x.Id == fileId).Bytes;
        }

        public void Modify(Guid computer, Guid fileId, byte[] data)
        {
            var fileData = files.FindOne(x => x.Computer == computer && x.Id == fileId);
            fileData.Bytes = data;
            files.Update(fileData);
        }
    }
}
