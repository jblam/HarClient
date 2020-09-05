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
        // as a "nothing up my sleeve number" protection against off-by-one errors,
        // start infinite redirects from a prime number.
        public const int InitialRedirectIdentifier = 17;

        public static IEnumerable<(HttpMethod method, int status)> RedirectTestCases
        {
            get
            {

                var allVerbs = new[]
                {
                HttpMethod.Get,
                HttpMethod.Post,
                HttpMethod.Put,
                HttpMethod.Delete,
                HttpMethod.Head,
            };
                var allStatusCodes = Enumerable.Range(300, 100);
                return from verb in allVerbs
                       from status in allStatusCodes
                       select (verb, status);
            }
        }

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
            var responses = Enumerable.Range(0, 100).Select(i =>
                   KeyValuePair.Create(
                       (MockClient.AsRelativeUri($"/redirect/infinite/{i}"), HttpMethod.Get),
                       Task.FromResult(RedirectTo($"/redirect/infinite/{i + 1}"))));
            var handler = new MockServerHandler();
            handler.Responses.AddRange(responses);
            var (sut, client) = MockClient.Create(handler);
            var finalResponse = await client.GetAsync("/redirect/infinite/" + InitialRedirectIdentifier);
            Assert.AreEqual(HttpStatusCode.Found, finalResponse.StatusCode);
            Assert.AreEqual(
                $"/redirect/infinite/{InitialRedirectIdentifier + HarMessageHandler.MaximumRedirectCount + 1}",
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

        [TestMethod]
        public async Task RedirectsExpectedStatuses()
        {
            var contentUri = new Uri("/content", UriKind.Relative);

            foreach (var (method, status) in RedirectTestCases)
            {
                var originalRequest = $"/redirect-echo?status={status}";
                var (sut, client) = MockClient.Create(new MockServerHandler
                {
                    Responses =
                    {
                        { contentUri.OriginalString, MockServerHandler.AnyMethod, new HttpResponseMessage(HttpStatusCode.OK) },
                        { originalRequest, method, new HttpResponseMessage((HttpStatusCode)status) { Headers = { Location = contentUri } } }
                    }
                });
                var response = await client.SendAsync(new HttpRequestMessage(method, originalRequest));
                if (ExpectsRedirect(status))
                {
                    Assert.AreEqual(contentUri.OriginalString, response.RequestMessage.RequestUri.PathAndQuery,
                        "Did not follow redirect for status {0} as expected",
                        status);
                    var expectedFinalRequestMethod = ExpectsMethodChange(method, status)
                        ? HttpMethod.Get
                        : method;
                    Assert.AreEqual(expectedFinalRequestMethod, response.RequestMessage.Method,
                        "Unexpected request method when following {0} redirect", status);
                }
                else
                {
                    Assert.AreEqual(originalRequest, response.RequestMessage.RequestUri.PathAndQuery,
                        "Unexpectedly followed redirect for status {0}", status);
                }
            }

            static bool ExpectsRedirect(int status) => status switch
            {
                300 => true,
                301 => true,
                302 => true,
                303 => true,
                307 => true,
                308 => true,
                _ => false
            };
            static bool ExpectsMethodChange(HttpMethod method, int status) => method == HttpMethod.Post && status >= 300 && status <= 303;
        }
    }
}
