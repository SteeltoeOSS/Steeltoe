// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Xunit;

namespace Steeltoe.Integration.Test.Channel;

public class DispatchingChannelErrorHandlingTest
{
    private readonly CountdownEvent _latch = new(1);

    private readonly IServiceCollection _services;

    public DispatchingChannelErrorHandlingTest()
    {
        _services = new ServiceCollection();
        _services.AddSingleton<IIntegrationServices, IntegrationServices>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        _services.AddSingleton<IConfiguration>(configurationRoot);
        _services.AddSingleton<IApplicationContext, GenericApplicationContext>();
    }

    [Fact]
    public void HandlerThrowsExceptionPublishSubscribeWithoutScheduler()
    {
        ServiceProvider provider = _services.BuildServiceProvider();
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
        var handler = new ThrowingHandler();
        channel.Subscribe(handler);
        IMessage message = IntegrationMessageBuilder.WithPayload("test").Build();
        Assert.Throws<MessageDeliveryException>(() => channel.Send(message));
    }

    [Fact]
    public void HandlerThrowsExceptionPublishSubscribeWithExecutor()
    {
        _services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        _services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _services.AddSingleton<IMessageChannel>(p => new DirectChannel(p.GetService<IApplicationContext>(), "errorChannel"));
        ServiceProvider provider = _services.BuildServiceProvider();

        var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
        var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var resultHandler = new ResultHandler(_latch);
        var throwingHandler = new ThrowMessageExceptionHandler();
        channel.Subscribe(throwingHandler);
        defaultErrorChannel.Subscribe(resultHandler);
        IMessage message = IntegrationMessageBuilder.WithPayload("test").Build();
        channel.Send(message);
        Assert.True(_latch.Wait(10000));
        IMessage errorMessage = resultHandler.LastMessage;
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
        ServiceProvider provider = _services.BuildServiceProvider();

        var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
        var channel = new TaskSchedulerChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var resultHandler = new ResultHandler(_latch);
        var throwingHandler = new ThrowMessageExceptionHandler();
        channel.Subscribe(throwingHandler);
        defaultErrorChannel.Subscribe(resultHandler);
        IMessage message = IntegrationMessageBuilder.WithPayload("test").Build();
        channel.Send(message);
        Assert.True(_latch.Wait(10000));
        IMessage errorMessage = resultHandler.LastMessage;
        Assert.IsType<MessagingException>(errorMessage.Payload);
        var exceptionPayload = (MessagingException)errorMessage.Payload;
        Assert.IsType<NotSupportedException>(exceptionPayload.InnerException);
        Assert.Same(message, exceptionPayload.FailedMessage);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, resultHandler.LastThread.ManagedThreadId);
    }

    private sealed class ThrowMessageExceptionHandler : IMessageHandler
    {
        public Exception ExceptionToThrow { get; } = new NotSupportedException("intentional test failure");

        public string ServiceName { get; set; } = nameof(ThrowMessageExceptionHandler);

        public void HandleMessage(IMessage message)
        {
            throw new MessagingException(message, ExceptionToThrow);
        }
    }

    private sealed class ThrowingHandler : IMessageHandler
    {
        public Exception ExceptionToThrow { get; } = new NotSupportedException("intentional test failure");

        public string ServiceName { get; set; } = nameof(ThrowingHandler);

        public void HandleMessage(IMessage message)
        {
            throw ExceptionToThrow;
        }
    }

    private sealed class ResultHandler : IMessageHandler
    {
        private readonly CountdownEvent _latch;
        private volatile IMessage _lastMessage;
        private volatile Thread _lastThread;

        public string ServiceName { get; set; } = nameof(ResultHandler);
        public IMessage LastMessage => _lastMessage;
        public Thread LastThread => _lastThread;

        public ResultHandler(CountdownEvent latch)
        {
            _latch = latch;
        }

        public void HandleMessage(IMessage message)
        {
            _lastMessage = message;
            _lastThread = Thread.CurrentThread;
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
