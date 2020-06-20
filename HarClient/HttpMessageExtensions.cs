using HarSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient
{
    static class HttpMessageExtensions
    {
        public static Request CreateHarRequest(this HttpRequestMessage httpRequest)
        {
            var output = new Request
            {
                BodySize = GetBodySize(httpRequest.Content),
                HttpVersion = httpRequest.Version.ToString(),
                Method = httpRequest.Method.Method,
                PostData = new PostData(),
                Url = httpRequest.RequestUri,
            };
            output.Headers.AddRange(httpRequest.Headers.Select(AsHeader));
            if (httpRequest.Content != null)
            {
                output.Headers.AddRange(httpRequest.Content.Headers.Select(AsHeader));
                var wrappedContent = new HarContent(httpRequest.Content);
                httpRequest.Content = wrappedContent;
                _ = output.AppendContentAsync(wrappedContent);
            }
            return output;
        }

        public static Response CreateHarResponse(this HttpResponseMessage httpResponse)
        {
            var output = new Response
            {
                BodySize = GetBodySize(httpResponse.Content),
                HttpVersion = httpResponse.Version.ToString(),
                // TODO: RedirectUrl is required; needs to serialise as empty-string if value is null
                RedirectUrl = httpResponse.Headers.Location,
                Status = (int)httpResponse.StatusCode,
                StatusText = httpResponse.ReasonPhrase,
            };
            output.Headers.AddRange(httpResponse.Headers.Select(AsHeader));
            if (httpResponse.Content != null)
            {
                output.Headers.AddRange(httpResponse.Content.Headers.Select(AsHeader));
                var wrappedContent = new HarContent(httpResponse.Content);
                httpResponse.Content = wrappedContent;
                _ = output.AppendContentAsync(wrappedContent);
            }
            else
            {
                output.Content = new Content
                {
                    MimeType = "text/plain",
                    Size = 0,
                    Text = ""
                };
            }
            return output;
        }

        static int GetBodySize(this HttpContent content) =>
            content == null
                ? 0
                // TODO: JSON serialisation should provide the "-1 if null" behaviour
                : (int)Math.Min(content.Headers.ContentLength ?? -1, int.MaxValue);
        static Header AsHeader(this KeyValuePair<string, IEnumerable<string>> h) => new Header
            {
                Name = h.Key,
                Value = string.Join(",", h.Value)
            };

        static async Task AppendContentAsync(this Request harRequest, HarContent content)
        {
            var postData = await content.GetPostData();
            harRequest.PostData = postData;
        }
        static async Task AppendContentAsync(this Response harResponse, HarContent content)
        {
            var harContent = await content.GetContent();
            harResponse.Content = harContent;
        }
    }
}
