// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;

public class DelegatingDecompressingPostProcessor : IMessagePostProcessor, IOrdered
{
    private readonly Dictionary<string, IMessagePostProcessor> _decompressors = new();

    public int Order { get; set; }

    public DelegatingDecompressingPostProcessor()
    {
        _decompressors.Add("gzip", new GUnzipPostProcessor());
        _decompressors.Add("zip", new UnzipPostProcessor());
        _decompressors.Add("deflate", new InflaterPostProcessor());
    }

    public void AddDecompressor(string contentEncoding, IMessagePostProcessor decompressor)
    {
        _decompressors[contentEncoding] = decompressor;
    }

    public IMessagePostProcessor RemoveDecompressor(string contentEncoding)
    {
        _decompressors.Remove(contentEncoding, out IMessagePostProcessor result);
        return result;
    }

    public void SetDecompressors(Dictionary<string, IMessagePostProcessor> decompressors)
    {
        _decompressors.Clear();

        foreach (KeyValuePair<string, IMessagePostProcessor> d in decompressors)
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
        string encoding = message.Headers.ContentEncoding();

        if (encoding == null)
        {
            return message;
        }

        int colonAt = encoding.IndexOf(':');

        if (colonAt > 0)
        {
            encoding = encoding.Substring(0, colonAt);
        }

        _decompressors.TryGetValue(encoding, out IMessagePostProcessor decompressor);

        if (decompressor != null)
        {
            return decompressor.PostProcessMessage(message);
        }

        return message;
    }
}
