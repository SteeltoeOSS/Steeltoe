// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrationTest
{
    [Fact]
    public void Payload_ContainsAllServiceDetails()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar",
            ["consul:discovery:instanceId"] = "foobar-1",
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1100",
            ["consul:discovery:scheme"] = "https",
            ["consul:discovery:tags:0"] = "tag1",
            ["consul:discovery:tags:1"] = "tag2",
            ["consul:discovery:metadata:foo"] = "bar",
            ["consul:discovery:instanceZone"] = "zone1",
            ["consul:discovery:instanceGroup"] = "group1"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceRegistration payload = registration.Payload;

        payload.ID.Should().Be("foobar-1");
        payload.Name.Should().Be("foobar");
        payload.Address.Should().Be("some-host");
        payload.Port.Should().Be(1100);
        payload.Tags.Should().Equal("tag1", "tag2");
        payload.Meta.Should().HaveCount(4);
        payload.Meta.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        payload.Meta.Should().ContainKey("zone").WhoseValue.Should().Be("zone1");
        payload.Meta.Should().ContainKey("group").WhoseValue.Should().Be("group1");
        payload.Meta.Should().ContainKey("secure").WhoseValue.Should().Be("true");

        // The check has its own tests below.
        payload.Check.Should().NotBeNull();
    }

    [Fact]
    public void Payload_ForAppWithoutPort_HasNoPort()
    {
        // For example a worker service, which does not listen on any port.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:useAspNetCoreUrls"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.Payload.Port.Should().Be(0);
    }

    [Theory]
    [InlineData("8080")]
    [InlineData(null)]
    public void TtlMode_WhenHeartbeatIsEnabled_RegistersTtlCheckAndHeartbeat(string? port)
    {
        // The heartbeat is enabled by default. A TTL check does not need to know the port of the app.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:port"] = port,
            ["consul:discovery:useAspNetCoreUrls"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.CheckMode.Should().Be(HealthCheckMode.Ttl);
        registration.Payload.Check.Should().NotBeNull();
        registration.Payload.Check.TTL.Should().NotBeNull();
        registration.Payload.Check.HTTP.Should().BeNull();
        registration.HeartbeatOptions.Should().NotBeNull();
        registration.SkipHttpCheckDueToUnknownPort.Should().BeFalse();
    }

    [Fact]
    public void HttpMode_WhenHeartbeatIsDisabledAndPortIsKnown_RegistersHttpCheckWithoutHeartbeat()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:port"] = "8080",
            ["consul:discovery:heartbeat:enabled"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.CheckMode.Should().Be(HealthCheckMode.Http);
        registration.Payload.Check.Should().NotBeNull();
        registration.Payload.Check.HTTP.Should().NotBeNull();
        registration.Payload.Check.TTL.Should().BeNull();
        registration.HeartbeatOptions.Should().BeNull();
        registration.SkipHttpCheckDueToUnknownPort.Should().BeFalse();
    }

    [Fact]
    public void NoneMode_WhenHealthCheckIsNotRegistered_RegistersNoCheckAndNoHeartbeat()
    {
        // The heartbeat setting keeps its default (enabled), but is meaningless without a health check.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:port"] = "8080",
            ["consul:discovery:registerHealthCheck"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.CheckMode.Should().Be(HealthCheckMode.None);
        registration.Payload.Check.Should().BeNull();
        registration.HeartbeatOptions.Should().BeNull();
        registration.SkipHttpCheckDueToUnknownPort.Should().BeFalse();
    }

    [Fact]
    public void NoneMode_WhenHttpCheckIsConfiguredButPortIsUnknown_RegistersNoCheckAndNoHeartbeat()
    {
        // The heartbeat is turned off to get an HTTP check, but the app does not know its own port.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:useAspNetCoreUrls"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.CheckMode.Should().Be(HealthCheckMode.None);
        registration.Payload.Check.Should().BeNull();
        registration.HeartbeatOptions.Should().BeNull();
        registration.SkipHttpCheckDueToUnknownPort.Should().BeTrue();
    }

    [Fact]
    public void NoneMode_WhenHealthCheckUrlIsSetButPortIsUnknown_RegistersNoCheck()
    {
        // The absolute URL does not make a difference: an HTTP check needs a service that has a port.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:healthCheckUrl"] = "https://health.example.com/foobar",
            ["consul:discovery:useAspNetCoreUrls"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.CheckMode.Should().Be(HealthCheckMode.None);
        registration.Payload.Check.Should().BeNull();
        registration.SkipHttpCheckDueToUnknownPort.Should().BeTrue();
    }

    [Fact]
    public void TtlCheck_UsesHeartbeatSettings()
    {
        // A health check path is configured, but ignored because the heartbeat is enabled.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:healthCheckPath"] = "/my/custom/health",
            ["consul:discovery:healthCheckCriticalTimeout"] = "10m",
            ["consul:discovery:heartbeat:ttlValue"] = "5",
            ["consul:discovery:heartbeat:ttlUnit"] = "s"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceCheck check = registration.Payload.Check;

        check.TTL.Should().Be(TimeSpan.FromSeconds(5));
        check.DeregisterCriticalServiceAfter.Should().Be(TimeSpan.FromMinutes(10));
        check.HTTP.Should().BeNull();
        check.Header.Should().BeNull();
        check.Interval.Should().BeNull();
        check.Timeout.Should().BeNull();
        check.TLSSkipVerify.Should().BeFalse();
    }

    [Theory]
    [InlineData("500", "ms", 500)]
    [InlineData("30", "s", 30_000)]
    [InlineData("2", "m", 120_000)]
    [InlineData("1", "h", 3_600_000)]
    public void TtlCheck_UsesConfiguredTimeUnit(string ttlValue, string ttlUnit, int expectedMilliseconds)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:ttlValue"] = ttlValue,
            ["consul:discovery:heartbeat:ttlUnit"] = ttlUnit
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.Payload.Check.TTL.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
    }

    [Fact]
    public void TtlCheck_UsesDefaults()
    {
        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration([]);

        AgentServiceCheck check = registration.Payload.Check;

        check.TTL.Should().Be(TimeSpan.FromSeconds(30));
        check.DeregisterCriticalServiceAfter.Should().Be(TimeSpan.FromMinutes(30));
        check.HTTP.Should().BeNull();
        check.Header.Should().BeNull();
        check.Interval.Should().BeNull();
        check.Timeout.Should().BeNull();
        check.TLSSkipVerify.Should().BeFalse();
    }

    [Fact]
    public void TtlCheck_OmitsUnsetCriticalTimeout()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:healthCheckCriticalTimeout"] = string.Empty
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceCheck check = registration.Payload.Check;

        check.TTL.Should().Be(TimeSpan.FromSeconds(30));
        check.DeregisterCriticalServiceAfter.Should().BeNull();
    }

    [Fact]
    public void HttpCheck_UsesHealthCheckSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:healthCheckInterval"] = "7s",
            ["consul:discovery:healthCheckTimeout"] = "3s",
            ["consul:discovery:healthCheckCriticalTimeout"] = "10m",
            ["consul:discovery:healthCheckTlsSkipVerify"] = "true"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceCheck check = registration.Payload.Check;

        check.HTTP.Should().Be("http://some-host:1234/actuator/health");
        check.Header.Should().HaveCount(1);
        check.Header.Should().ContainKey("X-Use-Status-Code-From-Response").WhoseValue.Should().Equal("true");
        check.Interval.Should().Be(TimeSpan.FromSeconds(7));
        check.Timeout.Should().Be(TimeSpan.FromSeconds(3));
        check.DeregisterCriticalServiceAfter.Should().Be(TimeSpan.FromMinutes(10));
        check.TLSSkipVerify.Should().BeTrue();
        check.TTL.Should().BeNull();
    }

    [Fact]
    public void HttpCheck_UsesDefaults()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:heartbeat:enabled"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceCheck check = registration.Payload.Check;

        check.HTTP.Should().Be("http://some-host:1234/actuator/health");
        check.Header.Should().HaveCount(1);
        check.Header.Should().ContainKey("X-Use-Status-Code-From-Response").WhoseValue.Should().Equal("true");
        check.Interval.Should().Be(TimeSpan.FromSeconds(10));
        check.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        check.DeregisterCriticalServiceAfter.Should().Be(TimeSpan.FromMinutes(30));
        check.TLSSkipVerify.Should().BeFalse();
        check.TTL.Should().BeNull();
    }

    [Fact]
    public void HttpCheck_OmitsUnsetTimes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:healthCheckInterval"] = string.Empty,
            ["consul:discovery:healthCheckTimeout"] = string.Empty,
            ["consul:discovery:healthCheckCriticalTimeout"] = string.Empty
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        AgentServiceCheck check = registration.Payload.Check;

        check.HTTP.Should().Be("http://some-host:1234/actuator/health");
        check.Interval.Should().BeNull();
        check.Timeout.Should().BeNull();
        check.DeregisterCriticalServiceAfter.Should().BeNull();
    }

    [Fact]
    public void HttpCheck_UsesConfiguredScheme()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:scheme"] = "https",
            ["consul:discovery:heartbeat:enabled"] = "false"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.Payload.Check.HTTP.Should().Be("https://some-host:1234/actuator/health");
    }

    [Fact]
    public void HttpCheck_UsesCustomPath()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:healthCheckPath"] = "/my/custom/health"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.Payload.Check.HTTP.Should().Be("http://some-host:1234/my/custom/health");
    }

    [Fact]
    public void HttpCheck_UsesAbsoluteUrlInsteadOfPath()
    {
        // For example when the health endpoint is reachable through a gateway, on a different host and port than the app is registered with.
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1234",
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:healthCheckPath"] = "/my/custom/health",
            ["consul:discovery:healthCheckUrl"] = "https://health.example.com:9000/foobar"
        };

        ConsulServiceRegistration registration = TestRegistrationFactory.CreateRegistration(appSettings);

        registration.Payload.Check.HTTP.Should().Be("https://health.example.com:9000/foobar");
    }
}
