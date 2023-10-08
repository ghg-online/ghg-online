using Google.Protobuf;
using Grpc.Core;
/*  
 *  Namespace   :   client.Api
 *  Filename    :   IGrpcMiddleware.cs
 *  Class       :   IGrpcMiddleware
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/10/08
 *  Last Modify :   2023/10/08
 *  
 */

namespace client.Api.GrpcMiddleware
{
    /// <summary>
    /// This interface provides an abstraction of gRPC middleware, which
    /// is used to launch a gRPC invoke with some sort of some extra service, for example,
    /// showing a waiting dialog, adding a timeout, update info on status bar,
    /// providing logging service, etc.
    /// </summary>
    /// <remarks>
    /// However, you should not change the response message.
    /// </remarks>
    public interface IGrpcMiddleware
    {
        /// <summary>
        /// Launch a gRPC invoke with some sort of middleware, for example, 
        /// showing a waiting dialog, adding a timeout, update info on status bar,
        /// providing logging service, etc.
        /// </summary>
        /// <remarks>
        /// However, you should not change the response message.
        /// </remarks>
        /// 
        /// <typeparam name="TRequest">The type of the request message.</typeparam>
        /// <typeparam name="TResponse">The type of the respond message.</typeparam>
        /// <param name="func">
        /// An async gRPC function to invoke, for example:
        /// <see cref="server.Protos.FileSystem.FileSystemClient.CreateDataFileAsync(server.Protos.CreateDataFileRequest, Metadata, DateTime?, CancellationToken)"/>
        /// </param>
        /// <param name="request">
        /// A request message to send to the server, for example:
        /// <see cref="server.Protos.CreateDirectoryRequest"/>
        /// </param>
        /// <returns>
        /// The respond message from the server, for example:
        /// <see cref="server.Protos.CreateDirectoryRespond"/>
        /// </returns>
        public abstract TResponse Invoke<TRequest, TResponse>
            (Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func,
            TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new();

        /// <summary>
        /// Launch a gRPC invoke with some sort of middleware, for example, 
        /// showing a waiting dialog, adding a timeout, update info on status bar,
        /// providing logging service, etc.
        /// </summary>
        /// <remarks>
        /// However, you should not change the response message.
        /// </remarks>
        /// 
        /// <typeparam name="TRequest">The type of the request message.</typeparam>
        /// <typeparam name="TResponse">The type of the respond message.</typeparam>
        /// <param name="func">
        /// An async gRPC function to invoke, for example:
        /// <see cref="server.Protos.FileSystem.FileSystemClient.CreateDataFileAsync(server.Protos.CreateDataFileRequest, Metadata, DateTime?, CancellationToken)"/>
        /// </param>
        /// <param name="request">
        /// A request message to send to the server, for example:
        /// <see cref="server.Protos.CreateDirectoryRequest"/>
        /// </param>
        /// <returns>
        /// A task that contains the respond message from the server, for example:
        /// <see cref="server.Protos.CreateDirectoryRespond"/>
        /// </returns>
        public abstract Task<TResponse> InvokeAsync<TRequest, TResponse>
            (Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func,
            TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new();
    }
}