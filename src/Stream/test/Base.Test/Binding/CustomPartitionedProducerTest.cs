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
using Steeltoe.Integration.Channel;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.Partitioning;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binding
{
    public class CustomPartitionedProducerTest : AbstractTest
    {
        [Fact]
        public async Task TestCustomPartitionedProducerByName()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=partOut",
                "spring.cloud.stream.bindings.output.producer.partitionCount=3",
                "spring.cloud.stream.bindings.output.producer.partitionKeyExtractorName=CustomPartitionKeyExtractorClass",
                "spring.cloud.stream.bindings.output.producer.partitionSelectorName=CustomPartitionSelectorClass",
                "spring.cloud.stream.defaultbinder=mock");

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClass>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClass>();

            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var source = provider.GetService<ISource>();
            var messageChannel = source.Output as DirectChannel;
            var foundpartInterceptor = false;
            foreach (var interceptor in messageChannel.ChannelInterceptors)
            {
                if (interceptor is PartitioningInterceptor partInterceptor)
                {
                    foundpartInterceptor = true;
                    Assert.NotNull(partInterceptor._partitionHandler);
                    Assert.IsType<CustomPartitionKeyExtractorClass>(partInterceptor._partitionHandler._partitionKeyExtractorStrategy);
                    Assert.IsType<CustomPartitionSelectorClass>(partInterceptor._partitionHandler._partitionSelectorStrategy);
                }
            }

            Assert.True(foundpartInterceptor);

            await provider.GetRequiredService<ILifecycleProcessor>().Stop();
        }

        [Fact]
        public async Task TestCustomPartitionedProducerAsSingletons()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=partOut",
                "spring.cloud.stream.bindings.output.producer.partitionCount=3",
                "spring.cloud.stream.defaultbinder=mock");

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClass>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClass>();

            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var source = provider.GetService<ISource>();
            var messageChannel = source.Output as DirectChannel;
            var foundpartInterceptor = false;
            foreach (var interceptor in messageChannel.ChannelInterceptors)
            {
                if (interceptor is PartitioningInterceptor partInterceptor)
                {
                    foundpartInterceptor = true;
                    Assert.NotNull(partInterceptor._partitionHandler);
                    Assert.IsType<CustomPartitionKeyExtractorClass>(partInterceptor._partitionHandler._partitionKeyExtractorStrategy);
                    Assert.IsType<CustomPartitionSelectorClass>(partInterceptor._partitionHandler._partitionSelectorStrategy);
                }
            }

            Assert.True(foundpartInterceptor);
            await provider.GetRequiredService<ILifecycleProcessor>().Stop();
        }

        [Fact]
        public async Task TestCustomPartitionedProducerMultipleInstances()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=partOut",
                "spring.cloud.stream.bindings.output.producer.partitionCount=3",
                "spring.cloud.stream.bindings.output.producer.partitionKeyExtractorName=CustomPartitionKeyExtractorClassOne",
                "spring.cloud.stream.bindings.output.producer.partitionSelectorName=CustomPartitionSelectorClassTwo",
                "spring.cloud.stream.defaultbinder=mock");

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassOne>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassOne>();

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassTwo>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassTwo>();

            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var source = provider.GetService<ISource>();
            var messageChannel = source.Output as DirectChannel;
            var foundpartInterceptor = false;
            foreach (var interceptor in messageChannel.ChannelInterceptors)
            {
                if (interceptor is PartitioningInterceptor partInterceptor)
                {
                    foundpartInterceptor = true;
                    Assert.NotNull(partInterceptor._partitionHandler);
                    Assert.IsType<CustomPartitionKeyExtractorClassOne>(partInterceptor._partitionHandler._partitionKeyExtractorStrategy);
                    Assert.IsType<CustomPartitionSelectorClassTwo>(partInterceptor._partitionHandler._partitionSelectorStrategy);
                }
            }

            Assert.True(foundpartInterceptor);

            await provider.GetRequiredService<ILifecycleProcessor>().Stop();
        }

        [Fact]
        public async Task TestCustomPartitionedProducerMultipleInstancesFailNoFilter()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=partOut",
                "spring.cloud.stream.bindings.output.producer.partitionCount=3",
                "spring.cloud.stream.defaultbinder=mock");

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassOne>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassOne>();

            container.AddSingleton<IPartitionKeyExtractorStrategy, CustomPartitionKeyExtractorClassTwo>();
            container.AddSingleton<IPartitionSelectorStrategy, CustomPartitionSelectorClassTwo>();

            var provider = container.BuildServiceProvider();

            var exceptionThrown = false;
            try
            {
                await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
        }
    }
}
