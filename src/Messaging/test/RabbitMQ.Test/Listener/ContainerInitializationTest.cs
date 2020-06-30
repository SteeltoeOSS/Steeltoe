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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Host;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class ContainerInitializationTest : AbstractTest, IDisposable
    {
        public const string TEST_MISMATCH = "test.mismatch";
        public const string TEST_MISMATCH2 = "test.mismatch2";

        private ServiceProvider provider;

        [Fact]
        public async Task TestNoAdmin()
        {
            var services = CreateServiceCollection();
            services.AddRabbitQueue(new Config.Queue(TEST_MISMATCH, false, false, true));
            services.AddSingleton((p) =>
            {
                return CreateMessageListenerContainer(p, TEST_MISMATCH);
            });

            services.AddSingleton<ILifecycle>(p =>
            {
                return p.GetService<DirectMessageListenerContainer>();
            });

            provider = services.BuildServiceProvider();
            try
            {
                await provider.GetRequiredService<IHostedService>().StartAsync(default);
            }
            catch (LifecycleException le)
            {
                Assert.IsType<InvalidOperationException>(le.InnerException.InnerException);
                Assert.Contains("mismatchedQueuesFatal", le.InnerException.InnerException.Message);
            }
        }

        [Fact]
        public async Task TestMismatchedQueue()
        {
            var services = CreateServiceCollection();
            services.AddSingleton((p) =>
            {
                return CreateMessageListenerContainer(p, TEST_MISMATCH);
            });

            services.AddSingleton<ILifecycle>(p =>
            {
                return p.GetService<DirectMessageListenerContainer>();
            });
            services.AddRabbitQueue(new Config.Queue(TEST_MISMATCH, false, false, true));
            services.AddRabbitAdmin();

            provider = services.BuildServiceProvider();
            try
            {
                await provider.GetRequiredService<IHostedService>().StartAsync(default);
            }
            catch (LifecycleException le)
            {
                Assert.IsType<InvalidOperationException>(le.InnerException.InnerException);
                Assert.Contains("mismatchedQueuesFatal", le.InnerException.InnerException.Message);
            }
        }

        [Fact]
        public async Task TestMismatchedQueueDuringRestart()
        {
            var services = CreateServiceCollection();
            services.AddSingleton((p) =>
            {
                return CreateMessageListenerContainer(p, TEST_MISMATCH);
            });

            services.AddSingleton<ILifecycle>(p =>
            {
                return p.GetService<DirectMessageListenerContainer>();
            });
            services.AddRabbitQueue(new Config.Queue(TEST_MISMATCH, true, false, false));
            services.AddRabbitAdmin();
            provider = services.BuildServiceProvider();

            CountdownEvent[] latches = SetUpChannelLatches(provider);
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
            var container = provider.GetService<DirectMessageListenerContainer>();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            var admin = provider.GetRabbitAdmin() as RabbitAdmin;
            admin.RetryTemplate = null;
            admin.DeleteQueue(TEST_MISMATCH);
            Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
            admin.DeclareQueue(new Config.Queue(TEST_MISMATCH, false, false, true));
            latches[2].Signal();
            Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

            var n = 0;
            while (n++ < 200 && container.IsRunning)
            {
                await Task.Delay(100);
            }

            Assert.False(container.IsRunning);
        }

        [Fact]
        public async Task TestMismatchedQueueDuringRestartMultiQueue()
        {
            var services = CreateServiceCollection();
            services.AddSingleton((p) =>
            {
                return CreateMessageListenerContainer(p, TEST_MISMATCH, TEST_MISMATCH2);
            });

            services.AddSingleton<ILifecycle>(p =>
            {
                return p.GetService<DirectMessageListenerContainer>();
            });
            services.AddRabbitQueue(new Config.Queue(TEST_MISMATCH, true, false, false));
            services.AddRabbitQueue(new Config.Queue(TEST_MISMATCH2, true, false, false));
            services.AddRabbitAdmin();
            provider = services.BuildServiceProvider();

            CountdownEvent[] latches = SetUpChannelLatches(provider);
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
            var container = provider.GetService<DirectMessageListenerContainer>();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10)));

            var admin = provider.GetRabbitAdmin() as RabbitAdmin;
            admin.RetryTemplate = null;
            admin.DeleteQueue(TEST_MISMATCH);
            Assert.True(latches[0].Wait(TimeSpan.FromSeconds(100)));
            admin.DeclareQueue(new Config.Queue(TEST_MISMATCH, false, false, true));
            latches[2].Signal();
            Assert.True(latches[1].Wait(TimeSpan.FromSeconds(10)));

            var n = 0;
            while (n++ < 200 && container.IsRunning)
            {
                await Task.Delay(100);
            }

            Assert.False(container.IsRunning);
        }

        public void Dispose()
        {
            var admin = provider.GetService<IRabbitAdmin>() as RabbitAdmin;
            if (admin != null)
            {
                admin.IgnoreDeclarationExceptions = true;
                try
                {
                    admin.DeleteQueue(TEST_MISMATCH);
                    admin.DeleteQueue(TEST_MISMATCH2);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            provider.Dispose();
        }

        private CountdownEvent[] SetUpChannelLatches(IServiceProvider context)
        {
            var cf = context.GetService<Connection.IConnectionFactory>() as CachingConnectionFactory;
            var cancelLatch = new CountdownEvent(1);
            var mismatchLatch = new CountdownEvent(1);
            var preventContainerRedeclareQueueLatch = new CountdownEvent(1);
            var listener = new TestListener(cancelLatch, mismatchLatch, preventContainerRedeclareQueueLatch);
            cf.AddChannelListener(listener);
            return new CountdownEvent[] { cancelLatch, mismatchLatch, preventContainerRedeclareQueueLatch };
        }

        private ServiceCollection CreateServiceCollection()
        {
            var services = CreateContainer();

            services.AddHostedService<RabbitHostService>();
            services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();
            services.TryAddSingleton<Connection.IConnectionFactory, CachingConnectionFactory>();
            services.TryAddSingleton<Converter.ISmartMessageConverter, Rabbit.Support.Converter.SimpleMessageConverter>();
            return services;
        }

        private class TestListener : IShutDownChannelListener
        {
            private CountdownEvent cancelLatch;
            private CountdownEvent mismatchLatch;
            private CountdownEvent preventContainerRedeclareQueueLatch;

            public TestListener(CountdownEvent cancelLatch, CountdownEvent mismatchLatch, CountdownEvent preventContainerRedeclareQueueLatch)
            {
                this.cancelLatch = cancelLatch;
                this.mismatchLatch = mismatchLatch;
                this.preventContainerRedeclareQueueLatch = preventContainerRedeclareQueueLatch;
            }

            public void OnCreate(IModel channel, bool transactional)
            {
            }

            public void OnShutDown(ShutdownEventArgs args)
            {
                if (RabbitUtils.IsNormalChannelClose(args))
                {
                    cancelLatch.Signal();
                    try
                    {
                        preventContainerRedeclareQueueLatch.Wait(TimeSpan.FromSeconds(10));
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
                else if (RabbitUtils.IsMismatchedQueueArgs(args))
                {
                    mismatchLatch.Signal();
                }
            }
        }

        private DirectMessageListenerContainer CreateMessageListenerContainer(IServiceProvider services, params string[] queueNames)
        {
            var cf = services.GetRequiredService<Connection.IConnectionFactory>();
            var ctx = services.GetRequiredService<IApplicationContext>();
            var queue2 = services.GetRequiredService<IQueue>();
            var listener = new TestMessageListener();
            var container = new DirectMessageListenerContainer(ctx, cf);
            container.SetQueueNames(queueNames);
            container.SetupMessageListener(listener);
            container.MismatchedQueuesFatal = true;

            return container;
        }

        private class TestMessageListener : IMessageListener
        {
            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message)
            {
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
            }
        }
    }
}
