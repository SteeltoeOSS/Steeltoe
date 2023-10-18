// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Test.Support;

public sealed class MessageBuilderTest
{
    [Fact]
    public void TestReadOnlyHeaders()
    {
        var factory = new DefaultMessageBuilderFactory();
        IMessage<string> message = factory.WithPayload("bar").SetHeader("foo", "baz").SetHeader("qux", "fiz").Build();
        Assert.Equal("baz", message.Headers.Get<string>("foo"));
        Assert.Equal("fiz", message.Headers.Get<string>("qux"));

        factory.ReadOnlyHeaders = new List<string>
        {
            "foo"
        };

        message = factory.FromMessage(message).Build();
        Assert.Null(message.Headers.Get<string>("foo"));
        Assert.Equal("fiz", message.Headers.Get<string>("qux"));
        factory.AddReadOnlyHeaders("qux");
        message = factory.FromMessage(message).Build();
        Assert.Null(message.Headers.Get<string>("foo"));
        Assert.Null(message.Headers.Get<string>("qux"));
    }
}
