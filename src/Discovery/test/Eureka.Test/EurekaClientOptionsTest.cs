// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaClientOptionsTest
{
    [Fact]
    public void Constructor_Initializes_Defaults()
    {
        var clientOptions = new EurekaClientOptions();

        clientOptions.Enabled.Should().BeTrue();
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(30);
        clientOptions.EurekaServer.ProxyHost.Should().BeNull();
        clientOptions.EurekaServer.ProxyPort.Should().Be(0);
        clientOptions.EurekaServer.ProxyUserName.Should().BeNull();
        clientOptions.EurekaServer.ProxyPassword.Should().BeNull();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(EurekaServerOptions.DefaultConnectTimeoutSeconds);
        clientOptions.ShouldRegisterWithEureka.Should().BeTrue();
        clientOptions.IsFetchDeltaDisabled.Should().BeFalse();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeTrue();
        clientOptions.RegistryRefreshSingleVipAddress.Should().BeNull();
        clientOptions.EurekaServerServiceUrls.Should().Be("http://localhost:8761/eureka/");
        clientOptions.Health.Should().NotBeNull();
        clientOptions.Health.ContributorEnabled.Should().BeTrue();
        clientOptions.Health.CheckEnabled.Should().BeFalse();
        clientOptions.Health.MonitoredApps.Should().BeNull();
    }

    [Fact]
    public void Constructor_ConfiguresEurekaDiscovery_Correctly()
    {
        const string appSettings = """
            {
              "eureka": {
                "client": {
                  "eurekaServer": {
                    "proxyHost": "proxyHost",
                    "proxyPort": 100,
                    "proxyUserName": "proxyUserName",
                    "proxyPassword": "proxyPassword",
                    "shouldGZipContent": true,
                    "connectTimeoutSeconds": 100
                  },
                  "allowRedirects": true,
                  "shouldDisableDelta": true,
                  "shouldFilterOnlyUpInstances": true,
                  "shouldFetchRegistry": true,
                  "registryRefreshSingleVipAddress": "registryRefreshSingleVipAddress",
                  "shouldRegisterWithEureka": true,
                  "registryFetchIntervalSeconds": 100,
                  "instanceInfoReplicationIntervalSeconds": 100,
                  "serviceUrl": "https://foo.bar:8761/eureka/",
                  "Health": {
                    "CheckEnabled": "true"
                  }
                },
                "instance": {
                  "registrationMethod": "foobar",
                  "hostName": "myHostName",
                  "instanceId": "instanceId",
                  "appName": "appName",
                  "appGroup": "appGroup",
                  "instanceEnabledOnInit": true,
                  "port": 100,
                  "securePort": 100,
                  "nonSecurePortEnabled": true,
                  "securePortEnabled": true,
                  "leaseExpirationDurationInSeconds": 100,
                  "leaseRenewalIntervalInSeconds": 100,
                  "secureVipAddress": "secureVipAddress",
                  "vipAddress": "vipAddress",
                  "asgName": "asgName",
                  "metadataMap": {
                    "foo": "bar",
                    "bar": "foo"
                  },
                  "statusPageUrlPath": "statusPageUrlPath",
                  "statusPageUrl": "statusPageUrl",
                  "homePageUrlPath": "homePageUrlPath",
                  "homePageUrl": "homePageUrl",
                  "healthCheckUrlPath": "healthCheckUrlPath",
                  "healthCheckUrl": "healthCheckUrl",
                  "secureHealthCheckUrl": "secureHealthCheckUrl"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        IConfigurationSection clientSection = configuration.GetSection(EurekaClientOptions.ConfigurationPrefix);
        var clientOptions = new EurekaClientOptions();
        clientSection.Bind(clientOptions);

        clientOptions.EurekaServer.ProxyHost.Should().Be("proxyHost");
        clientOptions.EurekaServer.ProxyPort.Should().Be(100);
        clientOptions.EurekaServer.ProxyPassword.Should().Be("proxyPassword");
        clientOptions.EurekaServer.ProxyUserName.Should().Be("proxyUserName");
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(100);
        clientOptions.EurekaServerServiceUrls.Should().Be("https://foo.bar:8761/eureka/");
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(100);
        clientOptions.RegistryRefreshSingleVipAddress.Should().Be("registryRefreshSingleVipAddress");
        clientOptions.IsFetchDeltaDisabled.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeTrue();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.ShouldRegisterWithEureka.Should().BeTrue();
        clientOptions.Health.Should().NotBeNull();
        clientOptions.Health.ContributorEnabled.Should().BeTrue();
        clientOptions.Health.CheckEnabled.Should().BeTrue();
        clientOptions.Health.MonitoredApps.Should().BeNull();
    }

    [Fact]
    public async Task Client_EnabledByDefault()
    {
        var services = new ServiceCollection();
        services.AddEurekaDiscoveryClient();

        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:serviceUrl"] = "http://testhost/eureka"
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        clientOptions.Value.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task ClientDisabledBySpringCloudDiscovery_EnabledFalse()
    {
        var services = new ServiceCollection();
        services.AddEurekaDiscoveryClient();

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:discovery:enabled"] = "false",
            ["eureka:client:serviceUrl"] = "http://testhost/eureka"
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        clientOptions.Value.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task ClientOptionsValidation_SucceedsWhenEurekaIsTurnedOff()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Action action = () => _ = clientOptions.Value;

        action.Should().NotThrow();
    }

    [Fact]
    public async Task ClientOptionsValidation_FailsWhenServiceUrlIsNotProvided()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:ServiceUrl"] = " "
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Action action = () => _ = clientOptions.Value;

        action.Should().ThrowExactly<OptionsValidationException>().WithMessage("Eureka Service URL must be provided.");
    }

    [Fact]
    public async Task ClientOptionsValidation_FailsWhenServiceUrlsAreInvalid()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:ServiceUrl"] = "http://server,/not-fully-qualified,https://eureka.com,broken"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Action action = () => _ = clientOptions.Value;

        OptionsValidationException exception = action.Should().ThrowExactly<OptionsValidationException>().Which;
        exception.Failures.Should().ContainSingle(message => message == "Eureka URL '/not-fully-qualified' is invalid.");
        exception.Failures.Should().ContainSingle(message => message == "Eureka URL 'broken' is invalid.");
    }

    [Fact]
    public async Task ClientOptionsValidation_FailsWhenRunningInCloudWithLocalhost()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        Action action = () => _ = clientOptions.Value;

        action.Should().ThrowExactly<OptionsValidationException>().WithMessage(
            "Eureka URL 'http://localhost:8761/eureka/' is not valid in containerized or cloud environments. " +
            "Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.");
    }

    [Fact]
    public async Task Client_FavorsEurekaClientEnabled()
    {
        var services = new ServiceCollection();
        services.AddEurekaDiscoveryClient();

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:discovery:enabled"] = "false",
            ["eureka:client:enabled"] = "true",
            ["eureka:client:serviceUrl"] = "http://testhost/eureka"
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();

        clientOptions.Value.Enabled.Should().BeTrue();
    }
}
