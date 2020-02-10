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
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Partitioning
{
    public class PartitionedConsumerTest : AbstractTest
    {
        [Fact]
        public async Task TestBindingPartitionedConsumer()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainerWithISinkBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=partIn",
                "spring.cloud.stream.bindings.input.consumer.partitioned=true",
                "spring.cloud.stream.instanceCount=2",
                "spring.cloud.stream.instanceIndex=0")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            Assert.NotNull(factory);
            var binder = factory.GetBinder(null);
            var mockBinder = Mock.Get<IBinder>(binder);
            var sink = provider.GetService<ISink>();
            IConsumerOptions captured = null;
            mockBinder.Setup((b) => b.BindConsumer("partIn", null, sink.Input, It.IsAny<IConsumerOptions>())).Callback<string, string, object, IConsumerOptions>((a, b, c, d) => { captured = d; });

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            mockBinder.Verify((b) => b.BindConsumer("partIn", null, sink.Input, It.IsAny<IConsumerOptions>()));

            Assert.Equal(0, captured.InstanceIndex);
            Assert.Equal(2, captured.InstanceCount);
        }
    }
}
