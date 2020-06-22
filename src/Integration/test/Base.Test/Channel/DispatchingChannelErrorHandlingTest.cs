﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
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
        private readonly CountdownEvent latch = new CountdownEvent(1);

        [Fact]
        public void HandlerThrowsExceptionPublishSubscribeWithoutScheduler()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new PublishSubscribeChannel(provider);
            var handler = new ThrowingHandler();
            channel.Subscribe(handler);
            var message = MessageBuilder.WithPayload("test").Build();
            Assert.Throws<MessageDeliveryException>(() => channel.Send(message));
        }

        [Fact]
        public void HandlerThrowsExceptionPublishSubscribeWithExecutor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.AddSingleton<IMessageChannel>((p) => new DirectChannel(p, "errorChannel"));
            var provider = services.BuildServiceProvider();

            var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
            var channel = new PublishSubscribeChannel(provider, TaskScheduler.Default);
            var resultHandler = new ResultHandler(latch);
            var throwingHandler = new ThrowMessageExceptionHandler();
            channel.Subscribe(throwingHandler);
            defaultErrorChannel.Subscribe(resultHandler);
            var message = MessageBuilder.WithPayload("test").Build();
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
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.AddSingleton<IMessageChannel>((p) => new DirectChannel(p, "errorChannel"));
            var provider = services.BuildServiceProvider();

            var defaultErrorChannel = provider.GetService<IMessageChannel>() as DirectChannel;
            var channel = new TaskSchedulerChannel(provider, TaskScheduler.Default);
            var resultHandler = new ResultHandler(latch);
            var throwingHandler = new ThrowMessageExceptionHandler();
            channel.Subscribe(throwingHandler);
            defaultErrorChannel.Subscribe(resultHandler);
            var message = MessageBuilder.WithPayload("test").Build();
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
            public Exception ExceptionToThrow = new NotSupportedException("intentional test failure");

            public void HandleMessage(IMessage message)
            {
                throw new MessagingException(message, ExceptionToThrow);
            }
        }

        private class ThrowingHandler : IMessageHandler
        {
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
