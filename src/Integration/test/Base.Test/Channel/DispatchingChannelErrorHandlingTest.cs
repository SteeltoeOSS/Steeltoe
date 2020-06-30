// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        private readonly CountdownEvent latch = new CountdownEvent(1);

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
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
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
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
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
