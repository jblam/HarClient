using HarSharp;
using System;
using System.Collections.Generic;
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
#if NETCOREAPP2_1
            : base(new SocketsHttpHandler())
#else
            : base(new HttpClientHandler())
#endif
        { }

        public HarMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }

        readonly List<Entry> entries = new List<Entry>();

        public Har CreateHar() => CreateHar(null);
        public Har CreateHar(string comment)
        {
            return new Har
            {
                Log = new Log
                {
                    Browser = null,
                    Creator = FromType(typeof(HarMessageHandler)),
                    Comment = comment,
                }
            };
        }

        static Creator FromType(Type t) => new Creator
        {
            Name = t.FullName,
            Version = t.Assembly.GetName().Version.ToString()
        };
    }
}
