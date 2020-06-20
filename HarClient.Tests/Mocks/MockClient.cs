using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Mocks
{
    using RequestDictionary = IDictionary<ValueTuple<Uri, HttpMethod>, Task<HttpResponseMessage>>;

    static class MockClient
    {
        public static (HarMessageHandler sut, HttpClient client) Create(MockServerHandler innerHandler)
        {
            var sut = new HarMessageHandler(innerHandler);
            var client = new HttpClient(sut) { BaseAddress = MockServerHandler.BaseUri };
            return (sut, client);
        }
        public static (HarMessageHandler sut, HttpClient client) Create(string path, HttpMethod method, HttpResponseMessage response) =>
            Create(new MockServerHandler
            {
                Responses =
                {
                    { path, method, response }
                }
            });
        public static (HarMessageHandler sut, HttpClient client) Create(string path, HttpMethod method, HttpRequestException exception) =>
            Create(new MockServerHandler
            {
                Responses =
                {
                    { path, method, exception }
                }
            });
        public static (HarMessageHandler sut, HttpClient client) Create(string path, HttpMethod method, Task<HttpResponseMessage> future) =>
            Create(new MockServerHandler
            {
                Responses =
                { 
                    { path, method, future }
                }
            });

        static Uri RelativeUri(this string s) => new Uri(MockServerHandler.BaseUri, s);
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, HttpRequestException exception) =>
            requests.Add((path.RelativeUri(), method), Task.FromException<HttpResponseMessage>(exception));
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, HttpResponseMessage message) =>
            requests.Add((path.RelativeUri(), method), Task.FromResult(message));
        public static void Add(this RequestDictionary requests, string path, HttpMethod method, Task<HttpResponseMessage> task) =>
            requests.Add((path.RelativeUri(), method), task);
    }
}
