using LiteDB;
using server.Entities;

namespace server.Services.Database
{
    using Directory = Entities.Directory;
    using File = Entities.File;

    public interface IDbHolder
    {
        ILiteCollection<Account> Accounts { get; }
        ILiteCollection<AccountLog> AccountLogs { get; }
        ILiteCollection<ActivationCode> ActivationCodes { get; }
        ILiteCollection<Computer> Computers { get; }
        ILiteCollection<Directory> Directories { get; }
        ILiteCollection<File> Files { get; }
        ILiteCollection<FileData> FileData { get; }

        bool BeginTrans();
        bool Commit();
        bool Rollback();
    }
}
