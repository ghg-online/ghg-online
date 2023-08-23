/*  
 *  Namespace   :   client
 *  Filename    :   HttpClientHandler.cs
 *  Class       :   HttpClientHandler
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

namespace client
{
    /// <summary>
    /// A class to force HttpClient to use a specific version policy.
    /// </summary>
    /// <remarks>
    /// <para>This is not used in current version.</para>
    /// </remarks>
    class HttpClientHandlerForceVersionPolicy : System.Net.Http.HttpClientHandler
    {
        private readonly HttpVersionPolicy _policy;

        /// <summary>
        /// Create a new instance of HttpClientHandlerForceVersionPolicy.
        /// </summary>
        /// <param name="policy">The policy you specific</param>
        /// <remarks>
        /// <para>Usually this is used as a parameter when constructing a GrpcChannel object</para>
        /// <seealso cref="client.Gui.WelcomeWindow"/>
        /// <seealso cref="client.Gui.ConnectingWindow"/>
        /// </remarks>
        public HttpClientHandlerForceVersionPolicy(HttpVersionPolicy policy)
        {
            _policy = policy;
        }

        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            request.VersionPolicy = _policy;
            return base.SendAsync(request, cancellationToken);
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.VersionPolicy = _policy;
            return base.Send(request, cancellationToken);
        }
    }

    /// <summary>
    /// A class to force HttpClient to use HTTP/1.1.
    /// </summary>
    /// <remarks>
    /// <para>By default, gRPC uses HTTP/2.0, and refuses to use HTTP/1.1.</para>
    /// <para>
    /// However, in some network environment, HTTP/2.0 is not supported.
    /// For example, some proxy servers do not support HTTP/2.0,
    /// and some cloud services do not support HTTP/2.0 as well.
    /// </para>
    /// <para>Thus, we wrote this class that cheat gRPC as if we are using HTTP/2.0, but use HTTP/1.1 instead.</para>
    /// </remarks>
    class HttpClientHandlerForceToUseHttp1_1 : System.Net.Http.HttpClientHandler
    {
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {

            request.Version = new System.Version(1, 1);
            var task = base.SendAsync(request, cancellationToken);
            return task.ContinueWith(reply =>
            {
                reply.Result.Version = new System.Version(2, 0);
                return reply.Result;
            }, cancellationToken);
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = new System.Version(1, 1);
            var reply = base.Send(request, cancellationToken);
            reply.Version = new System.Version(2, 0);
            return reply;
        }
    }

    /// <summary>
    /// Use this class to disable SSL certificate validation.
    /// </summary>
    class HttpClientHandlerDisableSslCertificateValidation : System.Net.Http.HttpClientHandler
    {
        /// <summary>
        /// Create a new instance of HttpClientHandlerDisableSslCertificateValidation.
        /// </summary>
        public HttpClientHandlerDisableSslCertificateValidation()
        {
            base.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
    }
}
