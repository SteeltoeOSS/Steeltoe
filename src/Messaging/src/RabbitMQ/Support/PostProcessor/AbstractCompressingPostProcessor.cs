// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

public abstract class AbstractCompressingPostProcessor : IMessagePostProcessor, IOrdered
{
    protected readonly ILogger Logger;

    protected AbstractCompressingPostProcessor(ILogger logger = null)
        : this(true, logger)
    {
    }

    protected AbstractCompressingPostProcessor(bool autoDecompress, ILogger logger = null)
    {
        Logger = logger;
        AutoDecompress = autoDecompress;
    }

    public bool AutoDecompress { get; }

    public bool CopyHeaders { get; set; }

    public int Order { get; set; }

    public virtual IMessage PostProcessMessage(IMessage message)
    {
        try
        {
            var zipped = new MemoryStream();
            var compressor = GetCompressorStream(zipped);
            var payStream = new MemoryStream((byte[])message.Payload);
            payStream.CopyTo(compressor);
            compressor.Close();

            var compressed = zipped.ToArray();

            Logger?.LogTrace("Compressed " + ((byte[])message.Payload).Length + " to " + compressed.Length);

            return CreateMessage(message, compressed);
        }
        catch (IOException e)
        {
            throw new RabbitIOException(e);
        }
    }

    public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
    {
        return PostProcessMessage(message);
    }

    protected virtual IMessage CreateMessage(IMessage message, byte[] compressed)
    {
        var headers = RabbitHeaderAccessor.GetMutableAccessor(message.Headers);
        if (CopyHeaders)
        {
            headers = RabbitHeaderAccessor.GetMutableAccessor(new MessageHeaders(message.Headers, message.Headers.Id, message.Headers.Timestamp));
        }

        if (AutoDecompress)
        {
            headers.SetHeader(RabbitMessageHeaders.SpringAutoDecompress, true);
        }

        headers.ContentEncoding = message.Headers.ContentEncoding() == null
            ? GetEncoding()
            : $"{GetEncoding()}:{message.Headers.ContentEncoding()}";

        return Message.Create(compressed, headers.ToMessageHeaders());
    }

    protected abstract string GetEncoding();

    protected abstract Stream GetCompressorStream(Stream stream);
}
