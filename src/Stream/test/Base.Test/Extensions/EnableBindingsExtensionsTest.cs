// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
