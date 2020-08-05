// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixServiceCollectionExtensionsTest
    {
        private readonly IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("DummyCommand");
        private readonly IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey("DummyCommand");

        [Fact]
        public void AddHystrixCommand_ThrowsIfServiceContainerNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            var stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, groupKey, null));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, groupKey, null));
            Assert.Contains(nameof(services), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, stringKey, null));
            Assert.Contains(nameof(services), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, null));
            Assert.Contains(nameof(services), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, groupKey, commandKey, null));
            Assert.Contains(nameof(services), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, groupKey, commandKey, null));
            Assert.Contains(nameof(services), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, stringKey, stringKey, null));
            Assert.Contains(nameof(services), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, stringKey, null));
            Assert.Contains(nameof(services), ex8.Message);
        }

        [Fact]
        public void AddHystrixCommand_ThrowsIfGroupKeyNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            var stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, (IHystrixCommandGroupKey)null, null));
            Assert.Contains(nameof(groupKey), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, (IHystrixCommandGroupKey)null, null));
            Assert.Contains(nameof(groupKey), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, (string)null, null));
            Assert.Contains(nameof(groupKey), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, (string)null, null));
            Assert.Contains(nameof(groupKey), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, null, commandKey, null));
            Assert.Contains(nameof(groupKey), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, null, commandKey, null));
            Assert.Contains(nameof(groupKey), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, null, stringKey, null));
            Assert.Contains(nameof(groupKey), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, null, stringKey, null));
            Assert.Contains(nameof(groupKey), ex8.Message);
        }

        [Fact]
        public void AddHystrixCommand_ThrowsIfConfigNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            var stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, null));
            Assert.Contains(nameof(config), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, null));
            Assert.Contains(nameof(config), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, stringKey, null));
            Assert.Contains(nameof(config), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, null));
            Assert.Contains(nameof(config), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, commandKey, null));
            Assert.Contains(nameof(config), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, commandKey, null));
            Assert.Contains(nameof(config), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, stringKey, stringKey, null));
            Assert.Contains(nameof(config), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, stringKey, null));
            Assert.Contains(nameof(config), ex8.Message);
        }

        [Fact]
        public void AddHystrixCommand_ThrowsIfCommandKeyNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            var stringKey = "DummyCommand";

            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, null, null));
            Assert.Contains(nameof(commandKey), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, null, null));
            Assert.Contains(nameof(commandKey), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, stringKey, null, null));
            Assert.Contains(nameof(commandKey), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, null, null));
            Assert.Contains(nameof(commandKey), ex8.Message);
        }

        [Fact]
        public void AddHystrixCommand_AddsToContainer()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, config);
            var provider = services.BuildServiceProvider();
            var command = provider.GetService<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            var expectedCommandKey = HystrixCommandKeyDefault.AsKey(typeof(DummyCommand).Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            var expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            var threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, config);
            provider = services.BuildServiceProvider();
            var icommand = provider.GetService<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, "GroupKey", config);
            provider = services.BuildServiceProvider();
            command = provider.GetService<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            expectedCommandKey = HystrixCommandKeyDefault.AsKey(typeof(DummyCommand).Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(command.CommandGroup.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, "GroupKey", config);
            provider = services.BuildServiceProvider();
            icommand = provider.GetService<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(command.CommandGroup.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, commandKey, config);
            provider = services.BuildServiceProvider();
            command = provider.GetService<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(commandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, commandKey, config);
            provider = services.BuildServiceProvider();
            icommand = provider.GetService<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(commandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, "GroupKey", "CommandKey", config);
            provider = services.BuildServiceProvider();
            command = provider.GetService<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal("CommandKey", command.CommandKey.Name);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(command.CommandGroup.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            services = new ServiceCollection();
            config = new ConfigurationBuilder().Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(services, "GroupKey", "CommandKey", config);
            provider = services.BuildServiceProvider();
            icommand = provider.GetService<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal("CommandKey", command.CommandKey.Name);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(command.CommandGroup.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);
        }

        [Fact]
        public void AddHystrixCommand_WithConfiguration_ConfiguresSettings()
        {
            var appSettings = new Dictionary<string, string>()
            {
                ["hystrix:command:default:metrics:rollingStats:timeInMilliseconds"] = "5555",
                ["hystrix:command:default:circuitBreaker:errorThresholdPercentage"] = "55",
                ["hystrix:command:default:circuitBreaker:sleepWindowInMilliseconds"] = "9999",
                ["hystrix:command:default:circuitBreaker:requestVolumeThreshold"] = "1111",
                ["hystrix:command:default:execution:isolation:thread:timeoutInMilliseconds"] = "2222",
                ["hystrix:threadpool:default:coreSize"] = "33",
                ["hystrix:threadpool:default:maximumSize"] = "55",
                ["hystrix:threadpool:default:maxQueueSize"] = "66",
                ["hystrix:threadpool:default:queueSizeRejectionThreshold"] = "6",
                ["hystrix:threadpool:default:allowMaximumSizeToDivergeFromCoreSize"] = "false"
            };

            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
            HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(services, groupKey, config);
            var provider = services.BuildServiceProvider();
            var command = provider.GetService<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            var expectedCommandKey = HystrixCommandKeyDefault.AsKey(typeof(DummyCommand).Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
            var expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
            var threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
            Assert.NotNull(threadOptions);
            Assert.NotNull(threadOptions._dynamic);
            Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

            Assert.Equal(55, threadOptions.MaximumSize);
            Assert.Equal(33, threadOptions.CoreSize);
            Assert.Equal(66, threadOptions.MaxQueueSize);
            Assert.Equal(6, threadOptions.QueueSizeRejectionThreshold);
            Assert.False(threadOptions.AllowMaximumSizeToDivergeFromCoreSize);

            Assert.Equal(5555, command.Options.MetricsRollingStatisticalWindowInMilliseconds);
            Assert.Equal(2222, command.Options.ExecutionTimeoutInMilliseconds);
            Assert.Equal(55, command.Options.CircuitBreakerErrorThresholdPercentage);
            Assert.Equal(9999, command.Options.CircuitBreakerSleepWindowInMilliseconds);
            Assert.Equal(1111, command.Options.CircuitBreakerRequestVolumeThreshold);
        }
    }
}
