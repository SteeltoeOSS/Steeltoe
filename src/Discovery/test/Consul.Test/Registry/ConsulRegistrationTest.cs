// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Consul;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Consul.Util;

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
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "some",
            ["consul:discovery:tags:0"] = "foo",
            ["consul:discovery:tags:1"] = "bar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        Assert.Equal(2, registration.Tags.Count);
        Assert.Contains("foo", registration.Tags);
        Assert.Contains("bar", registration.Tags);
    }

    [Fact]
    public void CreateMetadata_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "some",
            ["consul:discovery:metadata:foo"] = "bar",
            ["consul:discovery:metadata:baz"] = "qux",
            ["consul:discovery:instanceZone"] = "instanceZone",
            ["consul:discovery:instanceGroup"] = "instanceGroup",
            ["consul:discovery:scheme"] = "https"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        IReadOnlyDictionary<string, string?> metadata = registration.Metadata;

        Assert.Equal(5, metadata.Keys.Count());

        Assert.Contains(metadata, x => x.Key == "foo");
        Assert.Equal("bar", metadata["foo"]);

        Assert.Contains(metadata, x => x.Key == "baz");
        Assert.Equal("qux", metadata["baz"]);

        Assert.Contains(metadata, x => x.Key == "zone");
        Assert.Equal("instanceZone", metadata["zone"]);

        Assert.Contains(metadata, x => x.Key == "group");
        Assert.Equal("instanceGroup", metadata["group"]);

        Assert.Contains(metadata, x => x.Key == "secure");
        Assert.Equal("true", metadata["secure"]);
    }

    [Fact]
    public void AppName_SetAsExpected()
    {
        var appSettings = new Dictionary<string, string?>();

        // default value is assembly name
        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        Assert.Equal(Assembly.GetEntryAssembly()!.GetName().Name!.Replace('.', '-'), registration.ServiceId);

        // followed by spring:application:name
        appSettings.Add("spring:application:name", "SpringApplicationName");
        registration = TestRegistrationFactory.Create(appSettings);
        Assert.Equal("SpringApplicationName", registration.ServiceId);

        // Consul-discovery is the highest priority
        appSettings.Add("consul:discovery:serviceName", "ConsulDiscoveryServiceName");
        registration = TestRegistrationFactory.Create(appSettings);
        Assert.Equal("ConsulDiscoveryServiceName", registration.ServiceId);
    }

    [Fact]
    public void GetDefaultInstanceId_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "serviceName"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        Assert.StartsWith("serviceName-", registration.InstanceId, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInstanceId_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:instanceId"] = "instanceId",
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        Assert.Equal("instanceId", registration.InstanceId);

        appSettings.Remove("consul:discovery:instanceId");
        registration = TestRegistrationFactory.Create(appSettings);
        Assert.StartsWith("foobar-", registration.InstanceId, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateCheck_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            HostName = "some-host"
        };

        AgentServiceCheck result = ConsulRegistration.CreateCheck(1234, options);
        Assert.NotNull(result);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.Heartbeat!.TimeToLive), result.TTL);
        Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!), result.DeregisterCriticalServiceAfter);

        options.Heartbeat = null;
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
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar",
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1100"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        Assert.StartsWith("foobar-", registration.InstanceId, StringComparison.Ordinal);
        Assert.False(registration.IsSecure);
        Assert.Equal("foobar", registration.ServiceId);
        Assert.Equal("some-host", registration.Host);
        Assert.Equal(1100, registration.Port);
        Assert.Equal(new Uri("http://some-host:1100"), registration.Uri);

        Assert.NotNull(registration.InnerRegistration);
        Assert.Equal("some-host", registration.InnerRegistration.Address);
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
            HostName = "some-host",
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
}
