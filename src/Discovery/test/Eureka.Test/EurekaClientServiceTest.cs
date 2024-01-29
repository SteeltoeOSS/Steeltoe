// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaClientServiceTest
{
    [Fact]
    public void ConfigureClientOptions_ConfiguresCorrectly()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        EurekaClientOptions options = EurekaClientService.ConfigureClientOptions(configurationRoot);
        Assert.Equal("https://foo.bar:8761/eureka/", options.EurekaServerServiceUrls);
        Assert.True(options.ShouldFetchRegistry);
        Assert.False(options.ShouldRegisterWithEureka);
    }

    [Fact]
    public void GetLookupClient_ConfiguresClient()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        EurekaClientOptions options = EurekaClientService.ConfigureClientOptions(configurationRoot);
        EurekaClientService.LookupClient lookupClient = EurekaClientService.GetLookupClient(options, NullLoggerFactory.Instance);
        Assert.NotNull(lookupClient);
        Assert.NotNull(lookupClient.ClientOptions);
        Assert.Equal("https://foo.bar:8761/eureka/", lookupClient.ClientOptions.EurekaServerServiceUrls);
        Assert.True(lookupClient.ClientOptions.ShouldFetchRegistry);
        Assert.False(lookupClient.ClientOptions.ShouldRegisterWithEureka);
        Assert.Null(lookupClient.HeartBeatTimer);
        Assert.Null(lookupClient.CacheRefreshTimer);
        Assert.NotNull(lookupClient.HttpClient);
    }

    [Fact]
    public async Task GetInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        IList<IServiceInstance> result =
            await EurekaClientService.GetInstancesAsync(configurationRoot, "testService", NullLoggerFactory.Instance, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetServices_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();
        IList<string> result = await EurekaClientService.GetServicesAsync(configurationRoot, NullLoggerFactory.Instance, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
