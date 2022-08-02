// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test;

public class SimpleMessageConverterTest
{
    [Fact]
    public void ToMessageWithPayloadAndHeaders()
    {
        var headers = new MessageHeaders(new Dictionary<string, object>
        {
            { "foo", "bar" }
        });

        var converter = new SimpleMessageConverter();
        IMessage message = converter.ToMessage("payload", headers);

        Assert.Equal("payload", message.Payload);
        Assert.Equal("bar", message.Headers["foo"]);
    }

    [Fact]
    public void ToMessageWithPayloadAndMutableHeaders()
    {
        var accessor = new MessageHeaderAccessor();
        accessor.SetHeader("foo", "bar");
        accessor.LeaveMutable = true;
        IMessageHeaders headers = accessor.MessageHeaders;

        var converter = new SimpleMessageConverter();
        IMessage message = converter.ToMessage("payload", headers);

        Assert.Equal("payload", message.Payload);
        Assert.Same(headers, message.Headers);
        Assert.Equal("bar", message.Headers["foo"]);
    }
}
