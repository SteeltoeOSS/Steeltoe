// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class MessageConverterTest
{
    [Fact]
    public void TestHeaderEmbedding()
    {
        IMessage<byte[]> message = IntegrationMessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes("Hello")).SetHeader("foo", "bar")
            .SetHeader("baz", "quxx").Build();

        byte[] embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        string embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));
        MessageValues extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
        string extractedString = Encoding.UTF8.GetString((byte[])extracted.Payload);
        Assert.Equal("Hello", extractedString);
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("quxx", extracted["baz"]);
    }

    [Fact]
    public void TestConfigurableHeaders()
    {
        IMessage<byte[]> message = IntegrationMessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes("Hello")).SetHeader("foo", "bar")
            .SetHeader("baz", "quxx").SetHeader("contentType", "text/plain").Build();

        string[] headers =
        {
            "foo"
        };

        byte[] embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), EmbeddedHeaderUtils.HeadersToEmbed(headers));
        Assert.Equal(0xff, embedded[0]);
        string embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u000BcontentType\u0000\u0000\u0000\u000C\"text/plain\"\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
        MessageValues extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);

        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Null(extracted["baz"]);
        Assert.Equal("text/plain", extracted["contentType"]);
        Assert.Null(extracted["timestamp"]);

        MessageValues extractedWithRequestHeaders = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), true);
        Assert.Equal("bar", extractedWithRequestHeaders["foo"]);
        Assert.Null(extractedWithRequestHeaders["baz"]);
        Assert.Equal("text/plain", extractedWithRequestHeaders["contentType"]);
        Assert.NotNull(extractedWithRequestHeaders["timestamp"]);
        Assert.NotNull(extractedWithRequestHeaders["id"]);
        Assert.IsType<long>(extractedWithRequestHeaders["timestamp"]);
        Assert.IsType<string>(extractedWithRequestHeaders["id"]);
    }

    [Fact]
    public void TestHeaderExtractionWithDirectPayload()
    {
        IMessage<byte[]> message = IntegrationMessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes("Hello")).SetHeader("foo", "bar")
            .SetHeader("baz", "quxx").Build();

        byte[] embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        string embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));

        MessageValues extracted = EmbeddedHeaderUtils.ExtractHeaders(embedded);
        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("quxx", extracted["baz"]);
    }

    [Fact]
    public void TestUnicodeHeader()
    {
        IMessage<byte[]> message = IntegrationMessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes("Hello")).SetHeader("foo", "bar")
            .SetHeader("baz", "ØØØØØØØØ").Build();

        byte[] embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        string embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0012\"ØØØØØØØØ\"Hello", embeddedString.Substring(1));

        MessageValues extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("ØØØØØØØØ", extracted["baz"]);
    }

    [Fact]
    public void TestHeaderEmbeddingMissingHeader()
    {
        IMessage<byte[]> message = IntegrationMessageBuilder<byte[]>.WithPayload(Encoding.UTF8.GetBytes("Hello")).SetHeader("foo", "bar").Build();
        byte[] embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        string embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0001\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
    }

    [Fact]
    public void TestBadDecode()
    {
        byte[] bytes =
        {
            0xff,
            99
        };

        IMessage<byte[]> message = Message.Create(bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => EmbeddedHeaderUtils.ExtractHeaders(message, false));
    }
}
