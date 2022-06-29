// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixServiceCollectionExtensionsTest
{
    private readonly IHystrixCommandGroupKey _groupKey = HystrixCommandGroupKeyDefault.AsKey("DummyCommand");
    private readonly IHystrixCommandKey _commandKey = HystrixCommandKeyDefault.AsKey("DummyCommand");

    [Fact]
    public void AddHystrixCommand_ThrowsIfServiceContainerNull()
    {
        var stringKey = "DummyCommand";
        const string servicesParameterName = "services";

        var ex = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, _groupKey, null));
        Assert.Contains(servicesParameterName, ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, _groupKey, null));
        Assert.Contains(servicesParameterName, ex2.Message);
        var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, stringKey, null));
        Assert.Contains(servicesParameterName, ex3.Message);
        var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, null));
        Assert.Contains(servicesParameterName, ex4.Message);
        var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, _groupKey, _commandKey, null));
        Assert.Contains(servicesParameterName, ex5.Message);
        var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, _groupKey, _commandKey, null));
        Assert.Contains(servicesParameterName, ex6.Message);
        var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<DummyCommand>(null, stringKey, stringKey, null));
        Assert.Contains(servicesParameterName, ex7.Message);
        var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixServiceCollectionExtensions.AddHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, stringKey, null));
        Assert.Contains(servicesParameterName, ex8.Message);
    }

    [Fact]
    public void AddHystrixCommand_ThrowsIfGroupKeyNull()
    {
        IServiceCollection services = new ServiceCollection();
        var stringKey = "DummyCommand";
        const string groupKeyParameterName = "groupKey";

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>((IHystrixCommandGroupKey)null, null));
        Assert.Contains(groupKeyParameterName, ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>((IHystrixCommandGroupKey)null, null));
        Assert.Contains(groupKeyParameterName, ex2.Message);
        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>((string)null, null));
        Assert.Contains(groupKeyParameterName, ex3.Message);
        var ex4 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>((string)null, null));
        Assert.Contains(groupKeyParameterName, ex4.Message);
        var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(null, _commandKey, null));
        Assert.Contains(groupKeyParameterName, ex5.Message);
        var ex6 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(null, _commandKey, null));
        Assert.Contains(groupKeyParameterName, ex6.Message);
        var ex7 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(null, stringKey, null));
        Assert.Contains(groupKeyParameterName, ex7.Message);
        var ex8 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, null));
        Assert.Contains(groupKeyParameterName, ex8.Message);
    }

    [Fact]
    public void AddHystrixCommand_ThrowsIfConfigNull()
    {
        IServiceCollection services = new ServiceCollection();
        var stringKey = "DummyCommand";
        const string configParameterName = "config";

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(_groupKey, null));
        Assert.Contains(configParameterName, ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(_groupKey, null));
        Assert.Contains(configParameterName, ex2.Message);
        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(stringKey, null));
        Assert.Contains(configParameterName, ex3.Message);
        var ex4 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(stringKey, null));
        Assert.Contains(configParameterName, ex4.Message);
        var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(_groupKey, _commandKey, null));
        Assert.Contains(configParameterName, ex5.Message);
        var ex6 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(_groupKey, _commandKey, null));
        Assert.Contains(configParameterName, ex6.Message);
        var ex7 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(stringKey, stringKey, null));
        Assert.Contains(configParameterName, ex7.Message);
        var ex8 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(stringKey, stringKey, null));
        Assert.Contains(configParameterName, ex8.Message);
    }

    [Fact]
    public void AddHystrixCommand_ThrowsIfCommandKeyNull()
    {
        IServiceCollection services = new ServiceCollection();
        var stringKey = "DummyCommand";
        const string commandKeyParameterName = "commandKey";

        var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(_groupKey, null, null));
        Assert.Contains(commandKeyParameterName, ex5.Message);
        var ex6 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(_groupKey, null, null));
        Assert.Contains(commandKeyParameterName, ex6.Message);
        var ex7 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<DummyCommand>(stringKey, null, null));
        Assert.Contains(commandKeyParameterName, ex7.Message);
        var ex8 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixCommand<IDummyCommand, DummyCommand>(stringKey, null, null));
        Assert.Contains(commandKeyParameterName, ex8.Message);
    }

    [Fact]
    public void AddHystrixCommand_AddsToContainer()
    {
        IServiceCollection services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder().Build();
        services.AddHystrixCommand<DummyCommand>(_groupKey, config);
        var provider = services.BuildServiceProvider();
        var command = provider.GetService<DummyCommand>();
        Assert.NotNull(command);
        Assert.Equal(_groupKey, command.CommandGroup);
        var expectedCommandKey = HystrixCommandKeyDefault.AsKey(nameof(DummyCommand));
        Assert.Equal(expectedCommandKey, command.CommandKey);
        Assert.NotNull(command.Options);
        Assert.NotNull(command.Options._dynamic);
        var expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(_groupKey.Name);
        Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
        var threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
        Assert.NotNull(threadOptions);
        Assert.NotNull(threadOptions._dynamic);
        Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

        services = new ServiceCollection();
        config = new ConfigurationBuilder().Build();
        services.AddHystrixCommand<IDummyCommand, DummyCommand>(_groupKey, config);
        provider = services.BuildServiceProvider();
        var icommand = provider.GetService<IDummyCommand>();
        Assert.NotNull(icommand);
        command = icommand as DummyCommand;
        Assert.NotNull(command);
        Assert.Equal(_groupKey, command.CommandGroup);
        Assert.Equal(expectedCommandKey, command.CommandKey);
        Assert.NotNull(command.Options);
        Assert.NotNull(command.Options._dynamic);
        expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(_groupKey.Name);
        Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
        threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
        Assert.NotNull(threadOptions);
        Assert.NotNull(threadOptions._dynamic);
        Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

        services = new ServiceCollection();
        services.AddHystrixCommand<DummyCommand>("GroupKey", config);
        provider = services.BuildServiceProvider();
        command = provider.GetService<DummyCommand>();
        Assert.NotNull(command);
        Assert.Equal("GroupKey", command.CommandGroup.Name);
        expectedCommandKey = HystrixCommandKeyDefault.AsKey(nameof(DummyCommand));
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
        services.AddHystrixCommand<IDummyCommand, DummyCommand>("GroupKey", config);
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
        services.AddHystrixCommand<DummyCommand>(_groupKey, _commandKey, config);
        provider = services.BuildServiceProvider();
        command = provider.GetService<DummyCommand>();
        Assert.NotNull(command);
        Assert.Equal(_groupKey, command.CommandGroup);
        Assert.Equal(_commandKey, command.CommandKey);
        Assert.NotNull(command.Options);
        Assert.NotNull(command.Options._dynamic);
        expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(_groupKey.Name);
        Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
        threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
        Assert.NotNull(threadOptions);
        Assert.NotNull(threadOptions._dynamic);
        Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

        services = new ServiceCollection();
        config = new ConfigurationBuilder().Build();
        services.AddHystrixCommand<IDummyCommand, DummyCommand>(_groupKey, _commandKey, config);
        provider = services.BuildServiceProvider();
        icommand = provider.GetService<IDummyCommand>();
        Assert.NotNull(icommand);
        command = icommand as DummyCommand;
        Assert.NotNull(command);
        Assert.Equal(_groupKey, command.CommandGroup);
        Assert.Equal(_commandKey, command.CommandKey);
        Assert.NotNull(command.Options);
        Assert.NotNull(command.Options._dynamic);
        expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(_groupKey.Name);
        Assert.Equal(expectedThreadPoolKey, command.Options.ThreadPoolKey);
        threadOptions = command.Options.ThreadPoolOptions as HystrixThreadPoolOptions;
        Assert.NotNull(threadOptions);
        Assert.NotNull(threadOptions._dynamic);
        Assert.Equal(expectedThreadPoolKey, threadOptions.ThreadPoolKey);

        services = new ServiceCollection();
        services.AddHystrixCommand<DummyCommand>("GroupKey", "CommandKey", config);
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
        services.AddHystrixCommand<IDummyCommand, DummyCommand>("GroupKey", "CommandKey", config);
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
        var appSettings = new Dictionary<string, string>
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
        services.AddHystrixCommand<DummyCommand>(_groupKey, config);
        var provider = services.BuildServiceProvider();
        var command = provider.GetService<DummyCommand>();
        Assert.NotNull(command);
        Assert.Equal(_groupKey, command.CommandGroup);
        var expectedCommandKey = HystrixCommandKeyDefault.AsKey(nameof(DummyCommand));
        Assert.Equal(expectedCommandKey, command.CommandKey);
        Assert.NotNull(command.Options);
        Assert.NotNull(command.Options._dynamic);
        var expectedThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(_groupKey.Name);
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
