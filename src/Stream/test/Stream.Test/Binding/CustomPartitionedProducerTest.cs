// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.Partitioning;
using Xunit;

namespace Steeltoe.Stream.Binding;

public class CustomPartitionedProducerTest : AbstractTest
{
    [Fact]
    public async Task TestCustomPartitionedProducerByName()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceCollection container = CreateStreamsContainerWithISourceBinding(searchDirectories, "spring.cloud.stream.bindings.output.destination=partOut",
            "spring.cloud.stream.bindings.output.producer.partitionCount=3",
            "spring.cloud.stream.bindings.output.producer.partitionKeyExtractorName=CustomPartitionKeyExtractorClass",
            "spring.cloud.stream.bindings.output.producer.partitionSelectorName=CustomPartitionSelectorClass", "spring.cloud.stream.defaultbinder=mock");

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClass>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClass>();

        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var source = provider.GetService<ISource>();
        var messageChannel = source.Output as DirectChannel;
        bool foundPartInterceptor = false;

        foreach (IChannelInterceptor interceptor in messageChannel.ChannelInterceptors)
        {
            if (interceptor is PartitioningInterceptor partInterceptor)
            {
                foundPartInterceptor = true;
                Assert.NotNull(partInterceptor.PartitionHandler);
                Assert.IsType<CustomPartitionKeyExtractorClass>(partInterceptor.PartitionHandler.PartitionKeyExtractorStrategy);
                Assert.IsType<CustomPartitionSelectorClass>(partInterceptor.PartitionHandler.PartitionSelectorStrategy);
            }
        }

        Assert.True(foundPartInterceptor);

        await provider.GetRequiredService<ILifecycleProcessor>().StopAsync();
    }

    [Fact]
    public async Task TestCustomPartitionedProducerAsSingletons()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceCollection container = CreateStreamsContainerWithISourceBinding(searchDirectories, "spring.cloud.stream.bindings.output.destination=partOut",
            "spring.cloud.stream.bindings.output.producer.partitionCount=3", "spring.cloud.stream.defaultbinder=mock");

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClass>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClass>();

        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var source = provider.GetService<ISource>();
        var messageChannel = source.Output as DirectChannel;
        bool foundPartInterceptor = false;

        foreach (IChannelInterceptor interceptor in messageChannel.ChannelInterceptors)
        {
            if (interceptor is PartitioningInterceptor partInterceptor)
            {
                foundPartInterceptor = true;
                Assert.NotNull(partInterceptor.PartitionHandler);
                Assert.IsType<CustomPartitionKeyExtractorClass>(partInterceptor.PartitionHandler.PartitionKeyExtractorStrategy);
                Assert.IsType<CustomPartitionSelectorClass>(partInterceptor.PartitionHandler.PartitionSelectorStrategy);
            }
        }

        Assert.True(foundPartInterceptor);
        await provider.GetRequiredService<ILifecycleProcessor>().StopAsync();
    }

    [Fact]
    public async Task TestCustomPartitionedProducerMultipleInstances()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceCollection container = CreateStreamsContainerWithISourceBinding(searchDirectories, "spring.cloud.stream.bindings.output.destination=partOut",
            "spring.cloud.stream.bindings.output.producer.partitionCount=3",
            "spring.cloud.stream.bindings.output.producer.partitionKeyExtractorName=CustomPartitionKeyExtractorClassOne",
            "spring.cloud.stream.bindings.output.producer.partitionSelectorName=CustomPartitionSelectorClassTwo", "spring.cloud.stream.defaultbinder=mock");

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassOne>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassOne>();

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassTwo>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassTwo>();

        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var source = provider.GetService<ISource>();
        var messageChannel = source.Output as DirectChannel;
        bool foundPartInterceptor = false;

        foreach (IChannelInterceptor interceptor in messageChannel.ChannelInterceptors)
        {
            if (interceptor is PartitioningInterceptor partInterceptor)
            {
                foundPartInterceptor = true;
                Assert.NotNull(partInterceptor.PartitionHandler);
                Assert.IsType<CustomPartitionKeyExtractorClassOne>(partInterceptor.PartitionHandler.PartitionKeyExtractorStrategy);
                Assert.IsType<CustomPartitionSelectorClassTwo>(partInterceptor.PartitionHandler.PartitionSelectorStrategy);
            }
        }

        Assert.True(foundPartInterceptor);

        await provider.GetRequiredService<ILifecycleProcessor>().StopAsync();
    }

    [Fact]
    public async Task TestCustomPartitionedProducerMultipleInstancesFailNoFilter()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceCollection container = CreateStreamsContainerWithISourceBinding(searchDirectories, "spring.cloud.stream.bindings.output.destination=partOut",
            "spring.cloud.stream.bindings.output.producer.partitionCount=3", "spring.cloud.stream.defaultbinder=mock");

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassOne>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassOne>();

        container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassTwo>();
        container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassTwo>();

        ServiceProvider provider = container.BuildServiceProvider();

        bool exceptionThrown = false;

        try
        {
            await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);
    }
}
