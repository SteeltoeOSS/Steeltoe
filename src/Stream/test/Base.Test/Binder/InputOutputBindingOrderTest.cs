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
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class InputOutputBindingOrderTest : AbstractTest
    {
        [Fact]
        public async Task TestInputOutputBindingOrder()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithBinding(
                searchDirectories,
                typeof(IProcessor),
                "spring:cloud:stream:defaultBinder=mock");

            container.AddSingleton<SomeLifecycle>();
            container.AddSingleton<ILifecycle>(p => p.GetService<SomeLifecycle>());
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder(null, typeof(IMessageChannel));
            var processor = provider.GetService<IProcessor>();

            var mock = Mock.Get(binder);
            mock.Verify(b => b.BindConsumer("input", null, processor.Input, It.IsAny<ConsumerOptions>()));

            var lifecycle = provider.GetService<SomeLifecycle>();
            Assert.True(lifecycle.IsRunning);
        }

        public class SomeLifecycle : ISmartLifecycle
        {
            public SomeLifecycle(IBinderFactory factory, IProcessor procesor)
            {
                Factory = factory;
                Processor = procesor;
            }

            private bool running;

            public Task Start()
            {
                var binder = Factory.GetBinder(null, typeof(IMessageChannel));

                var mock = Mock.Get(binder);
                mock.Verify(b => b.BindProducer("output", Processor.Output, It.IsAny<ProducerOptions>()));

                running = true;
                return Task.CompletedTask;
            }

            public Task Stop()
            {
                running = false;
                return Task.CompletedTask;
            }

            public async Task Stop(Action callback)
            {
                await Stop();
                callback?.Invoke();
            }

            public bool IsRunning
            {
                get { return running; }
            }

            public IBinderFactory Factory { get; }

            public IProcessor Processor { get; }

            public bool IsAutoStartup => true;

            public int Phase => 0;
        }
    }
}
