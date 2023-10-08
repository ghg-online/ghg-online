using client.Gui;
using Google.Protobuf;
using Grpc.Core;

namespace client.Api.GrpcMiddleware
{
    public class VisualGrpcAdapter : IGrpcMiddleware
    {
        public TResponse Invoke<TRequest, TResponse>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func, TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new()
        {
            return VisualGrpc.Invoke(func, request);
        }

        public Task<TResponse> InvokeAsync<TRequest, TResponse>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func, TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new()
        {
            return VisualGrpc.InvokeAsync(func, request);
        }
    }
}
