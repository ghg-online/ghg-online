namespace server.Entities
{
    public class File
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        public Guid Computer { get; set; }
        public Guid Parent { get; set; }
        public TypeCode Type { get; set; }

        [System.Flags]
        public enum TypeCode
        {
            Readable = 1,
            Writable = 2,
            Executable = 4,
            Invokable = 8,
        }
        // For data files stored in the server database, TypeCode = Writable | Readable

        public File()
        {
            Name = string.Empty;
        }

        public File(Guid id, string name, bool isDeleted, Guid computer, Guid parent, TypeCode type)
        {
            Id = id;
            Name = name;
            IsDeleted = isDeleted;
            Computer = computer;
            Parent = parent;
            Type = type;
        }
    }
}
