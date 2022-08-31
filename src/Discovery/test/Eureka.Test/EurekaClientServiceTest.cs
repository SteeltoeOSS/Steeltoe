// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaClientServiceTest
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
        EurekaClientService.LookupClient lookupClient = EurekaClientService.GetLookupClient(options, null);
        Assert.NotNull(lookupClient);
        Assert.NotNull(lookupClient.ClientConfiguration);
        Assert.Equal("https://foo.bar:8761/eureka/", lookupClient.ClientConfiguration.EurekaServerServiceUrls);
        Assert.True(lookupClient.ClientConfiguration.ShouldFetchRegistry);
        Assert.False(lookupClient.ClientConfiguration.ShouldRegisterWithEureka);
        Assert.Null(lookupClient.HeartBeatTimer);
        Assert.Null(lookupClient.CacheRefreshTimer);
        Assert.NotNull(lookupClient.HttpClient);
    }

    [Fact]
    public void GetInstances_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();
        IList<IServiceInstance> result = EurekaClientService.GetInstances(configurationRoot, "testService");
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetServices_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "eureka:client:serviceUrl", "https://foo.bar:8761/eureka/" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();
        IList<string> result = EurekaClientService.GetServices(configurationRoot);
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
