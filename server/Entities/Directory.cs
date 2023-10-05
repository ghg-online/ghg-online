namespace server.Entities
{
    public class Directory
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        public Guid Computer { get; set; }
        public Guid Parent { get; set; }

        public Directory()
        {
            Name = string.Empty;
        }

        public Directory(Guid id, string name, bool isDeleted, Guid computer, Guid parent)
        {
            Id = id;
            Name = name;
            IsDeleted = isDeleted;
            Computer = computer;
            Parent = parent;
        }

        public static bool IsNameValid(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (name.Contains('/') || name.Contains('\0'))
                return false;
            return true;
        }
    }
}
