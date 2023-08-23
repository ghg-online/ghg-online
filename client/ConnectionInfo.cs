using Grpc.Net.Client;

namespace client
{
    public static class ConnectionInfo
    {
        public static string Username { get; private set; } = null!;
        public static string RoleCode { get; private set; } = null!;
        public static GrpcChannel GrpcChannel { get; private set; } = null!;

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
        /// <para>
        /// Currently, there are only 2 roles: "Admin" and "User".
        /// </para>
        /// <para>
        /// You can ensure this from the server code: server/Entities/Account.cs : Account.RoleCode
        /// </para>
        /// </remarks>
        public static void LoadRoleCode(string roleCode)
        {
            RoleCode = roleCode;
        }

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
