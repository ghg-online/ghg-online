namespace client.Api
{
    public abstract class GhgException : Exception
    {
        public GhgException() : base() { }
        public GhgException(string message) : base(message) { }
    }
    public class NotFoundException : GhgException { }
    public class AlreadyExistsException : GhgException { }
    public class DirectoryNotEmptyException : GhgException { }
    public class InvalidOperationException : GhgException { }
    public class InvalidNameException : GhgException { }
    public class DamagedFileSystemStructureException : GhgException
    {
        public DamagedFileSystemStructureException(string message) : base(message) { }
    }
}
