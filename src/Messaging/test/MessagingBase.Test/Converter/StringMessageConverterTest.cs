// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test;

public class StringMessageConverterTest
{
    [Fact]
    public void FromByteArrayMessage()
    {
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromStringMessage()
    {
        IMessage message = MessageBuilder.WithPayload("ABC").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageNoContentTypeHeader()
    {
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageCharset()
    {
        string payload = "H\u00e9llo W\u00f6rld";
        byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(payload);

        IMessage message = MessageBuilder.WithPayload(bytes)
            .SetHeader(MessageHeaders.ContentType, new MimeType("text", "plain", Encoding.GetEncoding("ISO-8859-1"))).Build();

        var converter = new StringMessageConverter();
        Assert.Equal(payload, converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageDefaultCharset()
    {
        string payload = "H\u00e9llo W\u00f6rld";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        IMessage message = MessageBuilder.WithPayload(bytes).Build();
        var converter = new StringMessageConverter();
        Assert.Equal(payload, converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageTargetClassNotSupported()
    {
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        var converter = new StringMessageConverter();
        Assert.Null(converter.FromMessage<object>(message));
    }

    [Fact]
    public void ToMessage()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, MimeTypeUtils.TextPlain }
        };

        var headers = new MessageHeaders(map);
        var converter = new StringMessageConverter();
        IMessage message = converter.ToMessage("ABC", headers);
        string result = Encoding.UTF8.GetString((byte[])message.Payload);
        Assert.Equal("ABC", result);
    }
}
