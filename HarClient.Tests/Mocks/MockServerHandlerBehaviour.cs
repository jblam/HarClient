﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HarClient.Tests.Mocks
{
    /// <summary>
    /// Meta-tests to assert the behaviour of the mock object.
    /// </summary>
    [TestClass]
    public class MockServerHandlerBehaviour
    {
        static MockServerHandler CreateHandler()
        {
            return new MockServerHandler
            {
                Responses =
                {
                    { "created", HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.Created) },
                    {
                        "test",
                        HttpMethod.Get,
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("Test"),
                        }
                    },
                    {
                        "redirect",
                        HttpMethod.Get,
                        new HttpResponseMessage(HttpStatusCode.Redirect)
                        {
                            Headers =
                            {
                                Location = new Uri(MockServerHandler.BaseUri, "/test")
                            }
                        }
                    },
                    {
                        "broken", HttpMethod.Get, new HttpRequestException("Out of peanuts")
                    }
                }
            };
        }

        static readonly HttpClient client = CreateHandler().CreateClient();

        [TestMethod]
        public async Task GetsContent()
        {
            var response = await client.GetAsync("test");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Test", await response.Content.ReadAsStringAsync());
        }
        [TestMethod]
        public async Task Redirects()
        {
            var response = await client.GetAsync("redirect");
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual(new Uri(MockServerHandler.BaseUri, "test"), response.Headers.Location);
        }
        [TestMethod, ExpectedException(typeof(TestException))]
        public async Task ThrowsIfUnexpectedRequest()
        {
            await client.GetAsync("http://garbage.url");
        }
        [TestMethod, ExpectedException(typeof(HttpRequestException))]
        public async Task ReturnsExceptionResponseAsDefined()
        {
            _ = await client.GetAsync("broken");
        }
    }
}
