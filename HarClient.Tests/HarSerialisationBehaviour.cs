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
                    // TODO: Github issue #22: multipart is treated as binary, so anything that's
                    // not a single-byte UTF8 value will be wrong in UTF16.
                    { new StringContent("A"), "A", "file" },
                    { new StringContent("B"), "B" }
                }
            };
            var sut = new HarEntrySource(multipartRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task QueryStringRequestIsValid()
        {
            var queryStringRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net?key=value&utf-8=✔");
            var sut = new HarEntrySource(queryStringRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            Assert.IsTrue(entry.Request.QueryString.Count > 0, "Did not create any query string params");
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task UrlEncodedContentRequestIsValid()
        {
            var urlEncodedRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    KeyValuePair.Create("A→", "B"),
                    KeyValuePair.Create("C", "D✔")
                })
            };
            var sut = new HarEntrySource(urlEncodedRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            Assert.IsNotNull(entry.Request.PostData, "Failed to produce any postData");
            Assert.IsNotNull(entry.Request.PostData.Params, "URL-encoded content did not produce a params collection");
            Assert.IsTrue(entry.Request.PostData.Params.Any(), "URL-encoded content did not produce any params");
            HarAssert.IsValidRequest(entry.Request);
        }

        [TestMethod]
        public async Task IncorrectlyLabelledUrlEncodedContentRequestIsValid()
        {
            var lyingUrlEncodedRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net")
            {
                Content = new StringContent("I am not really URL-encoded params", Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            var sut = new HarEntrySource(lyingUrlEncodedRequest, default);
            var entry = await sut.CreateEntryAsync(default);
            // SPEC:
            // > <params>
            // > List of posted parameters, if any
            // Since this doesn't specify any behaviour in the case where the stated content-type
            // doesn't match the actual content, we'll accept any vaguely sensible outcome.
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
