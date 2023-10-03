// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerDiscoveryServiceTest
{
    [Fact]
    public void ThrowsOnNulls()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        var settings = new ConfigServerClientSettings();
        var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerDiscoveryService(null, settings, null));
        var ex2 = Assert.Throws<ArgumentNullException>(() => new ConfigServerDiscoveryService(configurationRoot, null, null));
        Assert.Equal("configuration", ex.ParamName);
        Assert.Equal("settings", ex2.ParamName);
    }

    [Fact]
    public void ConfigServerDiscoveryService_FindsDiscoveryClient()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration);
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();
        var settings = new ConfigServerClientSettings();
        var logFactory = new LoggerFactory();

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, logFactory);
        Assert.NotNull(service.DiscoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(service.DiscoveryClient);
    }

    [Fact]
    public async Task InvokeGetInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();
        var settings = new ConfigServerClientSettings();

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, null);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokeGetInstances_RetryEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "1" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var settings = new ConfigServerClientSettings
        {
            RetryEnabled = true,
            Timeout = 10,
            RetryAttempts = 1
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, null);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfigServerInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        var settings = new ConfigServerClientSettings
        {
            RetryEnabled = false,
            Timeout = 10
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, null);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfigServerInstancesCatchesDiscoveryExceptions()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "testdiscovery:enabled", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(configurationRoot, new ConfigServerClientSettings(), null);

        // act - the test discovery client throws on GetInstances()
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var appSettings = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "eureka:client:anything", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var testDiscoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, new ConfigServerClientSettings(), null);
        Assert.IsType<EurekaDiscoveryClient>(service.DiscoveryClient);

        // replace the bootstrapped eureka client with a test client
        await service.ProvideRuntimeReplacementsAsync(testDiscoveryClient, CancellationToken.None);
        service.DiscoveryClient.Should().Be(testDiscoveryClient);
        service.LoggerFactory.Should().NotBeNull();
    }

    [Fact]
    public async Task RuntimeReplacementsShutdownInitialDiscoveryClient()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var replacementDiscoveryClient = new TestDiscoveryClient();

        var appSettings = new Dictionary<string, string>
        {
            { "testdiscovery:enabled", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(configurationRoot, new ConfigServerClientSettings(), null);
        var originalClient = (TestDiscoveryClient)service.DiscoveryClient;

        // replace the bootstrapped eureka client with a test client
        await service.ProvideRuntimeReplacementsAsync(replacementDiscoveryClient, CancellationToken.None);

        Assert.True(originalClient.HasShutdown, "ShutdownAsync() called on original discovery client.");
        Assert.False(replacementDiscoveryClient.HasShutdown, "ShutdownAsync() NOT called on replacement discovery client.");
    }

    [Fact]
    public async Task ShutdownAsyncShutsDownOriginalDiscoveryClient()
    {
        // arrange a basic ConfigServerDiscoveryService w/o logging
        var appSettings = new Dictionary<string, string>
        {
            { "testdiscovery:enabled", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var service = new ConfigServerDiscoveryService(configurationRoot, new ConfigServerClientSettings(), null);
        var originalClient = (TestDiscoveryClient)service.DiscoveryClient;

        // replace the bootstrapped eureka client with a test client
        await service.ShutdownAsync(CancellationToken.None);

        Assert.True(originalClient.HasShutdown, "ShutdownAsync() called on original discovery client.");
    }
}
