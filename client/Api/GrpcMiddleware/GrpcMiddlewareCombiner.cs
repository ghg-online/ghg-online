using Google.Protobuf;
using Grpc.Core;

namespace client.Api.GrpcMiddleware
{
    public class GrpcMiddlewareCombiner : List<IGrpcMiddleware>, IGrpcMiddleware
    {
        /// <inheritdoc/>
        /// <summary>
        /// This method invokes the first middleware in the list
        /// , which invokes the second middleware
        /// , which invokes the third middleware, etc.
        /// </summary>
        public TResponse Invoke<TRequest, TResponse>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func, TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new()
        {
            var copy = new List<IGrpcMiddleware>(this);
            if (copy.Count == 0)
                return func(request, null, null, CancellationToken.None).ResponseAsync.Result;
            copy.Reverse();
            var function = func;
            for (var i = 0; i < copy.Count - 1; i++)
            {
                var middleware = copy[i];
                function = new MiddlewareWrapper(middleware).WrapFunction(function);
            }
            var firstMiddleware = copy[^1];
            return firstMiddleware.Invoke(func, request);
        }

        public Task<TResponse> InvokeAsync<TRequest, TResponse>(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func, TRequest request)
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new()
        {
            var copy = new List<IGrpcMiddleware>(this);
            if (copy.Count == 0)
                return func(request, null, null, CancellationToken.None).ResponseAsync;
            copy.Reverse();
            var function = func;
            for (var i = 0; i < copy.Count - 1; i++)
            {
                var middleware = copy[i];
                function = new MiddlewareWrapper(middleware).WrapFunction(function);
            }
            var firstMiddleware = copy[^1];
            return firstMiddleware.InvokeAsync(func, request);
        }

        private class MiddlewareWrapper
        {
            private readonly IGrpcMiddleware _middleware;

            public MiddlewareWrapper(IGrpcMiddleware middleware)
            {
                _middleware = middleware;
            }

            public Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>>
                WrapFunction<TRequest, TResponse>(
                    Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func)
                where TRequest : IBufferMessage
                where TResponse : IBufferMessage, new()
            {
                var invoker = new SingleInvoke<TRequest, TResponse>(func);
                var wrapper = new Wrapper<TRequest, TResponse>(_middleware, func);
                return wrapper.Function;
            }

            private class Wrapper<TRequest, TResponse>
                where TRequest : IBufferMessage
                where TResponse : IBufferMessage, new()
            {
                private readonly IGrpcMiddleware _middleware;
                private readonly SingleInvoke<TRequest, TResponse> _invoker;

                public Wrapper(IGrpcMiddleware middleware, Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func)
                {
                    _middleware = middleware;
                    _invoker = new SingleInvoke<TRequest, TResponse>(func);
                }
                public AsyncUnaryCall<TResponse> Function(
                    TRequest request,
                    Metadata? metadata,
                    DateTime? dateTime,
                    CancellationToken cancellationToken)
                {
                    _middleware.InvokeAsync(_invoker.Function, request);
                    return _invoker.Function(request, metadata, dateTime, cancellationToken);
                }
            }

            private class SingleInvoke<TRequest, TResponse>
            where TRequest : IBufferMessage
            where TResponse : IBufferMessage, new()
            {
                private readonly
                    Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> _func;
                private bool _called = false;
                private AsyncUnaryCall<TResponse> _respond = null!;

                public SingleInvoke(Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>> func)
                {
                    _func = func;
                }

                public AsyncUnaryCall<TResponse> Function(
                    TRequest request,
                    Metadata? metadata,
                    DateTime? dateTime,
                    CancellationToken cancellationToken)
                {
                    lock (this)
                    {
                        if (false == _called)
                        {
                            _respond = _func(request, metadata, dateTime, cancellationToken);
                            _called = true;
                        }
                        return _respond;
                    }
                }
            }
        }
    }
}
