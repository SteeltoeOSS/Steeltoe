// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Utils.IO;
using System.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaClientOptionsTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Intializes_Defaults()
    {
        var opts = new EurekaClientOptions();
        Assert.True(opts.Enabled);
        Assert.Equal(EurekaClientConfig.Default_RegistryFetchIntervalSeconds, opts.RegistryFetchIntervalSeconds);
        Assert.Null(opts.ProxyHost);
        Assert.Equal(0, opts.ProxyPort);
        Assert.Null(opts.ProxyUserName);
        Assert.Null(opts.ProxyPassword);
        Assert.True(opts.ShouldGZipContent);
        Assert.Equal(EurekaClientConfig.Default_EurekaServerConnectTimeoutSeconds, opts.EurekaServerConnectTimeoutSeconds);
        Assert.True(opts.ShouldRegisterWithEureka);
        Assert.False(opts.ShouldDisableDelta);
        Assert.True(opts.ShouldFilterOnlyUpInstances);
        Assert.True(opts.ShouldFetchRegistry);
        Assert.Null(opts.RegistryRefreshSingleVipAddress);
        Assert.True(opts.ShouldOnDemandUpdateStatusChange);
        Assert.Equal(EurekaClientConfig.Default_ServerServiceUrl, opts.EurekaServerServiceUrls);
        Assert.NotNull(opts.Health);
        Assert.True(opts.Health.Enabled); // Health contrib enabled
        Assert.True(opts.Health.CheckEnabled); // Health check enabled
        Assert.Null(opts.Health.MonitoredApps);
    }

    [Fact]
    public void Constructor_ConfiguresEurekaDiscovery_Correctly()
    {
        var appsettings = @"
                {
                    ""eureka"": {
                        ""client"": {
                            ""eurekaServer"": {
                                ""proxyHost"": ""proxyHost"",
                                ""proxyPort"": 100,
                                ""proxyUserName"": ""proxyUserName"",
                                ""proxyPassword"": ""proxyPassword"",
                                ""shouldGZipContent"": true,
                                ""connectTimeoutSeconds"": 100
                            },
                            ""allowRedirects"": true,
                            ""shouldDisableDelta"": true,
                            ""shouldFilterOnlyUpInstances"": true,
                            ""shouldFetchRegistry"": true,
                            ""registryRefreshSingleVipAddress"":""registryRefreshSingleVipAddress"",
                            ""shouldOnDemandUpdateStatusChange"": true,
                            ""shouldRegisterWithEureka"": true,
                            ""registryFetchIntervalSeconds"": 100,
                            ""instanceInfoReplicationIntervalSeconds"": 100,
                            ""serviceUrl"": ""https://foo.bar:8761/eureka/""
                        },
                        ""instance"": {
                            ""registrationMethod"" : ""foobar"",
                            ""hostName"": ""myHostName"",
                            ""instanceId"": ""instanceId"",
                            ""appName"": ""appName"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""port"": 100,
                            ""securePort"": 100,
                            ""nonSecurePortEnabled"": true,
                            ""securePortEnabled"": true,
                            ""leaseExpirationDurationInSeconds"":100,
                            ""leaseRenewalIntervalInSeconds"": 100,
                            ""secureVipAddress"": ""secureVipAddress"",
                            ""vipAddress"": ""vipAddress"",
                            ""asgName"": ""asgName"",
                            ""metadataMap"": {
                                ""foo"": ""bar"",
                                ""bar"": ""foo""
                            },
                            ""statusPageUrlPath"": ""statusPageUrlPath"",
                            ""statusPageUrl"": ""statusPageUrl"",
                            ""homePageUrlPath"":""homePageUrlPath"",
                            ""homePageUrl"": ""homePageUrl"",
                            ""healthCheckUrlPath"": ""healthCheckUrlPath"",
                            ""healthCheckUrl"":""healthCheckUrl"",
                            ""secureHealthCheckUrl"":""secureHealthCheckUrl""   
                        }
                    }
                }";
        using var sandbox = new Sandbox();
        var path = sandbox.CreateFile("appsettings.json", appsettings);
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        var config = configurationBuilder.Build();

        var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
        var co = new EurekaClientOptions();
        clientSection.Bind(co);

        Assert.Equal("proxyHost", co.ProxyHost);
        Assert.Equal(100, co.ProxyPort);
        Assert.Equal("proxyPassword", co.ProxyPassword);
        Assert.Equal("proxyUserName", co.ProxyUserName);
        Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
        Assert.Equal("https://foo.bar:8761/eureka/", co.EurekaServerServiceUrls);
        Assert.Equal(100, co.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
        Assert.True(co.ShouldDisableDelta);
        Assert.True(co.ShouldFetchRegistry);
        Assert.True(co.ShouldFilterOnlyUpInstances);
        Assert.True(co.ShouldGZipContent);
        Assert.True(co.ShouldOnDemandUpdateStatusChange);
        Assert.True(co.ShouldRegisterWithEureka);
        Assert.NotNull(co.Health);
        Assert.True(co.Health.Enabled); // Health contrib enabled
        Assert.True(co.Health.CheckEnabled); // Health check enabled
        Assert.Null(co.Health.MonitoredApps);
    }
}
