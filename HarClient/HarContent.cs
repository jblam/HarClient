using HarSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace JBlam.HarClient
{
    class HarContent : HttpContent
    {
        public static HarContent WrapContent(HttpRequestMessage requestMessage)
        {
            if (requestMessage.Content == null)
                return new HarContent();
            else
            {
                var harContent = new HarContent(requestMessage.Content);
                requestMessage.Content = harContent;
                return harContent;
            }
        }
        public static HarContent WrapContent(HttpResponseMessage responseMessage)
        {
            if (responseMessage.Content == null)
                return new HarContent();
            else
            {
                var harContent = new HarContent(responseMessage.Content);
                responseMessage.Content = harContent;
                return harContent;
            }
        }

        HarContent()
        {
            bytesAsync = Task.FromResult(Array.Empty<byte>());
        }

        HarContent(HttpContent innerContent)
        {
            bytesAsync = innerContent.ReadAsByteArrayAsync();
            Headers.AddRange(innerContent.Headers);
        }

        readonly Task<byte[]> bytesAsync;

        // TODO: per Github issue #8, this should be the bytes transferred (with compression)
        // TODO: per Github issue #24 consider reimplementing the HAR model in order to encapsulate the `-1`
        //       with something more sematically-meaningful
        internal int HarBodySize => (int)Math.Min(Headers.ContentLength ?? -1, int.MaxValue);

        internal async Task<PostData?> GetPostData()
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            if (bytes.Length == 0)
            {
                return null;
            }
            else if (IsTextResponse(Headers.ContentType))
            {
                var output = new PostData
                {
                    MimeType = Headers.ContentType?.MediaType,
                    Text = Encoding.UTF8.GetString(bytes)
                };
                if (output.MimeType == "application/x-www-form-urlencoded")
                {
                    var queryParams = HttpUtility.ParseQueryString(output.Text);
                    output.Params.AddRange(queryParams.Select(t => new PostDataParameter { Name = t.key, Value = t.value }));
                }
                return output;
            }
            else
            {
                return new PostData
                {
                    MimeType = Headers.ContentType?.MediaType,
                    // Binary content is problematic in HAR 1.2 because there's no "meta encoding"
                    // on the postData object as there is for (response) content.
                    // For binary content, allow JSON.NET to escape the byte value where necessary.
                    // We hope that the interpreting implementation will naively translate the code
                    // point values back in to binary content.
                    Text = new string(bytes.Select(b => (char)b).ToArray())
                };
            }
        }
        internal async Task<Content> GetContent()
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            if (bytes.Length == 0)
            {
                return new Content
                {
                    MimeType = "text/plain",
                    Size = 0,
                    Text = ""
                };
            }
            else if (IsTextResponse(Headers.ContentType))
            {
                return new Content
                {
                    MimeType = Headers.ContentType.ToString(),
                    Encoding = null,
                    Size = bytes.Length,
                    Text = Encoding.UTF8.GetString(bytes)
                };
            }
            else
            {
                return new Content
                {
                    MimeType = Headers.ContentType?.ToString(),
                    Encoding = "base64",
                    Size = bytes.Length,
                    Text = Convert.ToBase64String(bytes),
                };
            }
        }
        static bool IsTextResponse(MediaTypeHeaderValue? mediaType)
        {
            if (mediaType is null)
                return false;
            if (mediaType.CharSet == Encoding.UTF8.WebName)
                return true;
            if (mediaType.MediaType.StartsWith("text/", StringComparison.InvariantCulture))
                return true;
            if (mediaType.MediaType.StartsWith("application/", StringComparison.InvariantCulture) || mediaType.MediaType.StartsWith("image/", StringComparison.InvariantCulture))
            {
                return mediaType.MediaType.EndsWith("json", StringComparison.InvariantCulture) ||
                    mediaType.MediaType.EndsWith("xml", StringComparison.InvariantCulture) ||
                    mediaType.MediaType.EndsWith("x-www-form-urlencoded", StringComparison.InvariantCulture);
            }
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            // paraphrasing https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpcontent.trycomputelength?view=netcore-3.1
            // return false if the length cannot be easily computed, and the system will buffer the
            // content for you.
            if (bytesAsync.Status == TaskStatus.RanToCompletion)
            {
                length = bytesAsync.Result.Length;
                return true;
            }
            else
            {
                length = default;
                return false;
            }
        }
    }
}
