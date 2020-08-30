using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace JBlam.HarClient
{
    static class HttpExtensions
    {
        public static HttpRequestMessage WithHeaders(this HttpRequestMessage message, HttpHeaders originalHeaders)
        {
            // Cursory examination of HttpHeaders source code indicates that this should never throw
            message.Headers.AddRange(originalHeaders);
            return message;
        }
        public static HttpRequestMessage CreateRedirectRequest(this HttpResponseMessage response, HttpRequestMessage originalRequest) =>
            new HttpRequestMessage(originalRequest.Method, new Uri(originalRequest.RequestUri, response.Headers.Location))
            {
                Content = originalRequest.Content,
            }.WithHeaders(originalRequest.Headers);
        public static bool IsRedirect(this HttpResponseMessage response, out Uri? location)
        {
            var output =
                response.StatusCode >= (HttpStatusCode)300 &&
                response.StatusCode < (HttpStatusCode)400 &&
                response.Headers.Location != null;
            location = output ? response.Headers.Location : null;
            return output;
        }
        public static Uri WithBase(this Uri maybeRelative, Uri presumedBase) =>
            maybeRelative.IsAbsoluteUri
                ? maybeRelative
                : new Uri(presumedBase, maybeRelative);
    }
}
