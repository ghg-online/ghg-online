namespace client.Api
{
    public interface IGhgApi
    {
        IConsole Console { get; }
        string Username { get; }
        server.Protos.Account.AccountClient AccountClient { get; }
        server.Protos.Computer.ComputerClient ComputerClient { get; }
        server.Protos.FileSystem.FileSystemClient FileSystemClient { get; }
        Computer MyComputer { get; }
    }
}