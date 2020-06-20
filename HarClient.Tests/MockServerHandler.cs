using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarClient.Tests
{
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

        public Dictionary<(string path, HttpMethod method), Task<HttpResponseMessage>> Responses { get; } =
            new Dictionary<(string path, HttpMethod method), Task<HttpResponseMessage>>();


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri.OriginalString;
            var method = request.Method;
            if (Responses.TryGetValue((path, method), out var response))
                return response;
            throw new InvalidOperationException($"No response defined for ({path}, {method})");
        }

        public static MockServerHandler Create()
        {
            return new MockServerHandler
            {
                Responses =
                {
                    {
                        ("http://mockserverhandler/test", HttpMethod.Get),
                        Task.FromResult(new HttpResponseMessage
                        {
                            Content = new StringContent("Test"),
                            StatusCode = HttpStatusCode.OK,
                        })
                    },
                    {
                        ("http://mockserverhandler/redirect", HttpMethod.Get),
                        Task.FromResult(new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.Redirect,
                            Headers =
                            {
                                Location = new Uri("http://mockserverhandler/test")
                            }
                        })
                    }
                }
            };
        }
    }

    [TestClass]
    public class MockServerHandlerBehaviour
    {
        [TestMethod]
        public async Task GetsContent()
        {
            var client = new HttpClient(MockServerHandler.Create());
            var response = await client.GetAsync("http://mockserverhandler/test");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Test", await response.Content.ReadAsStringAsync());
        }
        [TestMethod]
        public async Task Redirects()
        {
            var client = new HttpClient(MockServerHandler.Create());
            var response = await client.GetAsync("http://mockserverhandler/redirect");
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual(new Uri("http://mockserverhandler/test"), response.Headers.Location);
        }
    }
}
