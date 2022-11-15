// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Configuration;
using Steeltoe.Stream.Messaging;
using Xunit;

namespace Steeltoe.Stream.Test.Binder;

public class SourceBindingWithBindingTargetsTest : AbstractTest
{
    [Fact]
    public async Task TestSourceOutputChannelBound()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainerWithISourceBinding(searchDirectories, "spring:cloud:stream:defaultBinder=mock",
            "spring.cloud.stream.bindings.output.destination=testtock").BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        IBinder binder = factory.GetBinder(null);
        Assert.NotNull(binder);

        var source = provider.GetService<ISource>();
        Mock<IBinder> mock = Mock.Get(binder);
        mock.Verify(b => b.BindProducer("testtock", source.Output, It.IsAny<ProducerOptions>()));
    }
}
