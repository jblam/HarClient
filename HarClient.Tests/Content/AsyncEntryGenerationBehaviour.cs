using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Content
{
    [TestClass]
    public class AsyncEntryGenerationBehaviour
    {
        [TestMethod]
        public void CanCancelSerialisation()
        {
            var arbitraryRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var unresolvableResponse = new HttpResponseMessage() { Content = new DeferredHttpContent() };
            var sut = new HarEntrySource(arbitraryRequest, default);
            sut.SetResponse(unresolvableResponse);
            var cancellationTokenSource = new CancellationTokenSource();
            var entryTask = sut.CreateEntryAsync(cancellationTokenSource.Token);
            if (entryTask.IsCompleted)
                throw new TestInvariantViolatedException("Creation of entry unexpectedly resolved before content was copied");
            cancellationTokenSource.Cancel();
            Assert.IsTrue(entryTask.IsCompletedSuccessfully, "Entry task failed to complete after cancelling unresolvable content duplication");
            var entry = entryTask.Result;
            Assert.IsNotNull(entry.Request, "Cancelled HAR entry generation did not record the synchronously-available request");
            // JB 2020-07-15: at time of writing, the implementation does not return a partially-
            // complete Response object if the content is not available.
            Assert.IsNull(entry.Response, "HAR entry unexpectedly has a response, even though the content is unavailable");
        }

        [TestMethod]
        public void CanCancelSerialisationBeforeResponse()
        {
            var unresolvableRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var sut = new HarEntrySource(unresolvableRequest, default);
            var cancellationTokenSource = new CancellationTokenSource();
            var entryTask = sut.CreateEntryAsync(cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            Assert.IsTrue(entryTask.IsCompletedSuccessfully, "Entry task failed to complete after cancelling unresolvable content duplication");
            var entry = entryTask.Result;
            Assert.IsNotNull(entry.Request, "Cancelled HAR entry generation did not record the synchronously-available request");
        }

        [TestMethod]
        public void CanCancelSerialisationBeforeRequest()
        {
            var unresolvableRequest = new HttpRequestMessage(HttpMethod.Post, "http://example.net") { Content = new DeferredHttpContent() };
            var sut = new HarEntrySource(unresolvableRequest, default);
            var entryTask = sut.CreateEntryAsync(new CancellationToken(true));
            Assert.IsTrue(entryTask.IsCompletedSuccessfully, "Entry task failed to complete");
            Assert.IsNull(entryTask.Result.Request);
        }

        [TestMethod]
        public void WaitsForContentToResolve()
        {
            var arbitraryRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var deferredResponseContent = new DeferredHttpContent();
            var deferredResponse = new HttpResponseMessage { Content = deferredResponseContent };
            var sut = new HarEntrySource(arbitraryRequest, default);
            sut.SetResponse(deferredResponse);
            var entryTask = sut.CreateEntryAsync(default);
            if (entryTask.IsCompleted)
                throw new TestInvariantViolatedException("Entry task unexpectedly completed before content is available");
            deferredResponseContent.Resolve(new MemoryStream(Encoding.UTF8.GetBytes("Hello")));
            Assert.AreEqual(TaskStatus.RanToCompletion, entryTask.Status, "SUT failed to produce an entry when the content copy was completed");
        }

        [TestMethod]
        public async Task ExceptionThrownDuringTranscriptionCanBeCaught()
        {
            var arbitraryRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.net");
            var deferredResponseContent = new DeferredHttpContent();
            var deferredResponse = new HttpResponseMessage { Content = deferredResponseContent };
            var sut = new HarEntrySource(arbitraryRequest, default);
            sut.SetResponse(deferredResponse);
            var entryTask = sut.CreateEntryAsync(default);
            if (entryTask.IsCompleted)
                throw new TestInvariantViolatedException("Entry task unexpectedly completed before content is available");
            deferredResponseContent.Explode(new TestExpectedException("Out of peanuts"));
            await Assert.ThrowsExceptionAsync<TestExpectedException>(() => entryTask);
        }
    }
}
