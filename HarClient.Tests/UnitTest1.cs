using JBlam.HarClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace HarClient.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.IsInstanceOfType(new HarMessageHandler(), typeof(HttpMessageHandler));
        }

        [TestMethod]
        public void CreatesEmptyHar()
        {
            var har = new HarMessageHandler().CreateHar();
            Assert.IsNotNull(har);
        }
    }
}
