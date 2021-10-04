// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class DispatchingChannelErrorHandlingTest
    {
        private readonly CountdownEvent latch = new (1);

        private IServiceCollection services;

        public DispatchingChannelErrorHandlingTest()
        {
            services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        }

        [Fact]
        public void HandlerThrowsExceptionPublishSubscribeWithoutScheduler()
        {
            var provider = services.BuildServiceProvider();
            var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>());
            var handler = new ThrowingHandler();
            channel.Subscribe(handler);
            var message = IntegrationMessageBuilder.WithPayload("test").Build();
            Assert.Throws<MessageDeliveryException>(() => channel.Send(message));
        }

        [Fact]
        public void HandlerThrowsExceptionPublishSubscribeWithExecutor()
        {
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IMessageChannel>((p) => new DirectChannel(p.GetService<IApplicationContext>(), "errorChannel"));
            var provider = services.BuildServiceProvider();

            var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
            var channel = new PublishSubscribeChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
            var resultHandler = new ResultHandler(latch);
            var throwingHandler = new ThrowMessageExceptionHandler();
            channel.Subscribe(throwingHandler);
            defaultErrorChannel.Subscribe(resultHandler);
            var message = IntegrationMessageBuilder.WithPayload("test").Build();
            channel.Send(message);
            Assert.True(latch.Wait(10000));
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
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IMessageChannel>((p) => new DirectChannel(p.GetService<IApplicationContext>(), "errorChannel"));
            var provider = services.BuildServiceProvider();

            var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
            var channel = new TaskSchedulerChannel(provider.GetService<IApplicationContext>(), TaskScheduler.Default);
            var resultHandler = new ResultHandler(latch);
            var throwingHandler = new ThrowMessageExceptionHandler();
            channel.Subscribe(throwingHandler);
            defaultErrorChannel.Subscribe(resultHandler);
            var message = IntegrationMessageBuilder.WithPayload("test").Build();
            channel.Send(message);
            Assert.True(latch.Wait(10000));
            var errorMessage = resultHandler.LastMessage;
            Assert.IsType<MessagingException>(errorMessage.Payload);
            var exceptionPayload = (MessagingException)errorMessage.Payload;
            Assert.IsType<NotSupportedException>(exceptionPayload.InnerException);
            Assert.Same(message, exceptionPayload.FailedMessage);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, resultHandler.LastThread.ManagedThreadId);
        }

        private class ThrowMessageExceptionHandler : IMessageHandler
        {
            public string ServiceName { get; set; } = nameof(ThrowMessageExceptionHandler);

            public Exception ExceptionToThrow = new NotSupportedException("intentional test failure");

            public void HandleMessage(IMessage message)
            {
                throw new MessagingException(message, ExceptionToThrow);
            }
        }

        private class ThrowingHandler : IMessageHandler
        {
            public string ServiceName { get; set; } = nameof(ThrowingHandler);

            public Exception ExceptionToThrow = new NotSupportedException("intentional test failure");

            public void HandleMessage(IMessage message)
            {
                throw ExceptionToThrow;
            }
        }

        private class ResultHandler : IMessageHandler
        {
            private readonly CountdownEvent latch;

            public ResultHandler(CountdownEvent latch)
            {
                this.latch = latch;
            }

            public string ServiceName { get; set; } = nameof(ResultHandler);

            public volatile IMessage LastMessage;

            public volatile Thread LastThread;

            public void HandleMessage(IMessage message)
            {
                LastMessage = message;
                LastThread = Thread.CurrentThread;
                latch.Signal();
            }
        }

        private class TestTimedOutException : Exception
        {
            public TestTimedOutException()
            : base("timed out while waiting for latch")
            {
            }
        }
    }
}
