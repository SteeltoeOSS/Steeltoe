// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonInstanceInfoTest
{
    [Fact]
    public void Serialize()
    {
        var instanceInfo = new JsonInstanceInfo
        {
            InstanceId = "localhost:foo",
            HostName = "localhost",
            AppName = "FOO",
            IPAddress = "192.168.56.1",
            Status = InstanceStatus.Up,
            OverriddenStatus = InstanceStatus.OutOfService,
            OverriddenStatusLegacy = InstanceStatus.Down,
            Port = new JsonPortWrapper
            {
                Enabled = true,
                Port = 8080
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = false,
                Port = 443
            },
            CountryId = 1,
            DataCenterInfo = new JsonDataCenterInfo
            {
                ClassName = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                Name = "MyOwn"
            },
            LeaseInfo = new JsonLeaseInfo
            {
                RenewalIntervalInSeconds = 30,
                DurationInSeconds = 90,
                RegistrationTimestamp = 1_457_714_988_223,
                LastRenewalTimestamp = 1_457_716_158_319,
                EvictionTimestamp = 1_457_715_134_123,
                ServiceUpTimestamp = 1_457_714_988_223
            },
            Metadata = new Dictionary<string, string?>
            {
                ["@class"] = "java.util.Collections$EmptyMap"
            },
            HomePageUrl = "http://localhost:8080/",
            StatusPageUrl = "http://localhost:8080/info",
            HealthCheckUrl = "http://localhost:8080/health",
            VipAddress = "foo",
            IsCoordinatingDiscoveryServer = false,
            LastUpdatedTimestamp = 1_457_714_988_223,
            LastDirtyTimestamp = 1_457_714_988_172,
            ActionType = ActionType.Added
        };

        string result = JsonSerializer.Serialize(instanceInfo, EurekaJsonSerializerContext.Default.JsonInstanceInfo);

        result.Should().BeJson("""
            {
              "instanceId": "localhost:foo",
              "app": "FOO",
              "ipAddr": "192.168.56.1",
              "port": {
                "@enabled": "true",
                "$": 8080
              },
              "securePort": {
                "@enabled": "false",
                "$": 443
              },
              "homePageUrl": "http://localhost:8080/",
              "statusPageUrl": "http://localhost:8080/info",
              "healthCheckUrl": "http://localhost:8080/health",
              "vipAddress": "foo",
              "countryId": 1,
              "dataCenterInfo": {
                "@class": "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                "name": "MyOwn"
              },
              "hostName": "localhost",
              "status": "UP",
              "overriddenStatus": "OUT_OF_SERVICE",
              "overriddenstatus": "DOWN",
              "leaseInfo": {
                "renewalIntervalInSecs": 30,
                "durationInSecs": 90,
                "registrationTimestamp": "1457714988223",
                "lastRenewalTimestamp": "1457716158319",
                "evictionTimestamp": "1457715134123",
                "serviceUpTimestamp": "1457714988223"
              },
              "isCoordinatingDiscoveryServer": "false",
              "metadata": {
                "@class": "java.util.Collections$EmptyMap"
              },
              "lastUpdatedTimestamp": "1457714988223",
              "lastDirtyTimestamp": "1457714988172",
              "actionType": "ADDED"
            }
            """);
    }

    [Fact]
    public void Deserialize()
    {
        const string json = """
            {
              "instanceId": "localhost:foo",
              "hostName": "localhost",
              "app": "FOO",
              "ipAddr": "192.168.56.1",
              "status": "UP",
              "overriddenStatus": "OUT_OF_SERVICE",
              "overriddenstatus": "DOWN",
              "port": {
                "$": 8080,
                "@enabled": "true"
              },
              "securePort": {
                "$": 443,
                "@enabled": "false"
              },
              "countryId": 1,
              "dataCenterInfo": {
                "@class": "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
                "name": "MyOwn"
              },
              "leaseInfo": {
                "renewalIntervalInSecs": 30,
                "durationInSecs": 90,
                "registrationTimestamp": 1457714988223,
                "lastRenewalTimestamp": 1457716158319,
                "evictionTimestamp": 1457715134123,
                "serviceUpTimestamp": 1457714988223
              },
              "metadata": {
                "@class": "java.util.Collections$EmptyMap"
              },
              "homePageUrl": "http://localhost:8080/",
              "statusPageUrl": "http://localhost:8080/info",
              "healthCheckUrl": "http://localhost:8080/health",
              "vipAddress": "foo",
              "isCoordinatingDiscoveryServer": "false",
              "lastUpdatedTimestamp": "1457714988223",
              "lastDirtyTimestamp": "1457714988172",
              "actionType": "ADDED"
            }
            """;

        JsonInstanceInfo? result = JsonSerializer.Deserialize(json, EurekaJsonSerializerContext.Default.JsonInstanceInfo);

        result.Should().NotBeNull();
        result.InstanceId.Should().Be("localhost:foo");
        result.HostName.Should().Be("localhost");
        result.AppName.Should().Be("FOO");
        result.IPAddress.Should().Be("192.168.56.1");
        result.Status.Should().Be(InstanceStatus.Up);
        result.OverriddenStatus.Should().Be(InstanceStatus.OutOfService);
        result.OverriddenStatusLegacy.Should().Be(InstanceStatus.Down);
        result.Port.Should().NotBeNull();
        result.Port.Enabled.Should().BeTrue();
        result.Port.Port.Should().Be(8080);
        result.SecurePort.Should().NotBeNull();
        result.SecurePort.Enabled.Should().BeFalse();
        result.SecurePort.Port.Should().Be(443);
        result.CountryId.Should().Be(1);
        result.DataCenterInfo.Should().NotBeNull();
        result.DataCenterInfo.ClassName.Should().Be("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo");
        result.DataCenterInfo.Name.Should().Be("MyOwn");
        result.LeaseInfo.Should().NotBeNull();
        result.LeaseInfo.RenewalIntervalInSeconds.Should().Be(30);
        result.LeaseInfo.DurationInSeconds.Should().Be(90);
        result.LeaseInfo.RegistrationTimestamp.Should().Be(1_457_714_988_223);
        result.LeaseInfo.LastRenewalTimestamp.Should().Be(1_457_716_158_319);
        result.LeaseInfo.EvictionTimestamp.Should().Be(1_457_715_134_123);
        result.LeaseInfo.ServiceUpTimestamp.Should().Be(1_457_714_988_223);
        result.Metadata.Should().ContainSingle();
        result.Metadata.Should().ContainKey("@class").WhoseValue.Should().Be("java.util.Collections$EmptyMap");
        result.HomePageUrl.Should().Be("http://localhost:8080/");
        result.StatusPageUrl.Should().Be("http://localhost:8080/info");
        result.HealthCheckUrl.Should().Be("http://localhost:8080/health");
        result.VipAddress.Should().Be("foo");
        result.IsCoordinatingDiscoveryServer.Should().BeFalse();
        result.LastUpdatedTimestamp.Should().Be(1_457_714_988_223);
        result.LastDirtyTimestamp.Should().Be(1_457_714_988_172);
        result.ActionType.Should().Be(ActionType.Added);
    }
}
