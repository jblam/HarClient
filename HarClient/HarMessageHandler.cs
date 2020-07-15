using HarSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public HarMessageHandler()
            // JB 2020-06-11: per MS docs,
            // > Starting with .NET Core 2.1, the SocketsHttpHandler class provides the
            // > implementation used by higher-level HTTP networking classes such as HttpClient.
            //   -- https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler
#if NETCOREAPP2_1
            : base(new SocketsHttpHandler())
#else
            : base(new HttpClientHandler())
#endif
        { }

        public HarMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var entrySource = new HarEntrySource(request, DateTime.Now);
            entries.Add(entrySource);
            var response = await base.SendAsync(request, cancellationToken);
            entrySource.SetResponse(response);
            return response;
        }

        // JB 2020-06-19: Broad outline of workflow.
        // 1. Construct the HarMessageHandler instance
        // 2. Add that to the HttpClient. TODO: how does this interact with IHttpClientFactory?
        // 3. Run all the requests you wish to run
        // 4. Don't dispose the HttpClient, because apparently that's a footgun.
        // 5. Get the HAR out of the HarMessageHandler
        //
        // This produces the requirements
        // - need to store at least a mutable log of requests/responses
        // - at any point in the message handler lifetime, we need to produce valid HAR, so
        // - incomplete request/responses need to serialise correctly

        readonly List<HarEntrySource> entries = new List<HarEntrySource>();

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
            output.Log.Entries.AddRange(await Task.WhenAll(entries.Select(e => e.CreateEntryAsync(cancellationToken))));
            return output;
        }

        static Creator FromType(Type t) => new Creator
        {
            Name = t.FullName,
            Version = t.Assembly.GetName().Version.ToString()
        };
    }
}
