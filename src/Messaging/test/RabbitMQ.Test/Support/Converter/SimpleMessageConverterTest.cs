// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Extensions;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public class SimpleMessageConverterTest
{
    [Fact]
    public void BytesAsDefaultMessageBodyType()
    {
        var converter = new SimpleMessageConverter();
        var message = Message.Create(Encoding.UTF8.GetBytes("test"), new MessageHeaders());
        var result = converter.FromMessage<byte[]>(message);
        Assert.Equal("test", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void MessageToString()
    {
        var converter = new SimpleMessageConverter();
        var message = Message.Create(Encoding.UTF8.GetBytes("test"), new MessageHeaders());
        var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        accessor.ContentType = MessageHeaders.CONTENT_TYPE_TEXT_PLAIN;
        var result = converter.FromMessage<string>(message);
        Assert.Equal("test", result);
    }

    [Fact]
    public void MessageToBytes()
    {
        var converter = new SimpleMessageConverter();
        var message = Message.Create(new byte[] { 1, 2, 3 }, new MessageHeaders());
        var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        accessor.ContentType = MessageHeaders.CONTENT_TYPE_BYTES;
        var result = converter.FromMessage<byte[]>(message);
        Assert.Equal(3, result.Length);
        Assert.Equal(1, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(3, result[2]);
    }

    [Fact]
    public void StringToMessage()
    {
        var converter = new SimpleMessageConverter();
        var message = converter.ToMessage("test", new MessageHeaders());
        var contentType = message.Headers.ContentType();
        var contentEncoding = message.Headers.ContentEncoding();
        var encoding = Encoding.GetEncoding(contentEncoding);
        var content = encoding.GetString((byte[])message.Payload);
        Assert.Equal("text/plain", contentType);
        Assert.Equal("test", content);
    }

    [Fact]
    public void BytesToMessage()
    {
        var converter = new SimpleMessageConverter();
        var message = converter.ToMessage(new byte[] { 1, 2, 3 }, new MessageHeaders());
        var contentType = message.Headers.ContentType();
        var body = message.Payload as byte[];
        Assert.Equal(MessageHeaders.CONTENT_TYPE_BYTES, contentType);
        Assert.Equal(3, body.Length);
        Assert.Equal(1, body[0]);
        Assert.Equal(2, body[1]);
        Assert.Equal(3, body[2]);
    }
}