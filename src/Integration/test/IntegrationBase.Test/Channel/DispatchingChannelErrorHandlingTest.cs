// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class DispatchingChannelErrorHandlingTest
{
    private readonly CountdownEvent _latch = new (1);

    private readonly IServiceCollection _services;

    public DispatchingChannelErrorHandlingTest()
    {
        _services = new ServiceCollection();
        _services.AddSingleton<IIntegrationServices, IntegrationServices>();
        var config = new ConfigurationBuilder().Build();
        _services.AddSingleton<IConfiguration>(config);
        _services.AddSingleton<IApplicationContext, GenericApplicationContext>();
    }

    [Fact]
    public void HandlerThrowsExceptionPublishSubscribeWithoutScheduler()
    {
        var provider = _services.BuildServiceProvider();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        var handler = new ThrowingHandler();
        channel.Subscribe(handler);
        var message = IntegrationMessageBuilder.WithPayload("test").Build();
        Assert.Throws<MessageDeliveryException>(() => channel.Send(message));
    }

    [Fact]
    public void HandlerThrowsExceptionPublishSubscribeWithExecutor()
    {
        _services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        _services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _services.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "errorChannel"));
        var provider = _services.BuildServiceProvider();

        var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var resultHandler = new ResultHandler(_latch);
        var throwingHandler = new ThrowMessageExceptionHandler();
        channel.Subscribe(throwingHandler);
        defaultErrorChannel.Subscribe(resultHandler);
        var message = IntegrationMessageBuilder.WithPayload("test").Build();
        channel.Send(message);
        Assert.True(_latch.Wait(10000));
        var errorMessage = resultHandler.LastMessage;
        Assert.IsType<MessagingException>(errorMessage.Payload);
        var exceptionPayload = (MessagingException)errorMessage.Payload;
        Assert.IsType<NotSupportedException>(exceptionPayload.InnerException);
        Assert.Same(message, exceptionPayload.FailedMessage);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, resultHandler.LastThread.ManagedThreadId);
    }

    [Fact]
    public void HandlerThrowsExceptionExecutorChannel()
    {
        _services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        _services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _services.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "errorChannel"));
        var provider = _services.BuildServiceProvider();

        var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
        var channel = new TaskSchedulerChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var resultHandler = new ResultHandler(_latch);
        var throwingHandler = new ThrowMessageExceptionHandler();
        channel.Subscribe(throwingHandler);
        defaultErrorChannel.Subscribe(resultHandler);
        var message = IntegrationMessageBuilder.WithPayload("test").Build();
        channel.Send(message);
        Assert.True(_latch.Wait(10000));
        var errorMessage = resultHandler.LastMessage;
        Assert.IsType<MessagingException>(errorMessage.Payload);
        var exceptionPayload = (MessagingException)errorMessage.Payload;
        Assert.IsType<NotSupportedException>(exceptionPayload.InnerException);
        Assert.Same(message, exceptionPayload.FailedMessage);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, resultHandler.LastThread.ManagedThreadId);
    }

    private sealed class ThrowMessageExceptionHandler : IMessageHandler
    {
        public string ServiceName { get; set; } = nameof(ThrowMessageExceptionHandler);

        public Exception ExceptionToThrow = new NotSupportedException("intentional test failure");

        public void HandleMessage(IMessage message)
        {
            throw new MessagingException(message, ExceptionToThrow);
        }
    }

    private sealed class ThrowingHandler : IMessageHandler
    {
        public string ServiceName { get; set; } = nameof(ThrowingHandler);

        public Exception ExceptionToThrow = new NotSupportedException("intentional test failure");

        public void HandleMessage(IMessage message)
        {
            throw ExceptionToThrow;
        }
    }

    private sealed class ResultHandler : IMessageHandler
    {
        private readonly CountdownEvent _latch;

        public ResultHandler(CountdownEvent latch)
        {
            _latch = latch;
        }

        public string ServiceName { get; set; } = nameof(ResultHandler);

        public volatile IMessage LastMessage;

        public volatile Thread LastThread;

        public void HandleMessage(IMessage message)
        {
            LastMessage = message;
            LastThread = Thread.CurrentThread;
            _latch.Signal();
        }
    }

    public class TestTimedOutException : Exception
    {
        public TestTimedOutException()
            : base("timed out while waiting for latch")
        {
        }
    }
}
