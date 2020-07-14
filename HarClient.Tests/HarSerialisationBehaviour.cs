using HarSharp;
using JBlam.HarClient.Tests.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
            var multipartRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent("A"), "A", "file" },
                    { new StringContent("B"), "B" }
                }
            };
            var harRequest = multipartRequest.CreateHarRequest();
            HarAssert.IsValidRequest(harRequest);
        }


        [TestMethod]
        public void UrlEncodedContentRequestIsValid()
        {
            var urlEncodedRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    KeyValuePair.Create("A", "B"),
                    KeyValuePair.Create("C", "D")
                })
            };
            var harRequest = urlEncodedRequest.CreateHarRequest();
            if (!harRequest.PostData.Params.Any())
            {
                // TODO: this actually fails with NullReferenceException because the content-
                // duplication is not implemented for Params.
                throw new TestException("URL-encoded content is expected to produce postData params");
            }
            HarAssert.IsValidRequest(harRequest);
        }

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

        [TestMethod]
        public void IllegalJsonCharactersAreEscaped()
        {
            const string illegalText = "newline:\r\n, unicode control char:\u0003; backspace:\b; tab:\t";
            const string escapedText = @"newline:\r\n, unicode control char:\u0003; backspace:\b; tab:\t";
            var harContent = new HarSharp.Content
            {
                Text = illegalText
            };
            var jsonString = JsonConvert.SerializeObject(harContent, HarMessageHandler.HarSerializerSettings);
            StringAssert.Contains(jsonString, escapedText);
        }
    }
}
