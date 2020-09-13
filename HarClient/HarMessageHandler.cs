using HarSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JBlam.HarClient
{
    // This: http://www.softwareishard.com/blog/har-12-spec/#postData
    // That: https://github.com/giacomelli/HarSharp
    // Those: https://source.dot.net/#System.Net.Http/System/Net/Http/StringContent.cs,d17b034fe87f7436
    // Things: https://docs.microsoft.com/en-us/dotnet/api/system.net.http.stringcontent?view=netcore-3.1
    public class HarMessageHandler : DelegatingHandler
    {
        internal const int MaximumRedirectCount = 50;
        readonly RedirectPolicyImplementation redirectPolicyImplementation;

        /// <summary>
        /// Creates a new instance of <see cref="HarMessageHandler"/>, delegating to the default
        /// <see cref="HttpMessageHandler"/> implementation for the runtime environment.
        /// </summary>
        public HarMessageHandler()
#pragma warning disable CA2000 // Dispose objects before losing scope
            // Justification: ctor parameter is owned and disposed by the base class.
            // JB 2020-06-11: per MS docs,
            // > Starting with .NET Core 2.1, the SocketsHttpHandler class provides the
            // > implementation used by higher-level HTTP networking classes such as HttpClient.
            //   -- https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler
#if NETCOREAPP2_1
            : this(new SocketsHttpHandler(), RedirectPolicy.SuppressAndFollow)
#else
            : this(new HttpClientHandler(), RedirectPolicy.SuppressAndFollow)
#endif
#pragma warning restore CA2000 // Dispose objects before losing scope
        { }

        /// <summary>
        /// Creates a new instance of <see cref="HarMessageHandler"/>, delegating to the provided
        /// <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <param name="innerHandler">The inner handler which sends and receives messages.</param>
        /// <remarks>
        /// By default, the inner handler's auto-redirect behaviour is suppressed (see
        /// <seealso cref="RedirectPolicy.SuppressAndFollow"/>) so that this handler can observe
        /// redirect messages.
        /// </remarks>
        public HarMessageHandler(HttpMessageHandler innerHandler) : this(innerHandler, RedirectPolicy.SuppressAndFollow)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="HarMessageHandler"/>, delegating to the provided
        /// <see cref="HttpMessageHandler"/>, and using the specified policy for handling redirects.
        /// </summary>
        /// <param name="innerHandler">The inner handler which sends and receives messages.</param>
        /// <param name="redirectPolicy">The policy for handling redirect messages</param>
        public HarMessageHandler(HttpMessageHandler innerHandler, RedirectPolicy redirectPolicy) : base(innerHandler)
        {
            redirectPolicyImplementation = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = redirectPolicy
            };
        }

        /// <summary>
        /// Gets an instance of <see cref="JsonSerializerSettings"/> which is suitable for serialising
        /// an instance of <see cref="Har"/> as valid JSON.
        /// </summary>
        /// <example>
        /// Har har = CreateHar();
        /// var json = JsonConvert.SerializeObject(har, HarMessageHandler.HarSerializerSettings);
        /// </example>
        // TODO: move this somewhere discoverable
        public static JsonSerializerSettings HarSerializerSettings { get; } = new JsonSerializerSettings
        {
            // Note that changes here must be kept in sync with the test project's
            // HarAssert.ToJObject(object) implementation.
            ContractResolver = new HarContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
        };

        /// <summary>
        /// Gets or sets the effective redirect policy, which determines whether redirect messages
        /// are recorded in the HAR, and whether they are produced directly
        /// </summary>
        public RedirectPolicy RedirectPolicy
        {
            get => redirectPolicyImplementation.Policy;
            set => redirectPolicyImplementation.Policy = value;
        }

        protected override async Task<HttpResponseMessage?> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var nextRequest = request ?? throw new ArgumentNullException(nameof(request));
            var redirectSet = new HashSet<Uri>
            {
                nextRequest.RequestUri
            };
            while (true)
            {
                var response = await RequestOne(nextRequest, cancellationToken).ConfigureAwait(false);
                if (response != null &&
                    response.IsRedirect(out var location) &&
                    RedirectPolicy.FollowRedirects &&
                    redirectSet.Count <= MaximumRedirectCount &&
                    redirectSet.Add(location!.WithBase(nextRequest.RequestUri)))
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    // Justification: HttpRequestMessage disposes its content. The "temporary"
                    // redirect messages don't own the content; the original request does.
                    // Someone else is responsible for disposing that.
                    nextRequest = response.CreateRedirectRequest(request);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
                else
                {
                    return response;
                }
            }

            async Task<HttpResponseMessage> RequestOne(HttpRequestMessage m, CancellationToken t)
            {
                var entrySource = new HarEntrySource(m, DateTime.Now);
                entries.Add(entrySource);
                var response = await base.SendAsync(m, t).ConfigureAwait(false);
                entrySource.SetResponse(response);
                return response;
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=netcore-3.1
        // > If developers derive classes from HttpMessageHandler and override the SendAsync method,
        // > they must make sure that SendAsync can get called concurrently by different threads.
        readonly ConcurrentBag<HarEntrySource> entries = new ConcurrentBag<HarEntrySource>();

        public Task<Har> CreateHarAsync() => CreateHarAsync(null, CancellationToken.None);
        public Task<Har> CreateHarAsync(string? comment) => CreateHarAsync(comment, CancellationToken.None);
        public Task<Har> CreateHarAsync(CancellationToken cancellationToken) => CreateHarAsync(null, cancellationToken);
        public async Task<Har> CreateHarAsync(string? comment, CancellationToken cancellationToken)
        {
            var output = new Har
            {
                Log = new Log
                {
                    Browser = null,
                    Creator = FromType(typeof(HarMessageHandler)),
                    Comment = comment,
                    Version = "1.2",
                }
            };
            var harEntries = await Task.WhenAll(entries.Select(e => e.CreateEntryAsync(cancellationToken))).ConfigureAwait(false);
            output.Log.Entries.AddRange(harEntries.OrderBy(e => e.StartedDateTime));
            return output;
        }

        static Creator FromType(Type t) => new Creator
        {
            Name = t.FullName,
            Version = t.Assembly.GetName().Version.ToString()
        };
    }
}
