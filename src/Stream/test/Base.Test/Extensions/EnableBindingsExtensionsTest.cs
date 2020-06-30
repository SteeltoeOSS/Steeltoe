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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging;
using Steeltoe.Stream.Messaging;
using System.Linq;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class EnableBindingsExtensionsTest
    {
        [Fact]
        public void AddProcessor_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddDebug());
            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddStreamServices(config);
            container.AddProcessorStreamBinding();
            var serviceProvider = container.BuildServiceProvider();

            var binding = serviceProvider.GetService<IProcessor>();
            Assert.NotNull(binding);
            var channels = serviceProvider.GetServices<IMessageChannel>();

            // NullChannel, Integration Error Channel, Processor channels (input and output)
            Assert.Equal(4, channels.Count());

            Assert.NotNull(binding.Input);
            Assert.NotNull(binding.Output);
        }

        [Fact]
        public void AddSink_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddDebug());
            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddStreamServices(config);
            container.AddSinkStreamBinding();
            var serviceProvider = container.BuildServiceProvider();

            var binding = serviceProvider.GetService<ISink>();
            Assert.NotNull(binding);
            var channels = serviceProvider.GetServices<IMessageChannel>();

            // NullChannel, Integration Error Channel, Sink channel (input)
            Assert.Equal(3, channels.Count());

            Assert.NotNull(binding.Input);
        }

        [Fact]
        public void AddSource_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddDebug());
            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddStreamServices(config);
            container.AddSourceStreamBinding();
            var serviceProvider = container.BuildServiceProvider();

            var binding = serviceProvider.GetService<ISource>();
            Assert.NotNull(binding);
            var channels = serviceProvider.GetServices<IMessageChannel>();

            // NullChannel, Integration Error Channel, Source channel (output)
            Assert.Equal(3, channels.Count());

            Assert.NotNull(binding.Output);
        }
    }
}
