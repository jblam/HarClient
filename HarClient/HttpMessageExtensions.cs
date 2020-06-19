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
            var bodySize = httpRequest.Content == null
                ? 0
                : httpRequest.Content.Headers.ContentLength ?? -1;
            var output = new Request
            {
                BodySize = (int)Math.Min(bodySize, int.MaxValue),
                HttpVersion = httpRequest.Version.ToString(),
                Method = httpRequest.Method.Method,
                Url = httpRequest.RequestUri,
            };
            output.Headers.AddRange(httpRequest.Headers.Select(h => new Header
            {
                Name = h.Key,
                Value = string.Join(",", h.Value)
            }));
            if (httpRequest.Content != null)
            {
                var wrappedContent = new HarContent(httpRequest.Content);
                httpRequest.Content = wrappedContent;
                _ = output.AppendContent(wrappedContent);
            }
            return output;
        }
        static async Task AppendContent(this Request harRequest, HarContent content)
        {
            var postData = await content.GetPostData();
            harRequest.PostData = postData;
        }
    }
}
