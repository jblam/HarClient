using HarSharp;
using JBlam.HarClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Content
{
    [TestClass]
    public class ResponseContentBehaviour
    {
        static async Task<Har> GetContent(HttpContent content)
        {
            const string requestPath = "/" + nameof(ResponseContentBehaviour) + "/" + nameof(GetContent);
            var mockHandler = new MockServerHandler
            {
                Responses =
                {
                    {
                        requestPath,
                        HttpMethod.Get,
                        new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = content }
                    }
                }
            };
            var sut = new HarMessageHandler(mockHandler);
            var client = new HttpClient(sut) { BaseAddress = MockServerHandler.BaseUri };
            (await client.GetAsync(requestPath)).EnsureSuccessStatusCode();
            return sut.CreateHar();
        }

        static void AssertIsMimeType(string expectedMediaType, Encoding? expectedEncoding, string actualMimeType)
        {
            if (expectedEncoding == null)
            {
                // The charset must be not emitted
                Assert.AreEqual(expectedMediaType, actualMimeType, "Media type did not match the expected value");
            }
            else
            {
                // Spec:
                // > *mimeType [string]* - MIME type of the response text (value of the Content-Type
                // > response header). The charset attribute of the MIME type is included (if
                // > available).
                //
                // Since the mock HTTP handler will always produce a content-type value, the test
                // should fail if the HAR doesn't represent content-type in the `mimeType` field.
                var match = Regex.Match(actualMimeType, @"^(.*); charset=(.*)$");
                Assert.IsTrue(match.Success, "MIME type {0} was not well-formed", actualMimeType);
                var (actualMediaType, actualCharSet) = (match.Groups[1].Value, match.Groups[2].Value);
                Assert.AreEqual(expectedMediaType, actualMediaType, "Media type did not match the expected value");
                Assert.AreEqual(expectedEncoding.WebName, actualCharSet, "CharSet did not match the expected value");
            }
        }

        [TestMethod]
        public void StringContentHeadersProduceExpectedMimeType()
        {
            var content = new StringContent("Plain text");
            Assert.AreEqual("text/plain; charset=utf-8", content.Headers.ContentType.ToString());
            Assert.AreEqual("text/plain", content.Headers.ContentType.MediaType);
            Assert.AreEqual("utf-8", content.Headers.ContentType.CharSet);
        }

        [TestMethod]
        public async Task LogsAsciiPlain()
        {
            const string expectedHarContentValue = "Plain text";
            var content = new StringContent(expectedHarContentValue);
            var har = await GetContent(content);
            var harContent = har.Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            AssertIsMimeType(MediaTypeNames.Text.Plain, Encoding.UTF8, harContent.MimeType);
            Assert.AreEqual(expectedHarContentValue, harContent.Text);
            Assert.AreEqual(expectedHarContentValue.Length, harContent.Size);
            Assert.IsNull(harContent.Encoding, "Unexpected not-null Encoding value for text response");
        }

        [TestMethod]
        public async Task LogsAsciiJson()
        {
            const string expectedMediaType = MediaTypeNames.Application.Json;
            const string expectedJson = "{ \"content\": \"yes\" }";
            var content = new StringContent(expectedJson, Encoding.UTF8, expectedMediaType);
            var harContent = (await GetContent(content)).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            AssertIsMimeType(expectedMediaType, Encoding.UTF8, harContent.MimeType);
            Assert.AreEqual(expectedJson, harContent.Text);
            Assert.AreEqual(expectedJson.Length, harContent.Size);
            Assert.IsNull(harContent.Encoding, "Unexpected not-null Encoding value for text response");
        }

        [TestMethod]
        public async Task LogsMultibyteUtf8()
        {
            // This is a string containing Unicode which has a multibyte UTF8 representation. It
            // also requires surrogate pairs in UTF-16.
            // The string does not change under different Unicode normalisations, which is
            // important when making assetions about the Size property.
            const string stringValue = "Unicode: 👍 很好";
            var content = new StringContent(stringValue);
            var harContent = (await GetContent(content)).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            AssertIsMimeType(MediaTypeNames.Text.Plain, Encoding.UTF8, harContent.MimeType);
            // The spec is weird when it comes to this point:
            // > *text [string, optional]* - Response body sent from the server or loaded from the
            // > browser cache. This field is populated with textual content only. The text field
            // > is either HTTP decoded text or a encoded (e.g. "base64") representation of the
            // > response body. Leave out this field if the information is not available.
            // > *encoding [string, optional]* (new in 1.2) - Encoding used for response text field
            // > e.g "base64". Leave out this field if the text field is HTTP decoded (decompressed
            // > & unchunked), than trans-coded from its original character set into UTF-8.
            //
            // This implies that the `text` property will be "interpreted as UTF-8", unless
            // `encoding` is "base64" (or some other not-null value).
            //
            // However, HAR must be valid JSON. The JSON spec says that all "unicode escapes" must
            // be FOUR hex digits, implying that JSON will interpret unicode escape points as
            // UTF-16:
            // > character
            // >   '0020'. '10FFFF' - '"' - '\'
            // >   '\' escape
            // >
            // > escape
            // >   '"'
            // >   '\'
            // >   '/'
            // >   'b'
            // >   'f'
            // >   'n'
            // >   'r'
            // >   't'
            // >   'u' hex hex hex hex
            //
            // The Firefox HAR sample produces both direct characters, and UTF-16 escapes. It's
            // unclear at this time what influences the choice. This library will only produce
            // direct characters.
            Assert.AreEqual(stringValue, harContent.Text);
            Assert.AreEqual(Encoding.UTF8.GetByteCount(stringValue), harContent.Size);
        }
        /// <summary>
        /// Asserts that the HAR object representation contains characters which are illegal in
        /// JSON. The serialiser is in charge of ensuring they are properly escaped.
        /// </summary>
        /// <returns>A Task which resolves when the test is complete</returns>
        /// <remarks>
        /// Escaping behaviour is asserted in
        /// <seealso cref="HarSerialisationBehaviour.IllegalJsonCharactersAreEscaped"/>
        /// </remarks>
        [TestMethod]
        public async Task ObjectRepresentationContainsIllegalJsonCharacters()
        {
            const string textNeedingEscapes = "String with newlines:\r\nand tabs:\t backspaces:\b null:\0 and other control chars:\u0003;\u0019;\u001f and quotes:\"";
            var httpContent = new StringContent(textNeedingEscapes);
            var harContent = (await GetContent(httpContent)).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            AssertIsMimeType(MediaTypeNames.Text.Plain, Encoding.UTF8, harContent.MimeType);
            // Size refers to the encoded length; all chars are 1-byte in UTF-8
            Assert.AreEqual(textNeedingEscapes.Length, harContent.Size);
            Assert.AreEqual(textNeedingEscapes, harContent.Text);
        }

        [TestMethod]
        public async Task LogsBinaryImage()
        {
            var imageBytes = new byte[]
            {
                // smallest possible GIF
                0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00,
                0x00, 0xff, 0x00, 0x2c, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00,
                0x01, 0x00, 0x00, 0x02, 0x00, 0x3b,
            };
            var imageHttpContent = new ByteArrayContent(imageBytes);
            imageHttpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Image.Gif);
            var harContent = (await GetContent(imageHttpContent)).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            AssertIsMimeType(MediaTypeNames.Image.Gif, null, harContent.MimeType);
            // Size should equal the *content* size, not the size of the *HAR's representation*
            // of the content.
            Assert.AreEqual(imageBytes.Length, harContent.Size, "Unexpected content size reported");
            Assert.AreEqual("base64", harContent.Encoding, "Binary image data was unexpectedly not base64-encoded for the HAR");
            Assert.AreEqual(Convert.ToBase64String(imageBytes), harContent.Text);
        }

        [TestMethod]
        public async Task LogsForResponseMissingContentTypeHeader()
        {
            var bytes = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            var harContent = (await GetContent(new ByteArrayContent(bytes))).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            Assert.AreEqual(bytes.Length, harContent.Size, "Unexpected content size reported");
            Assert.AreEqual("base64", harContent.Encoding, "Unknown format data was unexpectedly not base64-encoded for the HAR");
            Assert.AreEqual(Convert.ToBase64String(bytes), harContent.Text);
        }

        [TestMethod]
        public async Task LogsSvgImageAsText()
        {
            const string svgText = @"<svg xmlns=""http://www.w3.org/2000/svg"" encoding=""utf-8""></svg>";
            var httpContent = new StringContent(svgText, Encoding.UTF8, "image/svg+xml");
            // charset appears to be legal according to https://tools.ietf.org/html/rfc7231#section-3.1.1.2
            // but does not appear to be emitted in practice.
            httpContent.Headers.ContentType.CharSet = null;
            var harContent = (await GetContent(httpContent)).Log.Entries.First().Response.Content;
            Assert.IsNotNull(harContent, "No content was recorded in the HAR");
            Assert.IsNull(harContent.Encoding, "HAR unexpectedly re-encoded the SVG text");
            AssertIsMimeType("image/svg+xml", null, harContent.MimeType);
            Assert.AreEqual(svgText, harContent.Text);
        }
    }
}
