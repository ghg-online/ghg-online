using client.Api.GrpcMiddleware;

namespace client.Api.Abstraction
{
    public interface IGhgApi
    {
        IGrpcMiddleware GrpcMiddleware { get; }
        IList<IGrpcMiddleware> GrpcMiddlewareList { get; }

        string Username { get; }
        IConsole Console { get; }
        IComputer MyComputer { get; }
    }
}