// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientExtensionTest
{
    [Fact]
    public void ClientEnabledByDefault()
    {
        var services = new ServiceCollection();
        var extension = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string?>
        {
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        extension.ConfigureEurekaServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientDisabledBySpringCloudDiscoveryEnabledFalse()
    {
        var services = new ServiceCollection();
        var extension = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string?>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        extension.ConfigureEurekaServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.False(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientFavorsEurekaClientEnabled()
    {
        var services = new ServiceCollection();
        var extension = new EurekaDiscoveryClientExtension();

        var appSettings = new Dictionary<string, string?>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "eureka:client:enabled", "true" },
            { "eureka:client:serviceurl", "http://testhost/eureka" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        extension.ConfigureEurekaServices(services);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public async Task CustomDelegatingHandlerCanBeAdded()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Eureka:Client:ServiceUrl", "https://www.google.com" },
            { "Eureka:Client:EurekaServer:ConnectTimeoutSeconds", "1" },
            { "Eureka:Client:EurekaServer:RetryCount", "1" }
        };

        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        services.AddSingleton<IConfiguration>(configuration);

        var pluggableHandler = new PluggableDelegatingHandler();
        services.AddSingleton(pluggableHandler);

        services.Configure<HttpClientFactoryOptions>("Eureka", options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<PluggableDelegatingHandler>()));
        });

        services.AddServiceDiscovery(configuration, builder => builder.UseEureka());

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var eurekaHttpClient = serviceProvider.GetRequiredService<EurekaHttpClient>();

        EurekaHttpResponse<Applications> response = await eurekaHttpClient.GetApplicationsAsync(CancellationToken.None);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        pluggableHandler.WasCalled.Should().BeTrue();
    }

    private sealed class PluggableDelegatingHandler : DelegatingHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
