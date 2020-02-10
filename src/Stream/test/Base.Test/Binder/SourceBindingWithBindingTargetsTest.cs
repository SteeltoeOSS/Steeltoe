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
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class SourceBindingWithBindingTargetsTest : AbstractTest
    {
        [Fact]
        public async Task TestSourceOutputChannelBound()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainerWithISourceBinding(
                searchDirectories,
                "spring:cloud:stream:defaultBinder=mock",
                "spring.cloud.stream.bindings.output.destination=testtock")
                .BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var factory = provider.GetService<IBinderFactory>();
            Assert.NotNull(factory);
            var binder = factory.GetBinder(null);
            Assert.NotNull(binder);

            var source = provider.GetService<ISource>();
            var mock = Mock.Get(binder);
            mock.Verify(b => b.BindProducer("testtock", source.Output, It.IsAny<ProducerOptions>()));
        }
    }
}
