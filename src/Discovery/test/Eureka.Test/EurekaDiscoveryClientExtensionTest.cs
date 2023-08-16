// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientExtensionTest
{
    [Fact]
    public void ClientEnabledByDefault()
    {
        var services = new ServiceCollection();
        var ext = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string>
        {
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        ext.ConfigureEurekaServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientDisabledBySpringCloudDiscoveryEnabledFalse()
    {
        var services = new ServiceCollection();
        var ext = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        ext.ConfigureEurekaServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.False(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientFavorsEurekaClientEnabled()
    {
        var services = new ServiceCollection();
        var ext = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "eureka:client:enabled", "true" },
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        ext.ConfigureEurekaServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void HttpClientHandlerProviderIsUsed()
    {
        var services = new ServiceCollection();
        var ext = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "eureka:client:enabled", "true" },
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddSingleton<IHttpClientHandlerProvider, TestClientHandlerProvider>();
        ext.ApplyServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Eureka");
        var handlerProvider = provider.GetRequiredService<IHttpClientHandlerProvider>();

        handlerProvider.Should().BeOfType(typeof(TestClientHandlerProvider));
        ((TestClientHandlerProvider)handlerProvider).Called.Should().BeTrue();
    }

    internal sealed class TestClientHandlerProvider : IHttpClientHandlerProvider
    {
        public bool Called { get; set; }

        public HttpClientHandler GetHttpClientHandler()
        {
            Called = true;
            return new HttpClientHandler();
        }
    }
}
