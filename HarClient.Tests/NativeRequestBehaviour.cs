using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    using static RequestBehaviour;

    // Ignore justification: documents native .NET platform behaviour; asserts the correctness of
    // the other tests, but does not assert the correctness of the library.
    // Also, inconvenient to require the demo server running.
    [TestClass, Ignore]
    public class NativeRequestBehaviour
    {
        [TestMethod]
        public async Task NativeDoesRedirect()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44398")
            };
            try
            {
                var response = await client.PostAsync($"/api/behaviour/redirect", new StringContent(""));
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

                // POST should redirect to GET
                Assert.AreEqual(HttpMethod.Get, response.RequestMessage.Method);
                // should be null for GET, regardless what the original method was
                Assert.IsNull(response.RequestMessage.Content);
            }
            catch (HttpRequestException hre)
            {
                throw new AssertInconclusiveException("Request failed. Possibly the server isn't running.", hre);
            }
        }

        [TestMethod]
        public async Task CanNativeResolveInfiniteRedirect()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44398")
            };
            try
            {
                var response = await client.GetAsync($"/api/behaviour/redirect/infinite/{InitialRedirectIdentifier}");
                Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
                Assert.AreEqual(
                    $"/api/behaviour/redirect/infinite/{InitialRedirectIdentifier + HarMessageHandler.MaximumRedirectCount + 1}",
                    response.Headers.Location?.ToString());
            }
            catch (HttpRequestException hre)
            {
                throw new AssertInconclusiveException("Request failed. Possibly the server isn't running.", hre);
            }
        }
        [TestMethod]
        public async Task CanNativeResolveCircularRedirect()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44398")
            };
            try
            {
                var response = await client.GetAsync($"/api/behaviour/redirect/circular/0");
                Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
                // Possibly the native implementation will just do 50 redirects, like the linear
                // series?
                StringAssert.StartsWith(response.Headers.Location?.ToString(), "/api/behaviour/redirect/circular/");
            }
            catch (HttpRequestException hre)
            {
                throw new AssertInconclusiveException("Request failed. Possibly the server isn't running.", hre);
            }
        }

        [TestMethod]
        public async Task CanTabulateNativeRedirectBehaviour()
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44398")
            };
            var outcomes = await RedirectTestCases.ToAsyncEnumerable()
                .SelectAwait(async t =>
                {
                    var (method, status) = t;
                    var response = await client.SendAsync(new HttpRequestMessage(method, $"api/behaviour/redirect-echo?status={status}"));
                    return (method, status, response);
                })
                .Where(t => t.response.IsSuccessStatusCode)
                .ToLookupAsync(t => (t.method, redirectMethod: t.response.RequestMessage.Method, t.status));

            var table = string.Join(
                Environment.NewLine,
                outcomes.Select(g => $"{g.Key.method,6} | {g.Key.redirectMethod,6} | {g.Key.status}"));

            // Outcome:
            // 
            // request | redirected | redirect
            //  method |     method |   status
            // ========|============|==========
            //     GET |        GET |      300
            //     GET |        GET |      301
            //     GET |        GET |      302
            //     GET |        GET |      303
            //     GET |        GET |      307
            //     GET |        GET |      308
            //    POST |        GET |      300
            //    POST |        GET |      301
            //    POST |        GET |      302
            //    POST |        GET |      303
            //    POST |       POST |      307
            //    POST |       POST |      308
            //     PUT |        PUT |      300
            //     PUT |        PUT |      301
            //     PUT |        PUT |      302
            //     PUT |        PUT |      303
            //     PUT |        PUT |      307
            //     PUT |        PUT |      308
            //  DELETE |     DELETE |      300
            //  DELETE |     DELETE |      301
            //  DELETE |     DELETE |      302
            //  DELETE |     DELETE |      303
            //  DELETE |     DELETE |      307
            //  DELETE |     DELETE |      308
            //    HEAD |       HEAD |      300
            //    HEAD |       HEAD |      301
            //    HEAD |       HEAD |      302
            //    HEAD |       HEAD |      303
            //    HEAD |       HEAD |      307
            //    HEAD |       HEAD |      308
            //
            // Analysis:
            // 300-303; 307, 308 → do redirect
            // POST && <= 303 → GET
        }
    }
}
