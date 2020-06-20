using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Mocks
{
    using RequestKey = ValueTuple<Uri, HttpMethod>;
    using RequestDictionary = IDictionary<ValueTuple<Uri, HttpMethod>, Task<HttpResponseMessage>>;

    class MockServerHandler : HttpMessageHandler
    {
        // Design principle: this class should have the exact same observable behaviour
        // as SocketsHttpHandler and HttpClientHandler. Any behaviour outside what they
        // do is out-of-scope for now.
        //
        // Testing use-cases:
        // - GET resource, receive 200OK + some content
        // - POST some content, receive 200OK + some content
        // - POST / redirect / GET
        // - PUT some content, receive 201Created (no content)
        // - GET a resource, receive 3xx redirect; GET redirect target, receieve 200OK + some content
        // - POST some content, receive 405 method not allowed w/ headers
        // - ... something to do with 100 Continue
        // - any kind of request, unreachable
        // - a request which returns some content, but the HAR is observed before the content arrives
        //
        // Requirements:
        // - mapping of URL to response (required so that redirects can resolve themselves without intervention)
        // - repsonse can be an instance of HttpResponseMessage, or Task<HttpResponseMessage> (resolvable through
        //   TaskCompletionSource<T>)

        public static Uri BaseUri { get; } = new Uri("http://mockserverhandler");

        public HttpClient CreateClient() => new HttpClient(this)
        {
            BaseAddress = BaseUri
        };

        public RequestDictionary Responses { get; } =
            new Dictionary<RequestKey, Task<HttpResponseMessage>>();


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri;
            var method = request.Method;
            if (Responses.TryGetValue((path, method), out var response))
                return response;
            throw new TestException($"No response defined for [{method} {path}]");
        }
    }
    static class RequestDictionaryExtensions
    {
        static Uri RelativeUri(this string s) => new Uri(MockServerHandler.BaseUri, s);
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, HttpRequestException exception) =>
            requests.Add((path.RelativeUri(), method), Task.FromException<HttpResponseMessage>(exception));
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, HttpResponseMessage message) =>
            requests.Add((path.RelativeUri(), method), Task.FromResult(message));
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, Task<HttpResponseMessage> task) =>
            requests.Add((path.RelativeUri(), method), task);
    }
}
