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

        registration.InstanceId.Should().Be("id");
        registration.ServiceId.Should().Be("name");
        registration.Host.Should().Be("address");
        registration.Port.Should().Be(1234);
        registration.Tags.Should().Contain("tag1");
        registration.Tags.Should().Contain("tag2");
        registration.Metadata.Should().ContainSingle();
        registration.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        registration.IsSecure.Should().BeFalse();
        registration.Uri.Should().Be(new Uri("http://address:1234"));
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

        registration.Tags.Should().HaveCount(2);
        registration.Tags.Should().Contain("foo");
        registration.Tags.Should().Contain("bar");
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

        metadata.Should().HaveCount(5);
        metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        metadata.Should().ContainKey("baz").WhoseValue.Should().Be("qux");
        metadata.Should().ContainKey("zone").WhoseValue.Should().Be("instanceZone");
        metadata.Should().ContainKey("group").WhoseValue.Should().Be("instanceGroup");
        metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
    }

    [Fact]
    public void AppName_SetAsExpected()
    {
        var appSettings = new Dictionary<string, string?>();

        // default value is assembly name
        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        registration.ServiceId.Should().Be(Assembly.GetEntryAssembly()!.GetName().Name!.Replace('.', '-'));

        // followed by spring:application:name
        appSettings.Add("spring:application:name", "SpringApplicationName");
        registration = TestRegistrationFactory.Create(appSettings);

        registration.ServiceId.Should().Be("SpringApplicationName");

        // Consul-discovery is the highest priority
        appSettings.Add("consul:discovery:serviceName", "ConsulDiscoveryServiceName");
        registration = TestRegistrationFactory.Create(appSettings);

        registration.ServiceId.Should().Be("ConsulDiscoveryServiceName");
    }

    [Fact]
    public void GetDefaultInstanceId_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "serviceName"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        registration.InstanceId.Should().StartWith("serviceName-");
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

        registration.InstanceId.Should().Be("instanceId");

        appSettings.Remove("consul:discovery:instanceId");
        registration = TestRegistrationFactory.Create(appSettings);

        registration.InstanceId.Should().StartWith("foobar-");
    }

    [Fact]
    public void CreateCheck_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            HostName = "some-host"
        };

        AgentServiceCheck result = ConsulRegistration.CreateCheck(1234, options);

        result.Should().NotBeNull();
        result.TTL.Should().Be(DateTimeConversions.ToTimeSpan(options.Heartbeat!.TimeToLive));
        result.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));

        options.Heartbeat = null;
        const int port = 1234;
        result = ConsulRegistration.CreateCheck(port, options);
        var uri = new Uri($"{options.Scheme}://{options.HostName}:{port}{options.HealthCheckPath}");

        result.HTTP.Should().Be(uri.ToString());
        result.Interval.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval!));
        result.Timeout.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout!));
        result.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));
        result.TLSSkipVerify.Should().Be(options.HealthCheckTlsSkipVerify);
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

        registration.InstanceId.Should().StartWith("foobar-");
        registration.IsSecure.Should().BeFalse();
        registration.ServiceId.Should().Be("foobar");
        registration.Host.Should().Be("some-host");
        registration.Port.Should().Be(1100);
        registration.Uri.Should().Be(new Uri("http://some-host:1100"));

        registration.InnerRegistration.Should().NotBeNull();
        registration.InnerRegistration.Address.Should().Be("some-host");
        registration.InnerRegistration.ID.Should().StartWith("foobar-");
        registration.InnerRegistration.Name.Should().Be("foobar");
        registration.InnerRegistration.Port.Should().Be(1100);
        registration.InnerRegistration.Check.Should().NotBeNull();
        registration.InnerRegistration.Tags.Should().NotBeNull();
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

        check.HTTP.Should().Contain(path);
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

        check.HTTP.Should().BeNull();
    }
}
