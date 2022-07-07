// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using System;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class ErrorHandlerTests
{
    [Fact]
    public void TestFatalsAreRejected()
    {
        var handler = new ConditionalRejectingErrorHandler();
        handler.HandleError(new ListenerExecutionFailedException("intended", new InvalidOperationException(), Message.Create(Encoding.UTF8.GetBytes(string.Empty))));
        Assert.Throws<RabbitRejectAndDontRequeueException>(
            () => handler.HandleError(new ListenerExecutionFailedException("intended", new MessageConversionException(string.Empty), Message.Create(Encoding.UTF8.GetBytes(string.Empty)))));
        var message = Message.Create(Encoding.UTF8.GetBytes(string.Empty));

        var parmInfo = new Moq.Mock<ParameterInfo>();
        parmInfo.Setup(p => p.Position).Returns(1);
        parmInfo.Setup(p => p.Member.ToString()).Returns("testMember");

        Assert.Throws<RabbitRejectAndDontRequeueException>(
            () => handler.HandleError(new ListenerExecutionFailedException("intended", new MethodArgumentTypeMismatchException(message, parmInfo.Object, string.Empty), message)));

        Assert.Throws<RabbitRejectAndDontRequeueException>(
            () => handler.HandleError(new ListenerExecutionFailedException("intended", new MethodArgumentNotValidException(message, parmInfo.Object, string.Empty), message)));
    }

    [Fact]
    public void TestSimple()
    {
        var cause = new InvalidCastException();
        Assert.Throws<RabbitRejectAndDontRequeueException>(() => DoTest(cause));
    }

    [Fact]
    public void TestMessagingException()
    {
        var cause = new MessageHandlingException(null, "test", new MessageHandlingException(null, "test", new InvalidCastException()));
        Assert.Throws<RabbitRejectAndDontRequeueException>(() => DoTest(cause));
    }

    private void DoTest(Exception cause)
    {
        var handler = new ConditionalRejectingErrorHandler();
        handler.HandleError(new ListenerExecutionFailedException("test", cause, Message.Create(Array.Empty<byte>())));
    }
}
