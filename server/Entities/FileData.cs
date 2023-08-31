namespace server.Entities
{
    public class FileData
    {
        public Guid Id { get; set; } // This id is not a new id. Instead, it should be the id of the file.
        public Guid Computer { get; set; }
        public byte[] Bytes { get; set; }

        public FileData()
        {
            Bytes = Array.Empty<byte>();
        }
    }
}
