using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    class HttpClientHandlerForceVersionPolicy : System.Net.Http.HttpClientHandler
    {
        private readonly HttpVersionPolicy _policy;

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
            });
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = new System.Version(1, 1);
            var reply = base.Send(request, cancellationToken);
            reply.Version = new System.Version(2, 0);
            return reply;
        }
    }

    class HttpClientHandlerDisableSslCertificateValidation : System.Net.Http.HttpClientHandler
    {
        public HttpClientHandlerDisableSslCertificateValidation()
        {
            base.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
    }
}
