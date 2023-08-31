namespace server.Entities
{
    public class Computer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
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
    }
}
