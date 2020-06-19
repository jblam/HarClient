﻿using HarSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JBlam.HarClient
{
    class HarContent : HttpContent
    {
        public HarContent(HttpContent innerContent)
        {
            bytesAsync = innerContent.ReadAsByteArrayAsync();
            Headers.AddRange(innerContent.Headers);
        }

        readonly Task<byte[]> bytesAsync;

        internal async Task<PostData> GetPostData()
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            // TODO: how to detect encoding properly?
            // TODO: params?
            return new PostData
            {
                MimeType = "text/base64",
                Text = Convert.ToBase64String(bytes)
            };
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var bytes = await bytesAsync.ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            // TODO: research expected semantics
            if (bytesAsync.Status == TaskStatus.RanToCompletion)
            {
                length = bytesAsync.Result.Length;
                return true;
            }
            else
            {
                length = default;
                return false;
            }
        }
    }
}
