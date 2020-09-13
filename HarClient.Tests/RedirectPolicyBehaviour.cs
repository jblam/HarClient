using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests
{
    [TestClass]
    public class RedirectPolicyBehaviour
    {
        class ArbitraryMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void WillNotObserveRedirectsWhenNotSuppressingInner()
        {
            var innerHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            var p = new RedirectPolicyImplementation(innerHandler);
            Assert.AreEqual(true, p.OriginalInnerAutoRedirect);
            Assert.AreEqual(false, p.WillObserveRedirectMessages);
            Assert.AreEqual(false, p.WillProduceRedirectMessages);
        }

        [TestMethod]
        public void SuppressesHttpClientHandlerRedirects()
        {
            var innerHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            var p = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = RedirectPolicy.SuppressAndFollow
            };
            Assert.IsFalse(innerHandler.AllowAutoRedirect);
            Assert.AreEqual(true, p.OriginalInnerAutoRedirect);
            Assert.AreEqual(true, p.WillObserveRedirectMessages);
            Assert.AreEqual(false, p.WillProduceRedirectMessages);
        }

        [TestMethod]
        public void SuppressesSocketsHttpHandlerRedirects()
        {
            var innerHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = true
            };
            var p = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = RedirectPolicy.SuppressAndFollow
            };
            Assert.IsFalse(innerHandler.AllowAutoRedirect);
            Assert.AreEqual(true, p.OriginalInnerAutoRedirect);
            Assert.AreEqual(true, p.WillObserveRedirectMessages);
            Assert.AreEqual(false, p.WillProduceRedirectMessages);
        }

        [TestMethod]
        public void DoesNotSuppressHandlerWithoutKnownAutoRedirect()
        {
            var innerHandler = new ArbitraryMessageHandler();
            var p = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = new RedirectPolicy(false, true)
            };
            Assert.IsNull(p.OriginalInnerAutoRedirect);
            Assert.IsNull(p.WillObserveRedirectMessages);
            Assert.IsNull(p.WillProduceRedirectMessages);
        }

        [TestMethod]
        public void RestoresInnerAutoRedirectValue()
        {
            var innerHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = true
            };
            var p = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = RedirectPolicy.SuppressAndFollow
            };
            p.Policy = RedirectPolicy.DoNotSuppress;
            Assert.IsTrue(innerHandler.AllowAutoRedirect);
        }

        [TestMethod]
        public void DoesNotManufactureInnerAutoRedirectValue()
        {
            var innerHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false
            };
            var p = new RedirectPolicyImplementation(innerHandler)
            {
                Policy = RedirectPolicy.SuppressAndFollow
            };
            p.Policy = RedirectPolicy.DoNotSuppress;
            Assert.IsFalse(innerHandler.AllowAutoRedirect);
        }
    }
}
