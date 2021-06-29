// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
