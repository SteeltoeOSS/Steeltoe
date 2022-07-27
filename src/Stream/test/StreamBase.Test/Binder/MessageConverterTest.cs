// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class MessageConverterTest
{
    [Fact]
    public void TestHeaderEmbedding()
    {
        var message = IntegrationMessageBuilder<byte[]>
            .WithPayload(Encoding.UTF8.GetBytes("Hello"))
            .SetHeader("foo", "bar")
            .SetHeader("baz", "quxx")
            .Build();
        var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        var embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));
        var extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
        var extractedString = Encoding.UTF8.GetString((byte[])extracted.Payload);
        Assert.Equal("Hello", extractedString);
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("quxx", extracted["baz"]);
    }

    [Fact]
    public void TestConfigurableHeaders()
    {
        var message = IntegrationMessageBuilder<byte[]>
            .WithPayload(Encoding.UTF8.GetBytes("Hello"))
            .SetHeader("foo", "bar")
            .SetHeader("baz", "quxx")
            .SetHeader("contentType", "text/plain")
            .Build();

        var headers = new string[] { "foo" };
        var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), EmbeddedHeaderUtils.HeadersToEmbed(headers));
        Assert.Equal(0xff, embedded[0]);
        var embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u000BcontentType\u0000\u0000\u0000\u000C\"text/plain\"\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
        var extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);

        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Null(extracted["baz"]);
        Assert.Equal("text/plain", extracted["contentType"]);
        Assert.Null(extracted["timestamp"]);

        var extractedWithRequestHeaders = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), true);
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
        var message = IntegrationMessageBuilder<byte[]>
            .WithPayload(Encoding.UTF8.GetBytes("Hello"))
            .SetHeader("foo", "bar")
            .SetHeader("baz", "quxx")
            .Build();
        var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        var embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));

        var extracted = EmbeddedHeaderUtils.ExtractHeaders(embedded);
        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("quxx", extracted["baz"]);
    }

    [Fact]
    public void TestUnicodeHeader()
    {
        var message = IntegrationMessageBuilder<byte[]>
            .WithPayload(Encoding.UTF8.GetBytes("Hello"))
            .SetHeader("foo", "bar")
            .SetHeader("baz", "ØØØØØØØØ")
            .Build();

        var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        var embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0012\"ØØØØØØØØ\"Hello", embeddedString.Substring(1));

        var extracted = EmbeddedHeaderUtils.ExtractHeaders(IntegrationMessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
        Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
        Assert.Equal("bar", extracted["foo"]);
        Assert.Equal("ØØØØØØØØ", extracted["baz"]);
    }

    [Fact]
    public void TestHeaderEmbeddingMissingHeader()
    {
        var message = IntegrationMessageBuilder<byte[]>
            .WithPayload(Encoding.UTF8.GetBytes("Hello"))
            .SetHeader("foo", "bar")
            .Build();
        var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
        Assert.Equal(0xff, embedded[0]);
        var embeddedString = Encoding.UTF8.GetString(embedded);
        Assert.Equal("\u0001\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
    }

    [Fact]
    public void TestBadDecode()
    {
        var bytes = new byte[] { (byte)0xff, 99 };
        var message = Steeltoe.Messaging.Message.Create<byte[]>(bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => EmbeddedHeaderUtils.ExtractHeaders(message, false));
    }
}