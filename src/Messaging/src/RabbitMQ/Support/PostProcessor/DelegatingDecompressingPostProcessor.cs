// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.PostProcessor
{
    public class DelegatingDecompressingPostProcessor : IMessagePostProcessor, IOrdered
    {
        private readonly Dictionary<string, IMessagePostProcessor> decompressors = new Dictionary<string, IMessagePostProcessor>();

        public DelegatingDecompressingPostProcessor()
        {
            this.decompressors.Add("gzip", new GUnzipPostProcessor());
            this.decompressors.Add("zip", new UnzipPostProcessor());
            this.decompressors.Add("deflate", new InflaterPostProcessor());
        }

        public int Order { get; set; }

        public void AddDecompressor(string contentEncoding, IMessagePostProcessor decompressor)
        {
            this.decompressors[contentEncoding] = decompressor;
        }

        public IMessagePostProcessor RemoveDecompressor(string contentEncoding)
        {
            decompressors.Remove(contentEncoding, out var result);
            return result;
        }

        public void SetDecompressors(Dictionary<string, IMessagePostProcessor> decompressors)
        {
            this.decompressors.Clear();
            foreach (var d in decompressors)
            {
                decompressors.Add(d.Key, d.Value);
            }
        }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return PostProcessMessage(message);
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var encoding = message.Headers.ContentEncoding();
            if (encoding == null)
            {
                return message;
            }
            else
            {
                int colonAt = encoding.IndexOf(':');
                if (colonAt > 0)
                {
                    encoding = encoding.Substring(0, colonAt);
                }

                decompressors.TryGetValue(encoding, out var decompressor);
                if (decompressor != null)
                {
                    return decompressor.PostProcessMessage(message);
                }
                else
                {
                    return message;
                }
            }
        }
    }
}
