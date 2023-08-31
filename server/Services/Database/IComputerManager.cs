using server.Entities;

namespace server.Services.Database
{
    public interface IComputerManager
    {
        Guid CreateComputer(string name, Guid owner, Guid rootDirectory);
        void DeleteComputer(Guid id);
        void RenameComputer(Guid id, string newName);
        void ModifyRootDirectory(Guid id, Guid rootDirectory);
        bool ExistComputer(Guid id);
        Computer QueryComputerById(Guid id);
        IEnumerable<Computer> QueryComputerByOwner(Guid owner);
    }
}
