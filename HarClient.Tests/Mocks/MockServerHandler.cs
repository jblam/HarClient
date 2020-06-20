using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Mocks
{
    using RequestDictionary = IDictionary<ValueTuple<Uri, HttpMethod>, Task<HttpResponseMessage>>;

    class MockServerHandler : HttpMessageHandler
    {
        // Design principle: this class should have the exact same observable behaviour
        // as SocketsHttpHandler and HttpClientHandler. Any behaviour outside what they
        // do is out-of-scope for now.

        // SPEC:
        // - Requestable resources are specified by URL and method
        //   > More than one resource is specifiable, such that tests can perform a sequence of
        //   > requests using the same client/handler.
        // - The response behaviour is specified in advance as a Task<HttpResponseMessage>, or an
        //   object convertable to Task<HttpResponseMessage>
        //   > HttpResponseMessage is specified directly so that there is no intermediate
        //   > implementation which also requires testing.
        //   > By returning HttpResponseMessage, the test suite is exactly as expressive as the
        //   > System.Net.Http library itself.
        // - An exception is thrown upon any request for a URL or method which was not explicitly specified
        //   > An unexpected request URL indicates that the test itself is broken.

        public static Uri BaseUri { get; } = new Uri("http://mockserverhandler");

        public HttpClient CreateClient() => new HttpClient(this)
        {
            BaseAddress = BaseUri
        };

        public RequestDictionary Responses { get; } =
            new Dictionary<(Uri, HttpMethod), Task<HttpResponseMessage>>();


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri;
            var method = request.Method;
            if (Responses.TryGetValue((path, method), out var response))
                return response;
            throw new TestException($"No response defined for [{method} {path}]");
        }
    }
}
