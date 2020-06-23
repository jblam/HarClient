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
    [TestClass]
    public class UnitTest1
    {
        // JB 2020-06-19: the following are smoke tests supporting initial exploratory development.
        // TODO: remove this as part of issue #12
        #region temp smoke tests
        [TestMethod]
        public async Task LogsLocalhost()
        {
            var (sut, client) = MockClient.Create(
                "test",
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") });
            _ = await client.PostAsync("test", new StringContent("Hello"));
            var har = sut.CreateHar();
            var harString = JsonConvert.SerializeObject(har, HarMessageHandler.HarSerializerSettings);
            Assert.IsNotNull(har);
        }
        #endregion
    }
}
