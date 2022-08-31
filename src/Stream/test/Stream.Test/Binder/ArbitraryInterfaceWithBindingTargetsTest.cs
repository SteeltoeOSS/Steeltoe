// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Stream.Configuration;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class ArbitraryInterfaceWithBindingTargetsTest : AbstractTest
{
    [Fact]
    public async Task TestArbitraryInterfaceChannelsBound()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainerWithBinding(searchDirectories, typeof(IFooChannels), "spring:cloud:stream:defaultBinder=mock",
            "spring:cloud:stream:bindings:Foo:destination=someQueue.0", "spring:cloud:stream:bindings:Bar:destination=someQueue.1",
            "spring:cloud:stream:bindings:Baz:destination=someQueue.2", "spring:cloud:stream:bindings:Qux:destination=someQueue.3").BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        IBinder binder = factory.GetBinder(null, typeof(IMessageChannel));
        Assert.NotNull(binder);
        var fooChannels = provider.GetService<IFooChannels>();
        Assert.NotNull(fooChannels);

        Mock<IBinder> mock = Mock.Get(binder);
        mock.Verify(b => b.BindConsumer("someQueue.0", null, fooChannels.Foo, It.IsAny<ConsumerOptions>()));
        mock.Verify(b => b.BindConsumer("someQueue.1", null, fooChannels.Bar, It.IsAny<ConsumerOptions>()));

        mock.Verify(b => b.BindProducer("someQueue.2", fooChannels.Baz, It.IsAny<ProducerOptions>()));
        mock.Verify(b => b.BindProducer("someQueue.3", fooChannels.Qux, It.IsAny<ProducerOptions>()));
    }
}
