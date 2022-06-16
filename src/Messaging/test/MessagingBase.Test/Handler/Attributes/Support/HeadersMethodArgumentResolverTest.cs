// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Invocation.Test;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class HeadersMethodArgumentResolverTest
{
    private readonly HeadersMethodArgumentResolver _resolver = new ();

    private readonly IMessage _message = MessageBuilder.WithPayload(Array.Empty<byte>())
        .CopyHeaders(new Dictionary<string, object> { { "foo", "bar" } })
        .Build();

    private readonly ResolvableMethod _resolvable = ResolvableMethod.On<HeadersMethodArgumentResolverTest>().Named(nameof(HandleMessage)).Build();

    [Fact]
    public void SupportsParameter()
    {
        Assert.True(_resolver.SupportsParameter(_resolvable.AnnotPresent(typeof(HeadersAttribute)).Arg(typeof(IDictionary<string, object>))));

        Assert.True(_resolver.SupportsParameter(_resolvable.Arg(typeof(MessageHeaders))));
        Assert.True(_resolver.SupportsParameter(_resolvable.Arg(typeof(MessageHeaderAccessor))));
        Assert.True(_resolver.SupportsParameter(_resolvable.Arg(typeof(TestMessageHeaderAccessor))));

        Assert.False(_resolver.SupportsParameter(_resolvable.AnnotPresent(typeof(HeadersAttribute)).Arg(typeof(string))));
    }

    [Fact]
    public void ResolveArgumentAnnotated()
    {
        var param = _resolvable.AnnotPresent(typeof(HeadersAttribute)).Arg(typeof(IDictionary<string, object>));
        var resolved = _resolver.ResolveArgument(param, _message);

        var condition = resolved is IDictionary<string, object>;
        Assert.True(condition);

        var headers = resolved as IDictionary<string, object>;
        Assert.Equal("bar", headers["foo"]);
    }

    [Fact]
    public void ResolveArgumentAnnotatedNotMap()
    {
        Assert.Throws<InvalidOperationException>(() => _resolver.ResolveArgument(_resolvable.AnnotPresent(typeof(HeadersAttribute)).Arg(typeof(string)), _message));
    }

    [Fact]
    public void ResolveArgumentMessageHeaders()
    {
        var resolved = _resolver.ResolveArgument(_resolvable.Arg(typeof(MessageHeaders)), _message);

        var condition = resolved is MessageHeaders;
        Assert.True(condition);
        var headers = (MessageHeaders)resolved;
        Assert.Equal("bar", headers["foo"]);
    }

    [Fact]
    public void ResolveArgumentMessageHeaderAccessor()
    {
        var param = _resolvable.Arg(typeof(MessageHeaderAccessor));
        var resolved = _resolver.ResolveArgument(param, _message);

        var condition = resolved is MessageHeaderAccessor;
        Assert.True(condition);
        var headers = (MessageHeaderAccessor)resolved;
        Assert.Equal("bar", headers.GetHeader("foo"));
    }

    [Fact]
    public void ResolveArgumentMessageHeaderAccessorSubclass()
    {
        var param = _resolvable.Arg(typeof(TestMessageHeaderAccessor));
        var resolved = _resolver.ResolveArgument(param, _message);

        var condition = resolved is TestMessageHeaderAccessor;
        Assert.True(condition);
        var headers = (TestMessageHeaderAccessor)resolved;
        Assert.Equal("bar", headers.GetHeader("foo"));
    }

    private void HandleMessage(
        [Headers] IDictionary<string, object> param1,
        [Headers] string param2,
        MessageHeaders param3,
        MessageHeaderAccessor param4,
        TestMessageHeaderAccessor param5)
    {
    }

    internal sealed class TestMessageHeaderAccessor : NativeMessageHeaderAccessor
    {
        public TestMessageHeaderAccessor(IMessage message)
            : base(message)
        {
        }

        public static TestMessageHeaderAccessor Wrap(IMessage message)
        {
            return new TestMessageHeaderAccessor(message);
        }
    }
}
