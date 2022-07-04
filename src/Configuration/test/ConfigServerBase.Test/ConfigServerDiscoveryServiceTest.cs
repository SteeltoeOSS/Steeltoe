// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerDiscoveryServiceTest
{
    [Fact]
    public void ThrowsOnNulls()
    {
        var config = new ConfigurationBuilder().Build();
        var settings = new ConfigServerClientSettings();
        var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerDiscoveryService(null, settings));
        var ex2 = Assert.Throws<ArgumentNullException>(() => new ConfigServerDiscoveryService(config, null));
        Assert.Equal("configuration", ex.ParamName);
        Assert.Equal("settings", ex2.ParamName);
    }

    [Fact]
    public void ConfigServerDiscoveryService_FindsDiscoveryClient()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" }
        };
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        var config = builder.Build();
        var settings = new ConfigServerClientSettings();
        var logFactory = new LoggerFactory();

        var service = new ConfigServerDiscoveryService(config, settings, logFactory);
        Assert.NotNull(service.DiscoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(service.DiscoveryClient);
    }

    [Fact]
    public void InvokeGetInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        var config = builder.Build();
        var settings = new ConfigServerClientSettings();

        var service = new ConfigServerDiscoveryService(config, settings);
        var result = service.GetConfigServerInstances();
        Assert.Empty(result);
    }

    [Fact]
    public void InvokeGetInstances_RetryEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "1" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var settings = new ConfigServerClientSettings
        {
            RetryEnabled = true,
            Timeout = 10,
            RetryAttempts = 1
        };
        var service = new ConfigServerDiscoveryService(config, settings);
        var result = service.GetConfigServerInstances();
        Assert.Empty(result);
    }

    [Fact]
    public void GetConfigServerInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        var config = builder.Build();
        var settings = new ConfigServerClientSettings { RetryEnabled = false, Timeout = 10 };

        var service = new ConfigServerDiscoveryService(config, settings);
        var result = service.GetConfigServerInstances();
        Assert.Empty(result);
    }

    [Fact]
    public void GetConfigServerInstancesCatchesDiscoveryExceptions()
    {
        var appSettings = new Dictionary<string, string> { { "testdiscovery:enabled", "true" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(config, new ConfigServerClientSettings());

        // act - the test discovery client throws on GetInstances()
        var result = service.GetConfigServerInstances();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var appSettings = new Dictionary<string, string> { { "eureka:client:anything", "true" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var testDiscoveryClient = new TestDiscoveryClient();
        var logFactory = new LoggerFactory();
        var service = new ConfigServerDiscoveryService(config, new ConfigServerClientSettings());
        Assert.IsType<EurekaDiscoveryClient>(service.DiscoveryClient);

        // replace the bootstrapped eureka client with a test client
        await service.ProvideRuntimeReplacementsAsync(testDiscoveryClient, logFactory);
        service.DiscoveryClient.Should().Be(testDiscoveryClient);
        service.LogFactory.Should().NotBeNull();
    }

    [Fact]
    public async Task RuntimeReplacementsShutdownInitialDiscoveryClient()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var replacementDiscoveryClient = new TestDiscoveryClient();
        var appSettings = new Dictionary<string, string> { { "testdiscovery:enabled", "true" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(config, new ConfigServerClientSettings());
        var originalClient = service.DiscoveryClient as TestDiscoveryClient;

        // replace the bootstrapped eureka client with a test client
        await service.ProvideRuntimeReplacementsAsync(replacementDiscoveryClient, null);

        Assert.True(originalClient.HasShutdown, "ShutdownAsync() called on original discovery client.");
        Assert.False(replacementDiscoveryClient.HasShutdown, "ShutdownAsync() NOT called on replacement discovery client.");
    }

    [Fact]
    public async Task ShutdownAsyncShutsDownOriginalDiscoveryClient()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var appSettings = new Dictionary<string, string> { { "testdiscovery:enabled", "true" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(config, new ConfigServerClientSettings());
        var originalClient = service.DiscoveryClient as TestDiscoveryClient;

        // replace the bootstrapped eureka client with a test client
        await service.ShutdownAsync();

        Assert.True(originalClient.HasShutdown, "ShutdownAsync() called on original discovery client.");
    }
}
