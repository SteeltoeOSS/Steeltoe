// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Text;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class PayloadMethodArgumentResolverTest
{
    private readonly PayloadMethodArgumentResolver _resolver;

    private readonly ParameterInfo _paramAnnotated;

    private readonly ParameterInfo _paramAnnotatedNotRequired;

    private readonly ParameterInfo _paramAnnotatedRequired;

    private readonly ParameterInfo _paramWithSpelExpression;

    private readonly ParameterInfo _paramNotAnnotated;

    public PayloadMethodArgumentResolverTest()
    {
        _resolver = new PayloadMethodArgumentResolver(new StringMessageConverter());

        MethodInfo payloadMethod = typeof(PayloadMethodArgumentResolverTest).GetMethod(nameof(HandleMessage), BindingFlags.NonPublic | BindingFlags.Instance);

        _paramAnnotated = payloadMethod.GetParameters()[0];
        _paramAnnotatedNotRequired = payloadMethod.GetParameters()[1];
        _paramAnnotatedRequired = payloadMethod.GetParameters()[2];
        _paramWithSpelExpression = payloadMethod.GetParameters()[3];
        _paramNotAnnotated = payloadMethod.GetParameters()[4];
    }

    [Fact]
    public void SupportsParameter()
    {
        Assert.True(_resolver.SupportsParameter(_paramAnnotated));
        Assert.True(_resolver.SupportsParameter(_paramNotAnnotated));

        var strictResolver = new PayloadMethodArgumentResolver(new StringMessageConverter(), false);

        Assert.True(strictResolver.SupportsParameter(_paramAnnotated));
        Assert.False(strictResolver.SupportsParameter(_paramNotAnnotated));
    }

    [Fact]
    public void ResolveRequired()
    {
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        object actual = _resolver.ResolveArgument(_paramAnnotated, message);
        Assert.Equal("ABC", actual);
    }

    [Fact]
    public void ResolveRequiredEmpty()
    {
        IMessage message = MessageBuilder.WithPayload(string.Empty).Build();
        Assert.Throws<MethodArgumentNotValidException>(() => _resolver.ResolveArgument(_paramAnnotated, message));
    }

    [Fact]
    public void ResolveRequiredEmptyNonAnnotatedParameter()
    {
        IMessage message = MessageBuilder.WithPayload(string.Empty).Build();
        Assert.Throws<MethodArgumentNotValidException>(() => _resolver.ResolveArgument(_paramNotAnnotated, message));
    }

    [Fact]
    public void ResolveNotRequired()
    {
        IMessage emptyByteArrayMessage = MessageBuilder.WithPayload(Array.Empty<byte>()).Build();
        Assert.Null(_resolver.ResolveArgument(_paramAnnotatedNotRequired, emptyByteArrayMessage));

        IMessage emptyStringMessage = MessageBuilder.WithPayload(string.Empty).Build();
        Assert.Null(_resolver.ResolveArgument(_paramAnnotatedNotRequired, emptyStringMessage));

        IMessage notEmptyMessage = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        Assert.Equal("ABC", _resolver.ResolveArgument(_paramAnnotatedNotRequired, notEmptyMessage));
    }

    [Fact]
    public void ResolveNonConvertibleParam()
    {
        IMessage notEmptyMessage = MessageBuilder.WithPayload(123).Build();
        var ex = Assert.Throws<MessageConversionException>(() => _resolver.ResolveArgument(_paramAnnotatedRequired, notEmptyMessage));
        Assert.Contains("Cannot convert", ex.Message);
    }

    [Fact]
    public void ResolveSpelExpressionNotSupported()
    {
        IMessage message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        Assert.Throws<InvalidOperationException>(() => _resolver.ResolveArgument(_paramWithSpelExpression, message));
    }

    [Fact]
    public void ResolveNonAnnotatedParameter()
    {
        IMessage notEmptyMessage = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
        Assert.Equal("ABC", _resolver.ResolveArgument(_paramNotAnnotated, notEmptyMessage));
    }

    internal void HandleMessage([Payload] string param, [Payload(Required = false)] string paramNotRequired,
        [Payload(Required = true)] CultureInfo nonConvertibleRequiredParam, [Payload("foo.bar")] string paramWithExpression, string paramNotAnnotated)
    {
    }
}
