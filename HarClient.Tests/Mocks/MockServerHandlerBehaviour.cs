using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HarClient.Tests.Mocks
{
    /// <summary>
    /// Meta-tests to assert the behaviour of the mock object.
    /// </summary>
    [TestClass]
    public class MockServerHandlerBehaviour
    {
        static MockServerHandler Create()
        {
            return new MockServerHandler
            {
                Responses =
                {
                    { "http://mockserverhandler/created", HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.Created) },
                    {
                        "http://mockserverhandler/test",
                        HttpMethod.Get,
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("Test"),
                        }
                    },
                    {
                        "http://mockserverhandler/redirect",
                        HttpMethod.Get,
                        new HttpResponseMessage(HttpStatusCode.Redirect)
                        {
                            Headers =
                            {
                                Location = new Uri("http://mockserverhandler/test")
                            }
                        }
                    },
                    {
                        "http://mockserverhandler/broken", HttpMethod.Get, new HttpRequestException("Out of peanuts")
                    }
                }
            };
        }

        [TestMethod]
        public async Task GetsContent()
        {
            var client = new HttpClient(Create());
            var response = await client.GetAsync("http://mockserverhandler/test");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Test", await response.Content.ReadAsStringAsync());
        }
        [TestMethod]
        public async Task Redirects()
        {
            var client = new HttpClient(Create());
            var response = await client.GetAsync("http://mockserverhandler/redirect");
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual(new Uri("http://mockserverhandler/test"), response.Headers.Location);
        }
        [TestMethod, ExpectedException(typeof(TestException))]
        public async Task ThrowsIfUnexpectedRequest()
        {
            var client = new HttpClient(Create());
            await client.GetAsync("http://garbage.url");
        }
        [TestMethod, ExpectedException(typeof(HttpRequestException))]
        public async Task ReturnsExceptionResponseAsDefined()
        {
            var client = new HttpClient(Create());
            _ = await client.GetAsync("http://mockserverhandler/broken");
        }
    }
}
