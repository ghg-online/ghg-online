/*  
 *  Namespace   :   client
 *  Filename    :   ConnectionInfo.cs
 *  Class       :   ConnectionInfo
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

using Grpc.Net.Client;

namespace client
{
    /// <summary>
    /// A static class to store connection info.
    /// </summary>
    public static class ConnectionInfo
    {
        /// <summary>
        /// The username of the user that is currently logged in.
        /// </summary>
        /// <remarks>Null if user is not logged in</remarks>
        public static string Username { get; private set; } = null!;

        /// <summary>
        /// The role of the user that is currently logged in.
        /// </summary>
        /// <remarks>Null if user if not logged in</remarks>
        public static string RoleCode { get; private set; } = null!;

        /// <summary>
        /// The connection to the server.
        /// </summary>
        /// <remarks>
        /// <para>Establishing a connection to the server is a time-consuming task.</para>
        /// <para>So we store the connection in this static class.</para>
        /// </remarks>
        public static GrpcChannel GrpcChannel { get; private set; } = null!;

        /// <summary>
        /// Load the username of the user that is currently logged in.
        /// </summary>
        /// <param name="username">Username of the user that is currently logged in</param>
        /// <remarks>This should only be called after a login is done</remarks>
        public static void LoadUsername(string username)
        {
            Username = username;
        }

        /// <summary>
        /// This method is called only by VisualGrpc.LoadToken() method.
        /// You don't need to call this method directly.
        /// </summary>
        /// <param name="roleCode">Role info decoded from jwt token</param>
        /// <remarks>
        /// <para>Currently, there are only 2 roles: "Admin" and "User".</para>
        /// <para>You can ensure this from the server code: server/Entities/Account.cs : Account.RoleCode</para>
        /// </remarks>
        public static void LoadRoleCode(string roleCode)
        {
            RoleCode = roleCode;
        }

        /// <summary>
        /// Load the connection to the server.
        /// </summary>
        /// <param name="grpcChannel">The Grpc channel</param>
        /// <remarks>
        /// <para>Establishing a connection to the server is a time-consuming task.</para>
        /// <para>So we store the connection in this static class.</para>
        /// </remarks>
        public static void LoadGrpcChannel(GrpcChannel grpcChannel)
        {
            GrpcChannel = grpcChannel;
        }

        public static void Clear()
        {
            Username = null!;
            GrpcChannel = null!;
            RoleCode = null!;
        }
    }
}
