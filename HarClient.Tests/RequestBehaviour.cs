using JBlam.HarClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class RequestBehaviour
    {
        static HttpResponseMessage RedirectTo(string relativeUri) => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers =
            {
                Location = new Uri(relativeUri, UriKind.Relative)
            }
        };

        [TestMethod]
        public async Task DoesRedirect()
        {
            var (sut, client) = MockClient.Create(new MockServerHandler()
            {
                Responses = 
                {
                    { "/redirect", HttpMethod.Get, RedirectTo("/destination") },
                    { "/destination", HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.OK){ Content = new StringContent("Hi") } }
                }
            });
            var response = await client.GetAsync("/redirect");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Hi", await response.Content.ReadAsStringAsync());
            var har = await sut.CreateHarAsync();
            Assert.AreEqual(2, har.Log.Entries.Count);
        }

        [TestMethod]
        public async Task TerminatesInfiniteRedirectSet()
        {
            // as a "nothing up my sleeve number" protection against off-by-one errors,
            // let's start from a prime number
            const int initialRedirectIdentifier = 17;
            var responses = Enumerable.Range(0, 100).Select(i =>
                   KeyValuePair.Create(
                       (MockClient.AsRelativeUri($"/redirect/infinite/{i}"), HttpMethod.Get),
                       Task.FromResult(RedirectTo($"/redirect/infinite/{i + 1}"))));
            var handler = new MockServerHandler();
            handler.Responses.AddRange(responses);
            var (sut, client) = MockClient.Create(handler);
            var finalResponse = await client.GetAsync("/redirect/infinite/" + initialRedirectIdentifier);
            Assert.AreEqual(HttpStatusCode.Found, finalResponse.StatusCode);
            Assert.AreEqual(
                $"/redirect/infinite/{initialRedirectIdentifier + HarMessageHandler.MaximumRedirectCount + 1}",
                finalResponse.Headers.Location.ToString());
        }

        [TestMethod]
        public async Task TerminatesCircularRedirectSet()
        {
            var (sut, client) = MockClient.Create(new MockServerHandler()
            {
                Responses =
                {
                    { "/redirect/circular/0", HttpMethod.Get, RedirectTo("/redirect/circular/1") },
                    { "/redirect/circular/1", HttpMethod.Get, RedirectTo("/redirect/circular/0") },
                }
            });
            var finalResponse = await client.GetAsync("/redirect/circular/0");
            Assert.AreEqual(HttpStatusCode.Found, finalResponse.StatusCode);
            Assert.AreEqual(2, (await sut.CreateHarAsync()).Log.Entries.Count);
            Assert.AreEqual(
                $"/redirect/circular/0",
                finalResponse.Headers.Location.ToString());
        }
    }
}
