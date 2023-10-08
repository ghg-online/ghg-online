using client.Api.Abstraction;
using client.Api.Entity;
using Grpc.Net.Client;
using server.Protos;
using static server.Protos.Account;
using static server.Protos.Computer;
using static server.Protos.FileSystem;

namespace client.Api
{
    public class ResourcePool
    {
        private readonly Dictionary<Guid, Object> pool;

        public AccountClient AccountClient { get; }
        public ComputerClient ComputerClient { get; }
        public FileSystemClient FileSystemClient { get; }
        public GhgApi GhgApi { get; private set; }

        public ResourcePool(GrpcChannel grpcChannel)
        {
            pool = new Dictionary<Guid, Object>();
            AccountClient = new AccountClient(grpcChannel);
            ComputerClient = new ComputerClient(grpcChannel);
            FileSystemClient = new FileSystemClient(grpcChannel);
            GhgApi = null!;
        }

        public void LoadGhgApi(GhgApi ghgApi)
        {
            if (GhgApi != null)
                throw new InvalidOperationException("GhgApi is already loaded");
            GhgApi = ghgApi;
        }

        public void Register(Guid guid, Object obj)
        {
            if (pool.ContainsKey(guid))
                throw new DuplicateRegistryException(obj.GetType(), guid);
            pool[guid] = obj;
        }

        /// <summary>
        /// Gets an object from the ResourcePool.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to get.
        /// </typeparam>
        /// <param name="guid">
        /// The fileId of the object to get.
        /// </param>
        /// <returns>
        /// The object with the given fileId. Null if no object with the given fileId
        /// </returns>
        public T? GetObjectFromLocalRegistry<T>(Guid guid) where T : class
        {
            if (pool.ContainsKey(guid))
                return pool[guid] as T;
            else
                return null;
        }

        public void ThrowIfAlreadyExists(Guid guid)
        {
            if (pool.ContainsKey(guid))
                throw new DuplicateConstructorInvokeException(pool[guid].GetType(), guid);
        }

        public IFile GetFile(Guid computerId, Guid fileId)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            if (fileId == Guid.Empty)
                throw new ArgumentException("fileId cannot be empty", nameof(fileId));
            var obj = GetObjectFromLocalRegistry<IFile>(fileId);
            if (obj != null)
                return obj;
            obj = new File(this, FileSystemClient, computerId, fileId);
            Register(fileId, obj);
            return obj;
        }

        public IFile GetFileWithUpdate(Guid computerId, Guid fileId, FileInfoEntity info)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            if (fileId == Guid.Empty)
                throw new ArgumentException("fileId cannot be empty", nameof(fileId));
            var obj = GetObjectFromLocalRegistry<IFile>(fileId);
            if (obj != null)
            {
                obj.UpdateCache(info);
                return obj;
            }
            obj = new File(this, FileSystemClient, info, computerId);
            Register(fileId, obj);
            return obj;
        }

        public IDirectory GetDirectory(Guid computerId, Guid directoryId)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            if (directoryId == Guid.Empty)
                throw new ArgumentException("directoryId cannot be empty", nameof(directoryId));
            var obj = GetObjectFromLocalRegistry<IDirectory>(directoryId);
            if (obj != null)
                return obj;
            obj = new Directory(this, FileSystemClient, computerId, directoryId);
            Register(directoryId, obj);
            return obj;
        }

        public IDirectory GetDirectoryWithUpdate(Guid computerId, Guid directoryId, DirectoryInfoEntity info)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            if (directoryId == Guid.Empty)
                throw new ArgumentException("directoryId cannot be empty", nameof(directoryId));
            var obj = GetObjectFromLocalRegistry<IDirectory>(directoryId);
            if (obj != null)
            {
                obj.UpdateCache(info);
                return obj;
            }
            obj = new Directory(this, FileSystemClient, info, computerId);
            Register(directoryId, obj);
            return obj;
        }

        public IComputer GetComputer(Guid computerId)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            var obj = GetObjectFromLocalRegistry<IComputer>(computerId);
            if (obj != null)
                return obj;
            obj = new Computer(this, ComputerClient, FileSystemClient, computerId);
            Register(computerId, obj);
            return obj;
        }

        public IComputer GetComputerWithUpdate(Guid computerId, ComputerInfoEntity info)
        {
            if (computerId == Guid.Empty)
                throw new ArgumentException("computerId cannot be empty", nameof(computerId));
            var obj = GetObjectFromLocalRegistry<IComputer>(computerId);
            if (obj != null)
            {
                obj.UpdateCache(info);
                return obj;
            }
            obj = new Computer(this, ComputerClient, FileSystemClient, info);
            Register(computerId, obj);
            return obj;
        }
    }
}
