using HarSharp;
using JBlam.HarClient.Tests.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class HarSerialisationBehaviour
    {
        [TestMethod]
        public void EmptyHarIsValid()
        {
            var har = new HarMessageHandler().CreateHar();
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

        [TestMethod]
        public void MultipartFormContentRequestIsValid()
        {
            // TODO: resesarch expected behaviour
            var multipartRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent("A"), "A", "file" },
                    { new StringContent("B"), "B" }
                }
            };
            var harRequest = multipartRequest.CreateHarRequest();
            if (!harRequest.PostData.Params.Any())
            {
                throw new TestException("Multipart content is expected to produce postData params");
            }
            HarAssert.IsValidRequest(harRequest);
        }

        // TODO: research expected behaviour with binary content.

        [TestMethod]
        public void StringContentResponseIsValid()
        {
            var stringResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("Good job")
            };
            var harResponse = stringResponse.CreateHarResponse();
            Assert.IsNotNull(harResponse.Content?.Text);
            HarAssert.IsValidResponse(harResponse);
        }

        // TODO: research expected behaviour with binary content.

        // TODO: research how encoding should work in HAR content.
    }
}
