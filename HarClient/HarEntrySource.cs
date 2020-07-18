using HarSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace JBlam.HarClient
{
    class HarEntrySource
    {
        public HarEntrySource(HttpRequestMessage requestMessage, DateTime startTime)
        {
            stopwatch = Stopwatch.StartNew();
            request = GetRequestAsync(requestMessage);
            this.startTime = startTime;
        }

        public void SetResponse(HttpResponseMessage responseMessage)
        {
            sendTime = stopwatch.Elapsed;
            response = GetResponseAsync(responseMessage);
        }

        readonly Stopwatch stopwatch;
        readonly Task<Request> request;
        Task<Response>? response;
        TimeSpan? sendTime;
        readonly DateTime startTime;


        internal static Header AsHeader(KeyValuePair<string, IEnumerable<string>> h) => new Header
        {
            Name = h.Key,
            Value = string.Join(",", h.Value)
        };
        Task<Request> GetRequestAsync(HttpRequestMessage httpRequest)
        {
            var harContent = HarContent.WrapContent(httpRequest);
            var request = new Request
            {
                BodySize = harContent.HarBodySize,
                HttpVersion = httpRequest.Version.ToString(),
                Method = httpRequest.Method.Method,
                Url = httpRequest.RequestUri,
            };
            request.QueryString.AddRange(
                HttpUtility.ParseQueryString(httpRequest.RequestUri.Query)
                    .Select(t => new QueryStringParameter { Name = t.key, Value = t.value }));
            request.Headers.AddRange(httpRequest.Headers.Select(AsHeader));
            request.Headers.AddRange(harContent.Headers.Select(AsHeader));
            return AppendContentAsync(harContent, request);

            static async Task<Request> AppendContentAsync(HarContent content, Request partialRequest)
            {
                partialRequest.PostData = await content.GetPostData().ConfigureAwait(false);
                return partialRequest;
            }
        }
        Task<Response> GetResponseAsync(HttpResponseMessage httpResponse)
        {
            var harContent = HarContent.WrapContent(httpResponse);
            var response = new Response
            {
                BodySize = harContent.HarBodySize,
                // TODO: Github issue #21: implement cookies
                HttpVersion = httpResponse.Version.ToString(),
                RedirectUrl = httpResponse.Headers.Location,
                Status = (int)httpResponse.StatusCode,
                StatusText = httpResponse.ReasonPhrase,
            };
            response.Headers.AddRange(httpResponse.Headers.Select(AsHeader));
            response.Headers.AddRange(harContent.Headers.Select(AsHeader));
            return AppendContentAsync(harContent, response);
            
            static async Task<Response> AppendContentAsync(HarContent content, Response partialResponse)
            {
                partialResponse.Content = await content.GetContent().ConfigureAwait(false);
                return partialResponse;
            }
        }

        public async Task<Entry> CreateEntryAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return CreateEntry(await request.ConfigureAwait(false), response == null ? null : await response.ConfigureAwait(false));
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateEntry(ResultIfSuccessful(request), ResultIfSuccessful(response));
            }
            else
            {
                var cancellationMixinSource = new TaskCompletionSource<object>();
                using var registration = cancellationToken.Register(() => cancellationMixinSource.TrySetCanceled());
                var requestOrCancelled = await Task.WhenAny(cancellationMixinSource.Task, request).ConfigureAwait(false);
                var responseOrCancelled = response == null
                    ? null
                    : await Task.WhenAny(cancellationMixinSource.Task, response).ConfigureAwait(false);
                return CreateEntry(
                    requestOrCancelled == request ? request.Result : null,
                    responseOrCancelled == response ? response?.Result : null);
            }
            // JB 2020-07-15 type restriction used here because we can't the return type as
            // possibly null without it. (see https://stackoverflow.com/q/54593923 )
            static T? ResultIfSuccessful<T>(Task<T>? t) where T : class =>
                t?.Status == TaskStatus.RanToCompletion ? t.Result : default;
        }
        Entry CreateEntry(Request? request, Response? response)
        {
            return new Entry
            {
                // TODO: Github issue #20: look in to whether we can actually implement this.
                Cache = new Cache(),
                // TODO: Github issue #21: implement cookies
                Request = request,
                Response = response,
                StartedDateTime = startTime,
                // TODO: Github issue #11: ensure this is spec-compliant and as complete as possible
                Timings = new Timings
                {
                    Send = sendTime?.TotalMilliseconds ?? -1,
                },
                Time = sendTime?.TotalMilliseconds ?? -1,
            };
        }
    }
}
