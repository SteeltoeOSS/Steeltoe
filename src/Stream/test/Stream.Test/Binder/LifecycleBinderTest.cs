// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Lifecycle;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class LifecycleBinderTest : AbstractTest
{
    [Fact]
    public async Task TestOnlySmartLifecyclesStarted()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");
        ServiceCollection container = CreateStreamsContainerWithBinding(searchDirectories, typeof(IFooChannels), "spring:cloud:stream:defaultBinder=mock");

        container.AddSingleton<ILifecycle, SimpleLifecycle>();

        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var lifecycle = provider.GetService<ILifecycle>();
        Assert.False(lifecycle.IsRunning);
    }

    public class SimpleLifecycle : ILifecycle
    {
        public bool IsRunning { get; private set; }

        public Task StartAsync()
        {
            IsRunning = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }
    }
}
