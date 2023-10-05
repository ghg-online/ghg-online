namespace server.Entities
{
    public class Computer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } // should not be empty
        public bool IsDeleted { get; set; }

        public Guid Owner { get; set; }
        public Guid RootDirectory { get; set; }

        public Computer()
        {
            Name = string.Empty;
        }

        public Computer(Guid owner, string name, Guid rootDirectory)
        {
            Owner = owner;
            Name = name;
            RootDirectory = rootDirectory;
        }

        public static bool IsNameValid(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return true;
        }
    }
}
