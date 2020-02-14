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
using Steeltoe.Common.Lifecycle;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class LifecycleBinderTest : AbstractTest
    {
        [Fact]
        public async Task TestOnlySmartLifecyclesStarted()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithBinding(
                searchDirectories,
                typeof(IFooChannels),
                "spring:cloud:stream:defaultBinder=mock");

            container.AddSingleton<ILifecycle, SimpleLifecycle>();

            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var lifecycle = provider.GetService<ILifecycle>();
            Assert.False(lifecycle.IsRunning);
        }

        public class SimpleLifecycle : ILifecycle
        {
            private bool running;

            public Task Start()
            {
                running = true;
                return Task.CompletedTask;
            }

            public Task Stop()
            {
                running = false;
                return Task.CompletedTask;
            }

            public bool IsRunning
            {
                get { return running; }
            }
        }
    }
}
