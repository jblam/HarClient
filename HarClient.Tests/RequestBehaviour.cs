using JBlam.HarClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class RequestBehaviour
    {
        [TestMethod]
        public async Task NativeDoesRedirect()
        {
            const int initialRedirectIdentifier = 17;
            var handler = new HarMessageHandler();
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:44398")
            };
            var response = await client.GetAsync($"/api/behaviour/redirect/infinite/{initialRedirectIdentifier}");
            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            Assert.AreEqual(
                $"/api/behaviour/redirect/infinite/{initialRedirectIdentifier + HarMessageHandler.MaximumRedirectCount + 1}",
                response.Headers.Location.ToString());
        }
    }
}
