using Google.Protobuf.Collections;

namespace client.Api
{
    public class ResourcePool
    {
        private readonly Dictionary<Guid, Object> pool;

        private ResourcePool()
        {
            pool = new Dictionary<Guid, Object>();
        }

        /// <summary>
        /// Gets the singleton instance of the ResourcePool.
        /// </summary>
        public static ResourcePool Instance { get; } = new ResourcePool();

        /// <summary>
        /// Registers an object to the ResourcePool.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to register.
        /// </typeparam>
        /// <param name="guid">
        /// The guid of the object to register.
        /// </param>
        /// <param name="obj">
        /// The object to register.
        /// </param>
        public void Register<T>(Guid guid, T obj) where T : class
        {
            pool.Add(guid, obj);
        }

        /// <summary>
        /// Gets an object from the ResourcePool.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to get.
        /// </typeparam>
        /// <param name="guid">
        /// The guid of the object to get.
        /// </param>
        /// <returns>
        /// The object with the given guid. Null if no object with the given guid
        /// </returns>
        public T? Get<T>(Guid guid) where T : class
        {
            return pool[guid] as T;
        }
    }
}
