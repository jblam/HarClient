using HarSharp;
using JBlam.HarClient.Tests.Mocks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Content
{
    [TestClass]
    public class RequestContentBehaviour
    {
        static async Task<Har> CreateSubmitLog(HttpContent contentToSend)
        {
            const string requestPath = "/" + nameof(ResponseContentBehaviour) + "/" + nameof(CreateSubmitLog);
            var mockHandler = new MockServerHandler
            {
                Responses =
                {
                    {
                        requestPath,
                        HttpMethod.Post,
                        new HttpResponseMessage(System.Net.HttpStatusCode.Accepted)
                    }
                }
            };
            var sut = new HarMessageHandler(mockHandler);
            var client = new HttpClient(sut) { BaseAddress = MockServerHandler.BaseUri };
            await client.PostAsync(requestPath, contentToSend);
            return await sut.CreateHarAsync();
        }

        [TestMethod]
        public async Task LogsArbitraryMimeType()
        {
            const string expectedMediaType = "application/x-arbitrary";
            const string expectedContent = "test1234";
            var har = await CreateSubmitLog(new StringContent(expectedContent, Encoding.UTF8, expectedMediaType));
            var postData = har.Log.Entries.First().Request.PostData;
            StringAssert.StartsWith(postData.MimeType, expectedMediaType);
            Assert.AreEqual(expectedContent, postData.Text);
            Assert.AreEqual(0, postData.Params.Count, "Unexpected params content in request post-data");
        }

        [TestMethod]
        public async Task LogsInvalidUtf8()
        {
            var invalidUtf8 = new byte[] { 0b1100_0000, (byte)'a' };
            var har = await CreateSubmitLog(new ByteArrayContent(invalidUtf8, 0, 2));
            var postData = har.Log.Entries.First().Request.PostData;
            Assert.AreEqual("\u00c0a", postData.Text);
        }

        [TestMethod]
        public async Task LogsUrlEncodedParams()
        {
            var content = new[]
            {
                KeyValuePair.Create("key", "value")
            };
            var har = await CreateSubmitLog(new FormUrlEncodedContent(content));
            var postData = har.Log.Entries.First().Request.PostData;
            Assert.AreEqual(content.Length, postData.Params.Count, "Params length was not equal to input");
            Assert.AreEqual(content[0].Key, postData.Params[0].Name);
            Assert.AreEqual(content[0].Value, postData.Params[0].Value);
        }

        [TestMethod]
        public async Task HandlesLyingUrlEncodedMimeType()
        {
            var har = await CreateSubmitLog(new StringContent("I am not really URL-encoded params", Encoding.UTF8, "application/x-www-form-urlencoded"));
            var postData = har.Log.Entries.First().Request.PostData;
            // SPEC:
            // > <params>
            // > List of posted parameters, if any
            // Since this doesn't specify any behaviour in the case where the stated content-type
            // doesn't match the actual content, we'll accept any vaguely sensible outcome.
            Assert.IsNotNull(postData.Params);
        }

        [TestMethod]
        public async Task LogsQueryStringParams()
        {
            var content = new[]
            {
                new QueryStringParameter{ Name = "key1", Value = "value1" },
                new QueryStringParameter{ Name = "key2", Value = "value2" }
            };
            var builder = new UriBuilder("http://example.net")
            {
                Query = string.Join("&", content.Select(kv => $"{Uri.EscapeUriString(kv.Name)}={Uri.EscapeUriString(kv.Value)}"))
            };
            var entrySource = new HarEntrySource(new HttpRequestMessage(HttpMethod.Get, builder.Uri), default);
            var request = (await entrySource.CreateEntryAsync(default)).Request;
            Assert.IsNotNull(request.QueryString);
            CollectionAssert.AreEqual(content, request.QueryString.ToList(), Comparer<QueryStringParameter>.Create((q1, q2) =>
            {
                return DefaultToNull(string.Compare(q1.Name, q2.Name)) ??
                    DefaultToNull(string.Compare(q1.Value, q2.Value)) ??
                        DefaultToNull(string.Compare(q1.Comment, q2.Comment)) ?? 0;
                static int? DefaultToNull(int i) => i == default ? new int?() : i;
            }));
        }
    }
}
