// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaInstanceOptionsTest
{
    [Fact]
    public void Constructor_Initializes_Defaults()
    {
        var instanceOptions = new EurekaInstanceOptions();

        instanceOptions.InstanceId.Should().BeNull();
        instanceOptions.AppName.Should().BeNull();
        instanceOptions.AppGroupName.Should().BeNull();
        instanceOptions.MetadataMap.Should().BeEmpty();
        instanceOptions.HostName.Should().BeNull();
        instanceOptions.IPAddress.Should().BeNull();
        instanceOptions.PreferIPAddress.Should().BeFalse();
        instanceOptions.VipAddress.Should().BeNull();
        instanceOptions.SecureVipAddress.Should().BeNull();
        instanceOptions.NonSecurePort.Should().BeNull();
        instanceOptions.IsNonSecurePortEnabled.Should().BeFalse();
        instanceOptions.SecurePort.Should().BeNull();
        instanceOptions.IsSecurePortEnabled.Should().BeFalse();
        instanceOptions.RegistrationMethod.Should().BeNull();
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(30);
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(90);
        instanceOptions.StatusPageUrlPath.Should().Be("/info");
        instanceOptions.StatusPageUrl.Should().BeNull();
        instanceOptions.HomePageUrlPath.Should().Be("/");
        instanceOptions.HomePageUrl.Should().BeNull();
        instanceOptions.HealthCheckUrlPath.Should().Be("/health");
        instanceOptions.HealthCheckUrl.Should().BeNull();
        instanceOptions.SecureHealthCheckUrl.Should().BeNull();
        instanceOptions.AutoScalingGroupName.Should().BeNull();
        instanceOptions.DataCenterInfo.Name.Should().Be(DataCenterName.MyOwn);
        instanceOptions.UseNetworkInterfaces.Should().BeFalse();
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
                  "serviceUrl": "http://localhost:8761/eureka/"
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

        IConfigurationSection instanceSection = configuration.GetSection(EurekaInstanceOptions.ConfigurationPrefix);
        var instanceOptions = new EurekaInstanceOptions();
        instanceSection.Bind(instanceOptions);

        instanceOptions.InstanceId.Should().Be("instanceId");
        instanceOptions.AppName.Should().Be("appName");
        instanceOptions.AppGroupName.Should().Be("appGroup");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(100);
        instanceOptions.SecurePort.Should().Be(100);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(100);
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(100);
        instanceOptions.SecureVipAddress.Should().Be("secureVipAddress");
        instanceOptions.VipAddress.Should().Be("vipAddress");
        instanceOptions.AutoScalingGroupName.Should().Be("asgName");

        instanceOptions.StatusPageUrlPath.Should().Be("statusPageUrlPath");
        instanceOptions.StatusPageUrl.Should().Be("statusPageUrl");
        instanceOptions.HomePageUrlPath.Should().Be("homePageUrlPath");
        instanceOptions.HomePageUrl.Should().Be("homePageUrl");
        instanceOptions.HealthCheckUrlPath.Should().Be("healthCheckUrlPath");
        instanceOptions.HealthCheckUrl.Should().Be("healthCheckUrl");
        instanceOptions.SecureHealthCheckUrl.Should().Be("secureHealthCheckUrl");
        instanceOptions.HostName.Should().Be("myHostName");
        instanceOptions.RegistrationMethod.Should().Be("foobar");

        instanceOptions.MetadataMap.Should().HaveCount(2);
        instanceOptions.MetadataMap.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instanceOptions.MetadataMap.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
    }
}
