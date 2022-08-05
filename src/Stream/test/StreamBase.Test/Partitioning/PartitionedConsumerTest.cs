// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using Xunit;

namespace Steeltoe.Stream.Partitioning;

public class PartitionedConsumerTest : AbstractTest
{
    [Fact]
    public async Task TestBindingPartitionedConsumer()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainerWithISinkBinding(searchDirectories, "spring.cloud.stream.bindings.input.destination=partIn",
                "spring.cloud.stream.bindings.input.consumer.partitioned=true", "spring.cloud.stream.instanceCount=2", "spring.cloud.stream.instanceIndex=0")
            .BuildServiceProvider();

        var factory = provider.GetService<IBinderFactory>();
        Assert.NotNull(factory);
        IBinder binder = factory.GetBinder(null);
        Mock<IBinder> mockBinder = Mock.Get(binder);
        var sink = provider.GetService<ISink>();
        IConsumerOptions captured = null;

        mockBinder.Setup(b => b.BindConsumer("partIn", null, sink.Input, It.IsAny<IConsumerOptions>())).Callback<string, string, object, IConsumerOptions>(
            (_, _, _, d) =>
            {
                captured = d;
            });

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        mockBinder.Verify(b => b.BindConsumer("partIn", null, sink.Input, It.IsAny<IConsumerOptions>()));

        Assert.Equal(0, captured.InstanceIndex);
        Assert.Equal(2, captured.InstanceCount);
    }
}
