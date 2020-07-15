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

        [TestMethod, Ignore]
        public async Task LogsInvalidUtf8()
        {
            var invalidUtf8 = new byte[] { 0b1100_0000, (byte)'a' };
            var har = await CreateSubmitLog(new ByteArrayContent(invalidUtf8, 0, 2));
            var postData = har.Log.Entries.First().Request.PostData;
            Assert.AreEqual("\u00c0a", postData.Text);

            // actually returns the replacement char \ufffd
            // The 1.3 spec by Ahmad Nassri resolves this by including an Encoding parameter inside
            // postData. For this specific case we might be able to fashion a UCS-2 code point
            // which works.
            // Could potentially use the UTF16 decoder on anything that's not obviously text, to
            // get a string that "smuggles out" arbitrary two-byte code points. This would cause
            // further issues with odd numbers of bytes, though.
            // Otherwise, can we use a single-byte-width codepage that roundtrips naïvely?
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
            CollectionAssert.AreEqual(content, request.QueryString.ToList());
        }
    }
}
