// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test
{
    public class RoundRobinDispatcherConcurrentTest
    {
        private const int TOTAL_EXECUTIONS = 40;

        private readonly UnicastingDispatcher dispatcher;

        private readonly Mock<IMessage> messageMock = new ();

        private readonly Mock<IMessageHandler> handlerMock1 = new ();

        private readonly Mock<IMessageHandler> handlerMock2 = new ();

        private readonly Mock<IMessageHandler> handlerMock3 = new ();

        private readonly Mock<IMessageHandler> handlerMock4 = new ();

        private readonly IServiceProvider provider;

        public RoundRobinDispatcherConcurrentTest()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            provider = services.BuildServiceProvider();
            dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>())
            {
                LoadBalancingStrategy = new RoundRobinLoadBalancingStrategy()
            };
        }

        [Fact]
        public void NoHandlerExhaustion()
        {
            dispatcher.AddHandler(handlerMock1.Object);
            dispatcher.AddHandler(handlerMock2.Object);
            dispatcher.AddHandler(handlerMock3.Object);
            dispatcher.AddHandler(handlerMock4.Object);

            var start = new CountdownEvent(1);
            var allDone = new CountdownEvent(TOTAL_EXECUTIONS);
            var message = messageMock.Object;
            var failed = false;
            void MessageSenderTask()
            {
                try
                {
                    start.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                if (!dispatcher.Dispatch(message))
                {
                    failed = true;
                }

                allDone.Signal();
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));
            Assert.False(failed);
            handlerMock1.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS / 4));
            handlerMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS / 4));
            handlerMock3.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS / 4));
            handlerMock4.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS / 4));
        }

        [Fact]
        public void UnlockOnFailure()
        {
            // dispatcher has no subscribers (shouldn't lead to deadlock)
            var start = new CountdownEvent(1);
            var allDone = new CountdownEvent(TOTAL_EXECUTIONS);
            var message = messageMock.Object;
            void MessageSenderTask()
            {
                try
                {
                    start.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                try
                {
                    dispatcher.Dispatch(message);
                    throw new Exception("this shouldn't happen");
                }
                catch (MessagingException)
                {
                    // expected
                }

                allDone.Signal();
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));
        }

        [Fact]
        public void NoHandlerSkipUnderConcurrentFailureWithFailover()
        {
            dispatcher.AddHandler(handlerMock1.Object);
            dispatcher.AddHandler(handlerMock2.Object);
            handlerMock1.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(new MessageRejectedException(messageMock.Object, null));
            var start = new CountdownEvent(1);
            var allDone = new CountdownEvent(TOTAL_EXECUTIONS);
            var message = messageMock.Object;
            var failed = false;
            void MessageSenderTask()
            {
                try
                {
                    start.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                if (!dispatcher.Dispatch(message))
                {
                    failed = true;
                }
                else
                {
                    allDone.Signal();
                }
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));
            Assert.False(failed);
            handlerMock1.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS / 2));
            handlerMock2.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(TOTAL_EXECUTIONS));
        }
    }
}
