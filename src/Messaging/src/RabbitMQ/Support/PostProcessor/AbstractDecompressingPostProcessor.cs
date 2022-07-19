// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using System;
using System.IO;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

public abstract class AbstractDecompressingPostProcessor : IMessagePostProcessor, IOrdered
{
    protected AbstractDecompressingPostProcessor()
        : this(false)
    {
    }

    protected AbstractDecompressingPostProcessor(bool alwaysDecompress)
    {
        AlwaysDecompress = alwaysDecompress;
    }

    public int Order { get; set; }

    public bool AlwaysDecompress { get; }

    public virtual IMessage PostProcessMessage(IMessage message)
    {
        var autoDecompress = message.Headers.Get<bool?>(RabbitMessageHeaders.SpringAutoDecompress);
        if (AlwaysDecompress || (autoDecompress != null && autoDecompress.Value))
        {
            try
            {
                var compressed = new MemoryStream((byte[])message.Payload);
                var decompressor = GetDeCompressorStream(compressed);
                var outStream = new MemoryStream();
                decompressor.CopyTo(outStream);
                decompressor.Close();

                var headers = RabbitHeaderAccessor.GetMutableAccessor(message.Headers);
                var encoding = headers.ContentEncoding;
                var colonAt = encoding.IndexOf(':');
                if (colonAt > 0)
                {
                    encoding = encoding.Substring(0, colonAt);
                }

                if (!GetEncoding().Equals(encoding))
                {
                    throw new InvalidOperationException($"Content encoding must be:{GetEncoding()}, was:{encoding}");
                }

                headers.ContentEncoding = colonAt < 0 ? null : headers.ContentEncoding.Substring(colonAt + 1);

                headers.RemoveHeader(RabbitMessageHeaders.SpringAutoDecompress);
                return Message.Create(outStream.ToArray(), headers.ToMessageHeaders());
            }
            catch (IOException e)
            {
                throw new RabbitIOException(e);
            }
        }
        else
        {
            return message;
        }
    }

    public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
    {
        return PostProcessMessage(message);
    }

    protected abstract string GetEncoding();

    protected abstract Stream GetDeCompressorStream(Stream stream);
}
