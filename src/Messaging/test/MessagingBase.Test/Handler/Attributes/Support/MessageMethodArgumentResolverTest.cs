// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Reflection;
using Xunit;

namespace Steeltoe.Messaging.Handler.Attributes.Support.Test;

public class MessageMethodArgumentResolverTest
{
    private readonly IMessageConverter _converter;

    private readonly MethodInfo _method;

    private readonly Mock<IMessageConverter> _mock;

    private MessageMethodArgumentResolver _resolver;

    public MessageMethodArgumentResolverTest()
    {
        _method = typeof(MessageMethodArgumentResolverTest)
            .GetMethod(
                nameof(Handle),
                BindingFlags.NonPublic | BindingFlags.Instance);

        _mock = new Mock<IMessageConverter>();
        _converter = _mock.Object;
        _resolver = new MessageMethodArgumentResolver(_converter);
    }

    [Fact]
    public void ResolveWithPayloadTypeAsObject()
    {
        var message = MessageBuilder.WithPayload("test").Build();
        var parameter = _method.GetParameters()[0];

        Assert.True(_resolver.SupportsParameter(parameter));
        Assert.Same(message, _resolver.ResolveArgument(parameter, message));
    }

    [Fact]
    public void ResolveWithMatchingPayloadType()
    {
        var message = MessageBuilder.WithPayload(123).Build();
        var parameter = _method.GetParameters()[1];

        Assert.True(_resolver.SupportsParameter(parameter));
        Assert.Same(message, _resolver.ResolveArgument(parameter, message));
    }

    [Fact]
    public void ResolveWithConversion()
    {
        var message = MessageBuilder.WithPayload("test").Build();
        var parameter = _method.GetParameters()[1];
        _mock.Setup(c => c.FromMessage(message, typeof(int))).Returns(4);

        var actual = (IMessage)_resolver.ResolveArgument(parameter, message);

        Assert.NotNull(actual);
        Assert.Equal(message.Headers, actual.Headers);
        Assert.Equal(4, actual.Payload);
    }

    [Fact]
    public void ResolveWithConversionNoMatchingConverter()
    {
        var message = MessageBuilder.WithPayload("test").Build();
        var parameter = _method.GetParameters()[1];

        Assert.True(_resolver.SupportsParameter(parameter));
        var ex = Assert.Throws<MessageConversionException>(() => _resolver.ResolveArgument(parameter, message));
        Assert.Contains("Int32", ex.Message);
        Assert.Contains("String", ex.Message);
    }

    [Fact]
    public void ResolveWithConversionEmptyPayload()
    {
        var message = MessageBuilder.WithPayload(string.Empty).Build();
        var parameter = _method.GetParameters()[1];
        Assert.True(_resolver.SupportsParameter(parameter));
        var ex = Assert.Throws<MessageConversionException>(() => _resolver.ResolveArgument(parameter, message));
        Assert.Contains("payload is empty", ex.Message);
        Assert.Contains("Int32", ex.Message);
        Assert.Contains("String", ex.Message);
    }

    // [Fact]
    // public void ResolveWithPayloadTypeUpperBound()
    // {
    //    IMessage<int> message = MessageBuilder<int>.WithPayload(123).Build();
    //    ParameterInfo parameter = this.method.GetParameters()[3];

    // Assert.True(this.resolver.SupportsParameter(parameter));
    //    Assert.Same(message, this.resolver.ResolveArgument(parameter, message));
    // }

    // [Fact]
    // public void ResolveWithPayloadTypeOutOfBound()
    // {
    //    IMessage<CultureInfo> message = MessageBuilder<CultureInfo>.WithPayload(CultureInfo.CurrentCulture).Build();
    //    ParameterInfo parameter = this.method.GetParameters()[3];

    // Assert.True(this.resolver.SupportsParameter(parameter));
    //    var ex = Assert.Throws<MessageConversionException>(() => this.resolver.ResolveArgument(parameter, message));
    //    Assert.Contains("Number", ex.Message);
    //    Assert.Contains("CultureInfo", ex.Message);
    // }
    [Fact]
    public void ResolveMessageSubclassMatch()
    {
        var message = new ErrorMessage(new InvalidOperationException());
        var parameter = _method.GetParameters()[4];

        Assert.True(_resolver.SupportsParameter(parameter));
        Assert.Same(message, _resolver.ResolveArgument(parameter, message));
    }

    [Fact]
    public void ResolveWithMessageSubclassAndPayloadWildcard()
    {
        var message = new ErrorMessage(new InvalidOperationException());
        var parameter = _method.GetParameters()[0];
        Assert.True(_resolver.SupportsParameter(parameter));
        Assert.Same(message, _resolver.ResolveArgument(parameter, message));
    }

    // [Fact]
    // public void ResolveWithWrongMessageType()
    // {
    //    var ex1 = new InvalidOperationException();
    //    IMessage<Exception> message = GenericMessage.Create<Exception>(ex1);
    //    ParameterInfo parameter = this.method.GetParameters()[0];

    // Assert.True(this.resolver.SupportsParameter(parameter));
    //    var ex = Assert.Throws<MethodArgumentTypeMismatchException>(() => this.resolver.ResolveArgument(parameter, message));
    //    Assert.Contains("ErrorMessage", ex.Message);
    //    Assert.Contains("GenericMessage", ex.Message);
    // }
    [Fact]
    public void ResolveWithPayloadTypeAsWildcardAndNoConverter()
    {
        _resolver = new MessageMethodArgumentResolver();

        var message = MessageBuilder.WithPayload("test").Build();
        var parameter = _method.GetParameters()[0];
        Assert.True(_resolver.SupportsParameter(parameter));
        Assert.Same(message, _resolver.ResolveArgument(parameter, message));
    }

    [Fact]
    public void ResolveWithConversionNeededButNoConverter()
    {
        _resolver = new MessageMethodArgumentResolver();

        var message = MessageBuilder.WithPayload("test").Build();
        var parameter = _method.GetParameters()[1];
        Assert.True(_resolver.SupportsParameter(parameter));
        var ex = Assert.Throws<MessageConversionException>(() => _resolver.ResolveArgument(parameter, message));
        Assert.Contains("Int32", ex.Message);
        Assert.Contains("String", ex.Message);
    }

    [Fact]
    public void ResolveWithConversionEmptyPayloadButNoConverter()
    {
        _resolver = new MessageMethodArgumentResolver();

        var message = MessageBuilder.WithPayload(string.Empty).Build();
        var parameter = _method.GetParameters()[1];
        Assert.True(_resolver.SupportsParameter(parameter));
        var ex = Assert.Throws<MessageConversionException>(() => _resolver.ResolveArgument(parameter, message));
        Assert.Contains("payload is empty", ex.Message);
        Assert.Contains("Int32", ex.Message);
        Assert.Contains("String", ex.Message);
    }

    [Fact]
    public void ResolveWithNewtonJSonConverter()
    {
        var inMessage = MessageBuilder.WithPayload("{\"prop\":\"bar\"}").Build();
        var parameter = _method.GetParameters()[5];

        _resolver = new MessageMethodArgumentResolver(new NewtonJsonMessageConverter());
        var actual = _resolver.ResolveArgument(parameter, inMessage);

        var condition1 = actual is IMessage;
        Assert.True(condition1);
        var outMessage = (IMessage)actual;
        var condition = outMessage.Payload is Foo;
        Assert.True(condition);
        Assert.Equal("bar", ((Foo)outMessage.Payload).Prop);
    }

    private void Handle(
        IMessage wildcardPayload,
        IMessage<int> integerPayload,
        IMessage<long> numberPayload,
        IMessage<string> anyNumberPayload,
        ErrorMessage subClass,
        IMessage<Foo> fooPayload)
    {
    }

    internal sealed class Foo
    {
        public string Prop { get; set; }
    }
}
