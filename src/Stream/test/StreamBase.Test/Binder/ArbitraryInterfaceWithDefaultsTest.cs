﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class ArbitraryInterfaceWithDefaultsTest : AbstractTest
{
    [Fact]
    public async Task TestArbitraryInterfaceChannelsBound()
    {
        var searchDirectories = GetSearchDirectories("MockBinder");
        var provider = CreateStreamsContainerWithBinding(searchDirectories, typeof(IFooChannels), "spring:cloud:stream:defaultBinder=mock")
            .BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        var binder = factory.GetBinder(null, typeof(IMessageChannel));
        Assert.NotNull(binder);
        var fooChannels = provider.GetService<IFooChannels>();
        Assert.NotNull(fooChannels);

        var mock = Mock.Get(binder);
        mock.Verify(b => b.BindConsumer("Foo", null, fooChannels.Foo, It.IsAny<ConsumerOptions>()));
        mock.Verify(b => b.BindConsumer("Bar", null, fooChannels.Bar, It.IsAny<ConsumerOptions>()));

        mock.Verify(b => b.BindProducer("Baz", fooChannels.Baz, It.IsAny<ProducerOptions>()));
        mock.Verify(b => b.BindProducer("Qux", fooChannels.Qux, It.IsAny<ProducerOptions>()));
    }
}