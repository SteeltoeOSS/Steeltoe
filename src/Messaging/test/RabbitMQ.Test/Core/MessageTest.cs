// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Support;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class MessageTest
{
    [Fact]
    public void ToStringForEmptyMessageBody()
    {
        IMessage<byte[]> message = Message.Create(Array.Empty<byte>(), new MessageHeaders());
        Assert.NotNull(message.ToString());
    }

    [Fact(Skip = "Standard ContentType header missing")]
    public void ProperEncoding()
    {
        IMessage<byte[]> message = Message.Create(EncodingUtils.Utf16.GetBytes("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP"), new MessageHeaders());
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        accessor.ContentType = MessageHeaders.ContentTypeJson;
        accessor.ContentEncoding = "UTF-16";
        Assert.Contains("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP", message.ToString());
    }

    [Fact]
    public void ToStringForNullMessageProperties()
    {
        IMessage<byte[]> message = Message.Create(Array.Empty<byte>(), null);
        Assert.NotNull(message.ToString());
    }

    [Fact]
    public void ToStringForNonStringMessageBody()
    {
        IMessage<DateTime> message = Message.Create(default(DateTime), null);
        Assert.NotNull(message.ToString());
    }
}
