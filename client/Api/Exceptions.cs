namespace client.Api
{
    public abstract class GhgException : Exception
    {
        public GhgException() : base() { }
        public GhgException(string message) : base(message) { }
    }

    public class DuplicateConstructorInvokeException : GhgException
    {
        public Type Type { get; }
        public Guid Guid { get; }
        public DuplicateConstructorInvokeException(Type type, Guid guid)
            : base("Do not call constructor directly. Use ResourcePool instead.")
        {
            Type = type;
            Guid = guid;
        }
    }

    public class DuplicateRegistryException : GhgException
    {
        public Type Type { get; }
        public Guid Guid { get; }
        public DuplicateRegistryException(Type type, Guid guid)
            : base("Do not call ResourcePool.Register directly. Use ResourcePool.GetXxx() instead.")
        {
            Type = type;
            Guid = guid;
        }
    }

    public class IllegalUpdateException : GhgException
    {
        public Type Type { get; }
        public Guid GuidOfObject { get; }
        public Guid GuidInUpdateInfo { get; }
        public IllegalUpdateException(Type type, Guid guidOfObject, Guid guidInUpdateInfo)
            :base("Guid of object not matching guid in update info when trying to update an object's data.")
        {
            Type = type;
            GuidOfObject = guidOfObject;
            GuidInUpdateInfo = guidInUpdateInfo;
        }
    }

    public class NotFoundException : GhgException { }
    public class AlreadyExistsException : GhgException { }
    public class DirectoryNotEmptyException : GhgException { }
    public class InvalidNameException : GhgException { }
    public class DamagedFileSystemStructureException : GhgException
    {
        public DamagedFileSystemStructureException(string message) : base(message) { }
    }
}
