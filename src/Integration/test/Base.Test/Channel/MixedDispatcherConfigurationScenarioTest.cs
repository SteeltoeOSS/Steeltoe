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

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class MixedDispatcherConfigurationScenarioTest
    {
        private const int TOTAL_EXECUTIONS = 40;
        private readonly IMessage message = new GenericMessage("test");
        private readonly CountdownEvent allDone;
        private readonly CountdownEvent start;
        private readonly Mock<IMessageHandler> handlerA;
        private readonly Mock<IMessageHandler> handlerB;
        private readonly Mock<IMessageHandler> handlerC;
        private readonly Mock<IList<Exception>> exceptionRegistry;
        private readonly IServiceProvider provider;
        private int failed;

        public MixedDispatcherConfigurationScenarioTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();

            handlerA = new Mock<IMessageHandler>();
            handlerB = new Mock<IMessageHandler>();
            handlerC = new Mock<IMessageHandler>();
            allDone = new CountdownEvent(TOTAL_EXECUTIONS);
            start = new CountdownEvent(1);
            failed = 0;
            exceptionRegistry = new Mock<IList<Exception>>();
        }

        [Fact]
        public void NoFailoverNoLoadBalancing()
        {
            var channel = new DirectChannel(provider, null, "noLoadBalancerNoFailover", null);
            channel.Failover = false;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            try
            {
                channel.Send(message);
            }
            catch (Exception)
            {
            }

            try
            {
                channel.Send(message);
            }
            catch (Exception)
            {
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(2));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(0));
        }

        [Fact]
        public void NoFailoverNoLoadBalancingConcurrent()
        {
            var channel = new DirectChannel(provider, null, "noLoadBalancerNoFailover", null);
            channel.Failover = false;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);

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

                var sent = false;
                try
                {
                    sent = channel.Send(message);
                }
                catch (Exception e2)
                {
                    exceptionRegistry.Object.Add(e2);
                }

                if (!sent)
                {
                    failed = 1;
                }

                allDone.Signal();
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));

            Assert.Equal(1, failed);
            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(TOTAL_EXECUTIONS));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(0));

            exceptionRegistry.Verify((list) => list.Add(It.IsAny<Exception>()), Times.Exactly(TOTAL_EXECUTIONS));
        }

        [Fact]
        public void NoFailoverNoLoadBalancingWithExecutorConcurrent()
        {
            var channel = new TaskSchedulerChannel(provider, TaskScheduler.Default, null, "noLoadBalancerNoFailoverExecutor", null);
            channel.Failover = false;
            handlerA.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                var e = new Exception();
                allDone.Signal();
                failed = 1;
                exceptionRegistry.Object.Add(e);
                throw e;
            });
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);

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

                channel.Send(message);
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));

            Assert.Equal(1, failed);
            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(TOTAL_EXECUTIONS));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(0));

            exceptionRegistry.Verify((list) => list.Add(It.IsAny<Exception>()), Times.Exactly(TOTAL_EXECUTIONS));
        }

        [Fact]
        public void NoFailoverLoadBalancing()
        {
            var channel = new DirectChannel(provider, "loadBalancerNoFailover", null);
            channel.Failover = false;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            dispatcher.AddHandler(handlerC.Object);

            try
            {
                channel.Send(message);
            }
            catch (Exception)
            {
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(0));
            handlerC.Verify((h) => h.HandleMessage(message), Times.Exactly(0));
            try
            {
                channel.Send(message);
            }
            catch (Exception)
            {
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerC.Verify((h) => h.HandleMessage(message), Times.Exactly(0));
            try
            {
                channel.Send(message);
            }
            catch (Exception)
            {
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerC.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
        }

        [Fact]
        public void NoFailoverLoadBalancingConcurrent()
        {
            var channel = new DirectChannel(provider, "loadBalancerNoFailover", null);
            channel.Failover = false;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            dispatcher.AddHandler(handlerC.Object);

            var start1 = new CountdownEvent(1);
            var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
            var message2 = message;
            var failed1 = 0;
            void MessageSenderTask()
            {
                try
                {
                    start1.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                var sent = false;
                try
                {
                    sent = channel.Send(message2);
                }
                catch (Exception e2)
                {
                    exceptionRegistry.Object.Add(e2);
                }

                if (!sent)
                {
                    failed1 = 1;
                }

                allDone1.Signal();
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start1.Signal();
            Assert.True(allDone1.Wait(10000));

            Assert.Equal(1, failed1);
            handlerA.Verify((h) => h.HandleMessage(message2), Times.Exactly(14));
            handlerB.Verify((h) => h.HandleMessage(message2), Times.Exactly(13));
            handlerC.Verify((h) => h.HandleMessage(message2), Times.Exactly(13));
            exceptionRegistry.Verify((list) => list.Add(It.IsAny<Exception>()), Times.Exactly(14));
        }

        [Fact]
        public void NoFailoverLoadBalancingWithExecutorConcurrent()
        {
            var channel = new TaskSchedulerChannel(provider, TaskScheduler.Default, null);
            channel.Failover = false;

            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            dispatcher.AddHandler(handlerC.Object);

            var start1 = new CountdownEvent(1);
            var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
            var message2 = message;
            var failed1 = 0;
            handlerA.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                failed1 = 1;
                var e = new Exception();
                exceptionRegistry.Object.Add(e);
                allDone1.Signal();
                throw e;
            });
            handlerB.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                allDone1.Signal();
            });
            handlerC.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                allDone1.Signal();
            });

            void MessageSenderTask()
            {
                try
                {
                    start1.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                channel.Send(message);
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start1.Signal();
            Assert.True(allDone1.Wait(10000));
            Assert.Equal(1, failed1);
            handlerA.Verify((h) => h.HandleMessage(message2), Times.Exactly(14));
            handlerB.Verify((h) => h.HandleMessage(message2), Times.Exactly(13));
            handlerC.Verify((h) => h.HandleMessage(message2), Times.Exactly(13));
            exceptionRegistry.Verify((list) => list.Add(It.IsAny<Exception>()), Times.Exactly(14));
        }

        [Fact]
        public void FailoverNoLoadBalancing()
        {
            var channel = new DirectChannel(provider, null, "loadBalancerNoFailover", null);
            channel.Failover = true;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);

            try
            {
                channel.Send(message);
            }
            catch (Exception)
            { /* ignore */
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(1));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(1));

            try
            {
                channel.Send(message);
            }
            catch (Exception)
            { /* ignore */
            }

            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(2));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(2));
        }

        [Fact]
        public void FailoverNoLoadBalancingConcurrent()
        {
            var channel = new DirectChannel(provider, null, "noLoadBalancerFailover", null);
            channel.Failover = true;
            handlerA.Setup((h) => h.HandleMessage(message)).Throws(new MessageRejectedException(message, null));
            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            dispatcher.AddHandler(handlerC.Object);

            var start1 = new CountdownEvent(1);
            var allDone1 = new CountdownEvent(TOTAL_EXECUTIONS);
            var message2 = message;
            var failed1 = 0;

            void MessageSenderTask()
            {
                try
                {
                    start1.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                var sent = false;
                try
                {
                    sent = channel.Send(message2);
                }
                catch (Exception e2)
                {
                    exceptionRegistry.Object.Add(e2);
                }

                if (!sent)
                {
                    failed1 = 1;
                }

                allDone1.Signal();
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start1.Signal();
            Assert.True(allDone1.Wait(10000));
            Assert.Equal(0, failed1);
            handlerA.Verify((h) => h.HandleMessage(message2), Times.Exactly(TOTAL_EXECUTIONS));
            handlerB.Verify((h) => h.HandleMessage(message2), Times.Exactly(TOTAL_EXECUTIONS));
            handlerC.Verify((h) => h.HandleMessage(message2), Times.Never());
            exceptionRegistry.Verify((list) => list.Add(It.IsAny<Exception>()), Times.Never());
        }

        [Fact]
        public void FailoverNoLoadBalancingWithExecutorConcurrent()
        {
            var channel = new TaskSchedulerChannel(provider, TaskScheduler.Default, null, null);
            channel.Failover = true;

            var dispatcher = channel.Dispatcher;
            dispatcher.AddHandler(handlerA.Object);
            dispatcher.AddHandler(handlerB.Object);
            dispatcher.AddHandler(handlerC.Object);

            handlerA.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                failed = 1;
                var e = new Exception();
                throw e;
            });
            handlerB.Setup((h) => h.HandleMessage(message)).Callback(() =>
            {
                allDone.Signal();
            });

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

                channel.Send(message);
            }

            for (var i = 0; i < TOTAL_EXECUTIONS; i++)
            {
                Task.Run(MessageSenderTask);
            }

            start.Signal();
            Assert.True(allDone.Wait(10000));
            handlerA.Verify((h) => h.HandleMessage(message), Times.Exactly(TOTAL_EXECUTIONS));
            handlerB.Verify((h) => h.HandleMessage(message), Times.Exactly(TOTAL_EXECUTIONS));
            handlerC.Verify((h) => h.HandleMessage(message), Times.Never());
        }
    }
}
