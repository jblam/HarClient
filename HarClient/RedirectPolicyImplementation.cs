using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace JBlam.HarClient
{
    class RedirectPolicyImplementation
    {
        private readonly HttpMessageHandler handler;
        private RedirectPolicy policy = default;

        public RedirectPolicyImplementation(HttpMessageHandler handler)
        {
            this.handler = handler;
            OriginalInnerAutoRedirect = GetAllowAutoRedirect(handler);
        }

        public RedirectPolicy Policy
        {
            get => policy;
            set
            {
                policy = value;
                SetAllowAutoRedirect(handler, value.SuppressInnerAutoRedirect
                    ? false
                    : OriginalInnerAutoRedirect ?? false);
            }
        }
        public bool? OriginalInnerAutoRedirect { get; }

        public bool? WillObserveRedirectMessages => !GetAllowAutoRedirect(handler);

        public bool? WillProduceRedirectMessages =>
            !Policy.FollowRedirects & WillObserveRedirectMessages;

        static bool? GetAllowAutoRedirect(HttpMessageHandler httpMessageHandler)
        {
            bool? socketsInnerHandlerSetting =
#if NETCOREAPP2_1
                (httpMessageHandler as SocketsHttpHandler)?.AllowAutoRedirect;
#else
                null;
#endif
            return socketsInnerHandlerSetting ?? (httpMessageHandler as HttpClientHandler)?.AllowAutoRedirect;
        }
        static void SetAllowAutoRedirect(HttpMessageHandler httpMessageHandler, bool value)
        {
#if NETCOREAPP2_1
            if (httpMessageHandler is SocketsHttpHandler socks)
                socks.AllowAutoRedirect = value;
#endif
            if (httpMessageHandler is HttpClientHandler http)
                http.AllowAutoRedirect = value;
        }
    }
}
