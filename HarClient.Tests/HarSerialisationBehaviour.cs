using HarSharp;
using JBlam.HarClient.Tests.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class HarSerialisationBehaviour
    {
        static Har CreateEmptyHar() => new HarMessageHandler().CreateHar();

        [TestMethod]
        public void EmptyHarIsValid()
        {
            var har = CreateEmptyHar();
            HarAssert.IsValid(har);
        }

        [TestMethod]
        public void GetRequestIsValid()
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var harRequest = httpRequest.CreateHarRequest();
            HarAssert.IsValidRequest(harRequest);
        }

        [TestMethod]
        public void StringContentRequestIsValid()
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new StringContent("STRING CONTENT")
            };
            var harRequest = httpRequest.CreateHarRequest();
            Assert.IsNotNull(harRequest.PostData?.Text);
            HarAssert.IsValidRequest(harRequest);
        }
    }
}
