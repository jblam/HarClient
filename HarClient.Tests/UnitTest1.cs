using JBlam.HarClient.Tests.Mocks;
using JBlam.HarClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    // TODO: design unit tests properly.

    // Wishlist:
    // - synthesize HTTP traffic for inputs { GET, POST, etc. } × outputs { 200, 201, 3xx, 4xx, 5xx }
    // Investigate:
    // - expected behaviour for cancelled requests
    // - possibility of producing a <timings> object
    // - possibility of producing header content sizes
    // Test that:
    // - ensure "one time only" content is serialised exactly once
    // - respect content encodings for strings (minimal UTF-x).
    // - produce base64 for non-string encodings
    [TestClass]
    public class UnitTest1
    {
        // JB 2020-06-19: the following are smoke tests supporting initial exploratory development.
        // They should be removed ASAP
        #region temp smoke tests
        [TestMethod]
        public void CreatesEmptyHar()
        {
            var har = new HarMessageHandler().CreateHar();
            var harString = JsonConvert.SerializeObject(har, HarMessageHandler.HarSerializerSettings);
            Assert.IsNotNull(har);
        }

        [TestMethod]
        public async Task LogsLocalhost()
        {
            var mock = new MockServerHandler
            {
                Responses =
                {
                    { "test", HttpMethod.Post, new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") } }
                }
            };
            var sut = new HarMessageHandler(mock);
            var client = new HttpClient(sut)
            {
                BaseAddress = MockServerHandler.BaseUri
            };
            _ = await client.PostAsync("test", new StringContent("Hello"));
            var har = sut.CreateHar();
            var harString = JsonConvert.SerializeObject(har, HarMessageHandler.HarSerializerSettings);
            Assert.IsNotNull(har);
        }
        #endregion
    }
}
