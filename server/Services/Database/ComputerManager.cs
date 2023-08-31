using LiteDB;
using server.Entities;

namespace server.Services.Database
{
    public class ComputerManager : IComputerManager
    {
        readonly ILiteCollection<Computer> computers;

        public ComputerManager(IDbHolder dbHolder)
        {
            computers = dbHolder.Computers;
        }

        public Guid CreateComputer(string name, Guid owner, Guid rootDirectory)
        {
            var newComputer = new Computer()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Owner = owner,
                RootDirectory = rootDirectory,
                IsDeleted = false,
            };
            computers.Insert(newComputer);
            return newComputer.Id;
        }

        public void DeleteComputer(Guid id)
        {
            var computer = computers.FindById(id);
            computer.IsDeleted = true;
            computers.Update(computer);
        }

        public bool ExistComputer(Guid id)
        {
            return computers.Exists(x => x.Id == id && x.IsDeleted == false);
        }

        public void ModifyRootDirectory(Guid id, Guid rootDirectory)
        {
            var computer = computers.FindOne(x => x.Id == id && x.IsDeleted == false);
            computer.RootDirectory = rootDirectory;
            computers.Update(computer);
        }

        public Computer QueryComputerById(Guid id)
        {
            return computers.FindOne(x => x.Id == id && x.IsDeleted == false);
        }

        public IEnumerable<Computer> QueryComputerByOwner(Guid owner)
        {
            return computers.Find(x => x.Owner == owner && x.IsDeleted == false);
        }

        public void RenameComputer(Guid id, string newName)
        {
            var computer = computers.FindOne(x => x.Id == id && x.IsDeleted == false);
            computer.Name = newName;
            computers.Update(computer);
        }
    }
}
