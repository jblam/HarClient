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
            new HttpRequestMessage(GetRedirectMethod(originalRequest, response.StatusCode), new Uri(originalRequest.RequestUri, response.Headers.Location))
            {
                Content = originalRequest.Content,
            }.WithHeaders(originalRequest.Headers);
        static HttpMethod GetRedirectMethod(HttpRequestMessage originalMessage, HttpStatusCode redirectResponseCode) =>
            originalMessage.Method != HttpMethod.Post
            ? originalMessage.Method
            : redirectResponseCode switch
            {
                HttpStatusCode.Ambiguous => HttpMethod.Get,
                HttpStatusCode.Moved => HttpMethod.Get,
                HttpStatusCode.Found => HttpMethod.Get,
                HttpStatusCode.RedirectMethod => HttpMethod.Get,
                _ => originalMessage.Method,
            };
        public static bool IsRedirect(this HttpResponseMessage response, out Uri? location)
        {
            var hasRedirectableStatus = response.StatusCode switch
            {
                HttpStatusCode.Ambiguous => true,
                HttpStatusCode.Moved => true,
                HttpStatusCode.Found => true,
                HttpStatusCode.RedirectMethod => true,
                HttpStatusCode.NotModified => false,
                HttpStatusCode.UseProxy => false,
                HttpStatusCode.Unused => false,
                HttpStatusCode.RedirectKeepVerb => true,
                (HttpStatusCode)308 => true,
                _ => false,
            };
            var canRedirect = hasRedirectableStatus && response.Headers.Location != null;
            location = hasRedirectableStatus ? response.Headers.Location : null;
            return canRedirect;
        }
        public static Uri WithBase(this Uri maybeRelative, Uri presumedBase) =>
            maybeRelative.IsAbsoluteUri
                ? maybeRelative
                : new Uri(presumedBase, maybeRelative);
    }
}
