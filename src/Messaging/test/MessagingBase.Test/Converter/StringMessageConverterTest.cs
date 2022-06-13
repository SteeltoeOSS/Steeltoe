// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test;

public class StringMessageConverterTest
{
    [Fact]
    public void FromByteArrayMessage()
    {
        var message = MessageBuilder.WithPayload(
            Encoding.UTF8.GetBytes("ABC")).SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromStringMessage()
    {
        var message = MessageBuilder.WithPayload(
            "ABC").SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageNoContentTypeHeader()
    {
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        var converter = new StringMessageConverter();
        Assert.Equal("ABC", converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageCharset()
    {
        var payload = "H\u00e9llo W\u00f6rld";
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(payload);
        var message = MessageBuilder.WithPayload(bytes)
            .SetHeader(MessageHeaders.CONTENT_TYPE, new MimeType("text", "plain", Encoding.GetEncoding("ISO-8859-1"))).Build();
        var converter = new StringMessageConverter();
        Assert.Equal(payload, converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageDefaultCharset()
    {
        var payload = "H\u00e9llo W\u00f6rld";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var message = MessageBuilder.WithPayload(bytes).Build();
        var converter = new StringMessageConverter();
        Assert.Equal(payload, converter.FromMessage<string>(message));
    }

    [Fact]
    public void FromMessageTargetClassNotSupported()
    {
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        var converter = new StringMessageConverter();
        Assert.Null(converter.FromMessage<object>(message));
    }

    [Fact]
    public void ToMessage()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN }
        };
        var headers = new MessageHeaders(map);
        var converter = new StringMessageConverter();
        var message = converter.ToMessage("ABC", headers);
        var result = Encoding.UTF8.GetString((byte[])message.Payload);
        Assert.Equal("ABC", result);
    }
}
