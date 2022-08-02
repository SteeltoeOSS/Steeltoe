// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Handler.Attributes.Test;
using Steeltoe.Messaging.Handler.Invocation.Test;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class HeaderMethodArgumentResolverTest
{
    private readonly HeaderMethodArgumentResolver _resolver = new(new DefaultConversionService());

    private readonly ResolvableMethod _resolvable = ResolvableMethod.On<HeaderMethodArgumentResolverTest>().Named(nameof(HandleMessage)).Build();

    [Fact]
    public void SupportsParameter()
    {
        Assert.True(_resolver.SupportsParameter(_resolvable.Annotation(MessagingPredicates.HeaderPlain()).Arg()));
        Assert.False(_resolver.SupportsParameter(_resolvable.AnnotationNotPresent(typeof(HeaderAttribute)).Arg()));
    }

    [Fact]
    public void ResolveArgument()
    {
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeader("param1", "foo").Build();
        object result = _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.HeaderPlain()).Arg(), message);
        Assert.Equal("foo", result);
    }

    [Fact]
    public void ResolveArgumentNativeHeader()
    {
        var headers = new TestMessageHeaderAccessor();
        headers.SetNativeHeader("param1", "foo");
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeaders(headers).Build();
        Assert.Equal("foo", _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.HeaderPlain()).Arg(), message));
    }

    [Fact]
    public void ResolveArgumentNativeHeaderAmbiguity()
    {
        var headers = new TestMessageHeaderAccessor();
        headers.SetHeader("param1", "foo");
        headers.SetNativeHeader("param1", "native-foo");
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeaders(headers).Build();

        Assert.Equal("foo", _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.HeaderPlain()).Arg(), message));
        Assert.Equal("native-foo", _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.Header("nativeHeaders.param1")).Arg(), message));
    }

    [Fact]
    public void ResolveArgumentNotFound()
    {
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();
        Assert.Throws<MessageHandlingException>(() => _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.HeaderPlain()).Arg(), message));
    }

    [Fact]
    public void ResolveArgumentDefaultValue()
    {
        IMessage message = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();
        object result = _resolver.ResolveArgument(_resolvable.Annotation(MessagingPredicates.Header("name", "bar")).Arg(), message);
        Assert.Equal("bar", result);
    }

    [Fact]
    public void ResolveOptionalHeaderWithValue()
    {
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("foo", "bar").Build();
        ParameterInfo param = _resolvable.Annotation(MessagingPredicates.Header("foo")).Arg();
        object result = _resolver.ResolveArgument(param, message);
        Assert.Equal("bar", result);
    }

    [Fact]
    public void ResolveOptionalHeaderAsEmpty()
    {
        IMessage message = MessageBuilder.WithPayload("foo").Build();
        ParameterInfo param = _resolvable.Annotation(MessagingPredicates.Header("foo")).Arg();
        object result = _resolver.ResolveArgument(param, message);
        Assert.Null(result);
    }

    private void HandleMessage([Header] string param1, [Header(Name = "name", DefaultValue = "bar")] string param2,
        [Header(Name = "name", DefaultValue = "#{systemProperties.systemProperty}")] string param3,
        [Header(Name = "#{systemProperties.systemProperty}")] string param4, string param5, [Header("nativeHeaders.param1")] string nativeHeaderParam1,
        [Header("foo")] string param6 = null)
    {
    }

    internal sealed class TestMessageHeaderAccessor : NativeMessageHeaderAccessor
    {
        public TestMessageHeaderAccessor()
            : base((IDictionary<string, List<string>>)null)
        {
        }
    }
}
