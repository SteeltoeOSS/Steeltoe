// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Consul.Util;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulRegistrationTest
{
    [Fact]
    public void Constructor_ThrowsOnNulls()
    {
        var registration = new AgentServiceRegistration();
        var options = new ConsulDiscoveryOptions();

        Assert.Throws<ArgumentNullException>(() => new ConsulRegistration(null, options));
        Assert.Throws<ArgumentNullException>(() => new ConsulRegistration(registration, (ConsulDiscoveryOptions)null));
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var registration = new AgentServiceRegistration
        {
            ID = "id",
            Name = "name",
            Address = "address",
            Port = 1234,
            Tags = new[]
            {
                "tag1",
                "tag2"
            },
            Meta = new Dictionary<string, string>
            {
                ["foo"] = "bar"
            }
        };

        var options = new ConsulDiscoveryOptions();
        var reg = new ConsulRegistration(registration, options);
        Assert.Equal("id", reg.InstanceId);
        Assert.Equal("name", reg.ServiceId);
        Assert.Equal("address", reg.Host);
        Assert.Equal(1234, reg.Port);
        Assert.Contains("tag1", reg.Tags);
        Assert.Contains("tag2", reg.Tags);
        Assert.Single(reg.Metadata);
        Assert.Contains("foo", reg.Metadata.Keys);
        Assert.Contains("bar", reg.Metadata.Values);
        Assert.False(reg.IsSecure);
        Assert.Equal(new Uri("http://address:1234"), reg.Uri);
    }

    [Fact]
    public void CreateTags_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Tags = new List<string>
            {
                "foo",
                "bar"
            },
            InstanceZone = "instancezone",
            InstanceGroup = "instancegroup",
            Scheme = "https"
        };

        string[] result = ConsulRegistration.CreateTags(options);
        Assert.Equal(2, result.Length);
        Assert.Contains("foo", result);
        Assert.Contains("bar", result);
    }

    [Fact]
    public void CreateMetadata_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["foo"] = "bar",
                ["baz"] = "qux"
            },
            InstanceZone = "instancezone",
            InstanceGroup = "instancegroup",
            Scheme = "https"
        };

        IDictionary<string, string> result = ConsulRegistration.CreateMetadata(options);
        Assert.Equal(5, result.Keys.Count);

        Assert.Contains(result, x => x.Key == "foo");
        Assert.Equal("bar", result["foo"]);

        Assert.Contains(result, x => x.Key == "baz");
        Assert.Equal("qux", result["baz"]);

        Assert.Contains(result, x => x.Key == "zone");
        Assert.Equal("instancezone", result["zone"]);

        Assert.Contains(result, x => x.Key == "group");
        Assert.Equal("instancegroup", result["group"]);

        Assert.Contains(result, x => x.Key == "secure");
        Assert.Equal("true", result["secure"]);
    }

    [Fact]
    public void AppName_SetAsExpected()
    {
        var options = new ConsulDiscoveryOptions();
        var appsettings = new Dictionary<string, string>();
        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        var appInstanceInfo = new ApplicationInstanceInfo(configuration);

        // default value is assembly name
        var result = ConsulRegistration.CreateRegistration(options, appInstanceInfo);
        Assert.Equal(TestHelpers.EntryAssemblyName.Replace('.', '-'), result.Service.Name);

        // followed by spring:application:name
        appsettings.Add("spring:application:name", "SpringApplicationName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.CreateRegistration(options, appInstanceInfo);
        Assert.Equal("SpringApplicationName", result.Service.Name);

        // Platform app name overrides spring name
        appsettings.Add("application:name", "PlatformName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.CreateRegistration(options, appInstanceInfo);
        Assert.Equal("PlatformName", result.Service.Name);

        // Consul-specific value beats generic value
        appsettings.Add("consul:serviceName", "ConsulServiceName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.CreateRegistration(options, appInstanceInfo);
        Assert.Equal("ConsulServiceName", result.Service.Name);

        // Consul-discovery is highest priority
        appsettings.Add("consul:discovery:serviceName", "ConsulDiscoveryServiceName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.CreateRegistration(options, appInstanceInfo);
        Assert.Equal("ConsulDiscoveryServiceName", result.Service.Name);
    }

    [Fact]
    public void GetDefaultInstanceId_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "consul:discovery:serviceName", "serviceName" }
        };

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        string result = ConsulRegistration.GetDefaultInstanceId(new ApplicationInstanceInfo(configuration));
        Assert.StartsWith("serviceName:", result, StringComparison.Ordinal);

        appsettings.Add("spring:application:instance_id", "springid");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        result = ConsulRegistration.GetDefaultInstanceId(new ApplicationInstanceInfo(configuration));
        Assert.Equal("serviceName:springid", result);

        appsettings.Add("vcap:application:instance_id", "vcapid");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        result = ConsulRegistration.GetDefaultInstanceId(new CloudFoundryApplicationOptions(configuration));
        Assert.Equal("serviceName:vcapid", result);
    }

    [Fact]
    public void GetInstanceId_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            InstanceId = "instanceId"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        }).Build();

        string result = ConsulRegistration.GetInstanceId(options, new ApplicationInstanceInfo(configurationRoot));
        Assert.Equal("instanceId", result);

        options.InstanceId = null;

        result = ConsulRegistration.GetInstanceId(options, new ApplicationInstanceInfo(configurationRoot));
        Assert.StartsWith("foobar-", result, StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizeForConsul_ReturnsExpected()
    {
        Assert.Equal("abc1", ConsulRegistration.NormalizeForConsul("abc1"));
        Assert.Equal("ab-c1", ConsulRegistration.NormalizeForConsul("ab:c1"));
        Assert.Equal("ab-c1", ConsulRegistration.NormalizeForConsul("ab::c1"));

        Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul("9abc"));
        Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul(":abc"));
        Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul("abc:"));
    }

    [Fact]
    public void CreateCheck_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions();
        AgentServiceCheck result = ConsulRegistration.CreateCheck(1234, options);
        Assert.NotNull(result);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.Heartbeat.Ttl), result.TTL);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout), result.DeregisterCriticalServiceAfter);

        options.Heartbeat = null;
        Assert.Throws<ArgumentOutOfRangeException>(() => ConsulRegistration.CreateCheck(0, options));

        const int port = 1234;
        result = ConsulRegistration.CreateCheck(port, options);
        var uri = new Uri($"{options.Scheme}://{options.HostName}:{port}{options.HealthCheckPath}");
        Assert.Equal(uri.ToString(), result.HTTP);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval), result.Interval);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout), result.Timeout);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout), result.DeregisterCriticalServiceAfter);
        Assert.Equal(options.HealthCheckTlsSkipVerify, result.TLSSkipVerify);
    }

    [Fact]
    public void CreateRegistration_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Port = 1100
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        }).Build();

        var reg = ConsulRegistration.CreateRegistration(options, new ApplicationInstanceInfo(configurationRoot));

        Assert.StartsWith("foobar-", reg.InstanceId, StringComparison.Ordinal);
        Assert.False(reg.IsSecure);
        Assert.Equal("foobar", reg.ServiceId);
        Assert.Equal(options.HostName, reg.Host);
        Assert.Equal(1100, reg.Port);
        string hostName = options.HostName;
        Assert.Equal(new Uri($"http://{hostName}:1100"), reg.Uri);
        Assert.NotNull(reg.Service);

        Assert.Equal(hostName, reg.Service.Address);
        Assert.StartsWith("foobar-", reg.Service.ID, StringComparison.Ordinal);
        Assert.Equal("foobar", reg.Service.Name);
        Assert.Equal(1100, reg.Service.Port);
        Assert.NotNull(reg.Service.Check);
        Assert.NotNull(reg.Service.Tags);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsDisabled_ThenShouldSetHttp()
    {
        const string path = "/my/custom/health";
        var options = new ConsulDiscoveryOptions
        {
            HealthCheckPath = path,
            Heartbeat = new ConsulHeartbeatOptions
            {
                Enabled = false
            }
        };

        var check = ConsulRegistration.CreateCheck(1234, options);

        Assert.Contains(path, check.HTTP, StringComparison.InvariantCulture);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsEnabled_ThenHttpShouldBeNull()
    {
        const string path = "/my/custom/health";

        var options = new ConsulDiscoveryOptions
        {
            HealthCheckPath = path,
            Heartbeat = new ConsulHeartbeatOptions
            {
                Enabled = true
            }
        };

        AgentServiceCheck check = ConsulRegistration.CreateCheck(1234, options);

        Assert.Null(check.HTTP);
    }

    [Fact]
    public void CreateCheck_WhenHeartbeatIsDisabledAndPortIsANegativeNumber_ThenShouldThrow()
    {
        var options = new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions
            {
                Enabled = false
            }
        };

        const int port = -1234;

        Assert.Throws<ArgumentException>(() => ConsulRegistration.CreateCheck(port, options));
    }
}
