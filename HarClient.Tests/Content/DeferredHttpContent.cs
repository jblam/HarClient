using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient.Tests.Content
{
    class DeferredHttpContent : HttpContent
    {
        readonly TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();

        public void Resolve(Stream stream) => tcs.SetResult(stream);
        public void Explode(Exception exception) => tcs.SetException(exception);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var content = await tcs.Task;
            await content.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = default;
            return false;
        }
    }
}
