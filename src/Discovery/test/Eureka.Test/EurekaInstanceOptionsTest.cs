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

        Assert.Null(instanceOptions.InstanceId);
        Assert.Null(instanceOptions.AppName);
        Assert.Null(instanceOptions.AppGroupName);
        Assert.Empty(instanceOptions.MetadataMap);
        Assert.Null(instanceOptions.HostName);
        Assert.Null(instanceOptions.IPAddress);
        Assert.False(instanceOptions.PreferIPAddress);
        Assert.Null(instanceOptions.VipAddress);
        Assert.Null(instanceOptions.SecureVipAddress);
        Assert.Null(instanceOptions.NonSecurePort);
        Assert.False(instanceOptions.IsNonSecurePortEnabled);
        Assert.Null(instanceOptions.SecurePort);
        Assert.False(instanceOptions.IsSecurePortEnabled);
        Assert.Null(instanceOptions.RegistrationMethod);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(30, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal(90, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal("/info", instanceOptions.StatusPageUrlPath);
        Assert.Null(instanceOptions.StatusPageUrl);
        Assert.Equal("/", instanceOptions.HomePageUrlPath);
        Assert.Null(instanceOptions.HomePageUrl);
        Assert.Equal("/health", instanceOptions.HealthCheckUrlPath);
        Assert.Null(instanceOptions.HealthCheckUrl);
        Assert.Null(instanceOptions.SecureHealthCheckUrl);
        Assert.Null(instanceOptions.AutoScalingGroupName);
        Assert.Equal(DataCenterName.MyOwn, instanceOptions.DataCenterInfo.Name);
        Assert.False(instanceOptions.UseNetworkInterfaces);
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

        Assert.Equal("instanceId", instanceOptions.InstanceId);
        Assert.Equal("appName", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(100, instanceOptions.NonSecurePort);
        Assert.Equal(100, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(100, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal(100, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal("secureVipAddress", instanceOptions.SecureVipAddress);
        Assert.Equal("vipAddress", instanceOptions.VipAddress);
        Assert.Equal("asgName", instanceOptions.AutoScalingGroupName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);
        Assert.Equal("myHostName", instanceOptions.HostName);
        Assert.Equal("foobar", instanceOptions.RegistrationMethod);

        Assert.Equal(2, instanceOptions.MetadataMap.Count);
        Assert.Equal("bar", instanceOptions.MetadataMap["foo"]);
        Assert.Equal("foo", instanceOptions.MetadataMap["bar"]);
    }
}
