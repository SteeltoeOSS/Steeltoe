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
    public void Constructor_SetsProperties()
    {
        var source = new AgentServiceRegistration
        {
            ID = "id",
            Name = "name",
            Address = "address",
            Port = 1234,
            Tags =
            [
                "tag1",
                "tag2"
            ],
            Meta = new Dictionary<string, string>
            {
                ["foo"] = "bar"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var registration = new ConsulRegistration(source, optionsMonitor);

        Assert.Equal("id", registration.InstanceId);
        Assert.Equal("name", registration.ServiceId);
        Assert.Equal("address", registration.Host);
        Assert.Equal(1234, registration.Port);
        Assert.Contains("tag1", registration.Tags);
        Assert.Contains("tag2", registration.Tags);
        Assert.Single(registration.Metadata);
        Assert.Contains("foo", registration.Metadata.Keys);
        Assert.Contains("bar", registration.Metadata.Values);
        Assert.False(registration.IsSecure);
        Assert.Equal(new Uri("http://address:1234"), registration.Uri);
    }

    [Fact]
    public void CreateTags_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Tags =
            {
                "foo",
                "bar"
            },
            InstanceZone = "instancezone",
            InstanceGroup = "instancegroup",
            Scheme = "https"
        };

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(options);
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(new ConfigurationBuilder().Build()));
        IList<string> tags = registration.Tags;

        Assert.Equal(2, tags.Count);
        Assert.Contains("foo", tags);
        Assert.Contains("bar", tags);
    }

    [Fact]
    public void CreateMetadata_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Metadata =
            {
                ["foo"] = "bar",
                ["baz"] = "qux"
            },
            InstanceZone = "instancezone",
            InstanceGroup = "instancegroup",
            Scheme = "https"
        };

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(options);
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(new ConfigurationBuilder().Build()));
        IDictionary<string, string> metadata = registration.Metadata;

        Assert.Equal(5, metadata.Keys.Count);

        Assert.Contains(metadata, x => x.Key == "foo");
        Assert.Equal("bar", metadata["foo"]);

        Assert.Contains(metadata, x => x.Key == "baz");
        Assert.Equal("qux", metadata["baz"]);

        Assert.Contains(metadata, x => x.Key == "zone");
        Assert.Equal("instancezone", metadata["zone"]);

        Assert.Contains(metadata, x => x.Key == "group");
        Assert.Equal("instancegroup", metadata["group"]);

        Assert.Contains(metadata, x => x.Key == "secure");
        Assert.Equal("true", metadata["secure"]);
    }

    [Fact]
    public void AppName_SetAsExpected()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var appsettings = new Dictionary<string, string>();
        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        var appInstanceInfo = new ApplicationInstanceInfo(configuration);

        // default value is assembly name
        var result = ConsulRegistration.Create(optionsMonitor, appInstanceInfo);
        Assert.Equal(TestHelpers.EntryAssemblyName.Replace('.', '-'), result.ServiceId);

        // followed by spring:application:name
        appsettings.Add("spring:application:name", "SpringApplicationName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.Create(optionsMonitor, appInstanceInfo);
        Assert.Equal("SpringApplicationName", result.ServiceId);

        // Platform app name overrides spring name
        appsettings.Add("application:name", "PlatformName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.Create(optionsMonitor, appInstanceInfo);
        Assert.Equal("PlatformName", result.ServiceId);

        // Consul-specific value beats generic value
        appsettings.Add("consul:serviceName", "ConsulServiceName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.Create(optionsMonitor, appInstanceInfo);
        Assert.Equal("ConsulServiceName", result.ServiceId);

        // Consul-discovery is highest priority
        appsettings.Add("consul:discovery:serviceName", "ConsulDiscoveryServiceName");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        appInstanceInfo = new ApplicationInstanceInfo(configuration);
        result = ConsulRegistration.Create(optionsMonitor, appInstanceInfo);
        Assert.Equal("ConsulDiscoveryServiceName", result.ServiceId);
    }

    [Fact]
    public void GetDefaultInstanceId_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "consul:discovery:serviceName", "serviceName" }
        };

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));

        Assert.StartsWith("serviceName-", registration.InstanceId, StringComparison.Ordinal);

        appsettings.Add("spring:application:instance_id", "springid");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));
        Assert.Equal("serviceName-springid", registration.InstanceId);

        appsettings.Add("vcap:application:instance_id", "vcapid");
        configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        registration = ConsulRegistration.Create(optionsMonitor, new CloudFoundryApplicationOptions(configuration));
        Assert.Equal("serviceName-vcapid", registration.InstanceId);
    }

    [Fact]
    public void GetInstanceId_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            InstanceId = "instanceId"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        }).Build();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(options);
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configurationRoot));

        Assert.Equal("instanceId", registration.InstanceId);

        options.InstanceId = null;

        registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configurationRoot));
        Assert.StartsWith("foobar-", registration.InstanceId, StringComparison.Ordinal);
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
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.Heartbeat!.TimeToLive), result.TTL);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!), result.DeregisterCriticalServiceAfter);

        options.Heartbeat = null;
        Assert.Throws<ArgumentOutOfRangeException>(() => ConsulRegistration.CreateCheck(0, options));

        const int port = 1234;
        result = ConsulRegistration.CreateCheck(port, options);
        var uri = new Uri($"{options.Scheme}://{options.HostName}:{port}{options.HealthCheckPath}");
        Assert.Equal(uri.ToString(), result.HTTP);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval!), result.Interval);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout!), result.Timeout);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!), result.DeregisterCriticalServiceAfter);
        Assert.Equal(options.HealthCheckTlsSkipVerify, result.TLSSkipVerify);
    }

    [Fact]
    public void CreateRegistration_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Port = 1100
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        }).Build();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(options);
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configurationRoot));

        Assert.StartsWith("foobar-", registration.InstanceId, StringComparison.Ordinal);
        Assert.False(registration.IsSecure);
        Assert.Equal("foobar", registration.ServiceId);
        Assert.Equal(options.HostName, registration.Host);
        Assert.Equal(1100, registration.Port);
        Assert.Equal(new Uri($"http://{options.HostName}:1100"), registration.Uri);

        Assert.NotNull(registration.InnerRegistration);
        Assert.Equal(options.HostName, registration.InnerRegistration.Address);
        Assert.StartsWith("foobar-", registration.InnerRegistration.ID, StringComparison.Ordinal);
        Assert.Equal("foobar", registration.InnerRegistration.Name);
        Assert.Equal(1100, registration.InnerRegistration.Port);
        Assert.NotNull(registration.InnerRegistration.Check);
        Assert.NotNull(registration.InnerRegistration.Tags);
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

        AgentServiceCheck check = ConsulRegistration.CreateCheck(1234, options);

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

        Assert.Throws<ArgumentOutOfRangeException>(() => ConsulRegistration.CreateCheck(port, options));
    }
}
