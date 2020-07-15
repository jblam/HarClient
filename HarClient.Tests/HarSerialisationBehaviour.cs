using HarSharp;
using JBlam.HarClient.Tests.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class HarSerialisationBehaviour
    {
        [TestMethod]
        public async Task EmptyHarIsValid()
        {
            var har = await new HarMessageHandler().CreateHarAsync();
            HarAssert.IsValid(har);
        }

        [TestMethod]
        public async Task GetRequestIsValid()
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var sut = new HarEntrySource(httpRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task StringContentRequestIsValid()
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new StringContent("STRING CONTENT")
            };
            var sut = new HarEntrySource(httpRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            Assert.IsNotNull(entry.Request.PostData?.Text);
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task MultipartFormContentRequestIsValid()
        {
            var multipartRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent("A"), "A", "file" },
                    { new StringContent("B"), "B" }
                }
            };
            var sut = new HarEntrySource(multipartRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            HarAssert.IsValidRequest(entry.Request);
        }


        [TestMethod]
        public async Task UrlEncodedContentRequestIsValid()
        {
            var urlEncodedRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    KeyValuePair.Create("A", "B"),
                    KeyValuePair.Create("C", "D")
                })
            };
            var sut = new HarEntrySource(urlEncodedRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            Assert.IsNotNull(entry.Request.PostData);
            if (!entry.Request.PostData.Params.Any())
            {
                // TODO: this actually fails with NullReferenceException because the content-
                // duplication is not implemented for Params.
                throw new TestException("URL-encoded content is expected to produce postData params");
            }
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task StringContentResponseIsValid()
        {
            var arbitraryRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var stringResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("Good job")
            };
            var sut = new HarEntrySource(arbitraryRequest, default);
            sut.SetResponse(stringResponse);
            var entry = await sut.CreateEntryAsync(default);
            Assert.IsNotNull(entry.Response);
            Assert.IsNotNull(entry.Response.Content?.Text);
            HarAssert.IsValidResponse(entry.Response);
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
