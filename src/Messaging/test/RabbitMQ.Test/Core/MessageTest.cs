// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class MessageTest
{
    [Fact]
    public void ToStringForEmptyMessageBody()
    {
        var message = Message.Create(Array.Empty<byte>(), new MessageHeaders());
        Assert.NotNull(message.ToString());
    }

    [Fact(Skip = "Standard ContentType header missing")]
    public void ProperEncoding()
    {
        var message = Message.Create(EncodingUtils.Utf16.GetBytes("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP"), new MessageHeaders());
        var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        accessor.ContentType = MessageHeaders.ContentTypeJson;
        accessor.ContentEncoding = "UTF-16";
        Assert.Contains("ÁRVÍZTŰRŐ TÜKÖRFÚRÓGÉP", message.ToString());
    }

    [Fact]
    public void ToStringForNullMessageProperties()
    {
        var message = Message.Create(Array.Empty<byte>(), null);
        Assert.NotNull(message.ToString());
    }

    [Fact]
    public void ToStringForNonStringMessageBody()
    {
        var message = Message.Create(default(DateTime), null);
        Assert.NotNull(message.ToString());
    }
}
