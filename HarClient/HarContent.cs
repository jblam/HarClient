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

namespace JBlam.HarClient
{
    class HarContent : HttpContent
    {
        public HarContent(HttpContent innerContent)
        {
            bytesAsync = innerContent.ReadAsByteArrayAsync();
            Headers.AddRange(innerContent.Headers);
        }

        readonly Task<byte[]> bytesAsync;

        internal async Task<PostData?> GetPostData()
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            if (bytes.Length > 0)
            {
                // TODO: how to detect encoding properly?
                // TODO: params?
                return new PostData
                {
                    MimeType = Headers.ContentType.MediaType,
                    Text = Convert.ToBase64String(bytes)
                };
            }
            else
            {
                // Zero-length content is used because .NET separates the "content headers" from
                // the other headers. From HAR's perspective, there is no post data.
                return null;
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
            else
            {
                if (IsTextResponse(Headers.ContentType))
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

                static bool IsTextResponse(MediaTypeHeaderValue? mediaType)
                {
                    if (mediaType is null)
                        return false;
                    if (mediaType.CharSet == Encoding.UTF8.WebName)
                        return true;
                    if (mediaType.MediaType.StartsWith("text/"))
                        return true;
                    if (mediaType.MediaType.StartsWith("application/") || mediaType.MediaType.StartsWith("image/"))
                    {
                        return mediaType.MediaType.EndsWith("json") ||
                            mediaType.MediaType.EndsWith("xml");
                    }
                    return false;
                }
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            // TODO: research expected semantics
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
