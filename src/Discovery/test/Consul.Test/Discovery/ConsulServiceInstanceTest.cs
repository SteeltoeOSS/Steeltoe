// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Consul;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Test.Registry;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulServiceInstanceTest
{
    [Fact]
    public void FromServiceEntry_Initializes()
    {
        var healthService = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = "ServiceId",
                ID = "Instance1",
                Address = "foo.bar.com",
                Port = 1234,
                Tags =
                [
                    "tag1",
                    "tag2"
                ],
                Meta = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                    ["secure"] = "true"
                }
            }
        };

        ConsulServiceInstance instance = ConsulServiceInstance.FromServiceEntry(healthService);

        instance.Host.Should().Be("foo.bar.com");
        instance.ServiceId.Should().Be("ServiceId");
        instance.InstanceId.Should().Be("Instance1");
        instance.IsSecure.Should().BeTrue();
        instance.Port.Should().Be(1234);
        instance.Tags.Should().HaveCount(2);
        instance.Tags.Should().Contain("tag1");
        instance.Tags.Should().Contain("tag2");
        instance.Metadata.Should().HaveCount(2);
        instance.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instance.Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
        instance.Uri.Should().Be(new Uri("https://foo.bar.com:1234"));
        instance.NonSecureUri.Should().BeNull();
        instance.SecureUri.Should().Be(instance.Uri);
    }

    [Fact]
    public void FromServiceEntry_accepts_null_tags_and_meta()
    {
        var healthService = new ServiceEntry
        {
            Service = new AgentService
            {
                Service = "ServiceId",
                ID = "Instance1",
                Address = "foo.bar.com",
                Port = 1234
            }
        };

        ConsulServiceInstance instance = ConsulServiceInstance.FromServiceEntry(healthService);

        instance.Tags.Should().BeEmpty();
        instance.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void FromOptions_Initializes()
    {
        var options = new ConsulDiscoveryOptions
        {
            ServiceName = "foobar",
            InstanceId = "Instance1",
            HostName = "test.foo.bar",
            Port = 1234,
            Metadata =
            {
                ["foo"] = "bar"
            },
            Tags =
            {
                "tag1",
                "tag2"
            }
        };

        ConsulServiceInstance instance = ConsulServiceInstance.FromOptions(options);

        instance.Host.Should().Be("test.foo.bar");
        instance.ServiceId.Should().Be("foobar");
        instance.InstanceId.Should().Be("Instance1");
        instance.IsSecure.Should().BeFalse();
        instance.Port.Should().Be(1234);
        instance.Tags.Should().HaveCount(2);
        instance.Tags.Should().Contain("tag1");
        instance.Tags.Should().Contain("tag2");
        instance.Metadata.Should().HaveCount(2);
        instance.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instance.Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("false");
        instance.Uri.Should().Be(new Uri("http://test.foo.bar:1234"));
        instance.NonSecureUri.Should().Be(instance.Uri);
        instance.SecureUri.Should().BeNull();
    }

    [Fact]
    public void FromOptions_ReturnsExpectedTags()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "some",
            ["consul:discovery:tags:0"] = "foo",
            ["consul:discovery:tags:1"] = "bar"
        };

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.Tags.Should().HaveCount(2);
        instance.Tags.Should().Contain("foo");
        instance.Tags.Should().Contain("bar");
    }

    [Fact]
    public void FromOptions_ReturnsExpectedMetadata()
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

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);
        IReadOnlyDictionary<string, string?> metadata = instance.Metadata;

        metadata.Should().HaveCount(5);
        metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        metadata.Should().ContainKey("baz").WhoseValue.Should().Be("qux");
        metadata.Should().ContainKey("zone").WhoseValue.Should().Be("instanceZone");
        metadata.Should().ContainKey("group").WhoseValue.Should().Be("instanceGroup");
        metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
    }

    [Fact]
    public void FromOptions_SetsExpectedServiceId()
    {
        var appSettings = new Dictionary<string, string?>();

        // default value is assembly name
        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.ServiceId.Should().Be(Assembly.GetEntryAssembly()!.GetName().Name!.Replace('.', '-'));

        // followed by spring:application:name
        appSettings.Add("spring:application:name", "SpringApplicationName");
        instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.ServiceId.Should().Be("SpringApplicationName");

        // Consul-discovery is the highest priority
        appSettings.Add("consul:discovery:serviceName", "ConsulDiscoveryServiceName");
        instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.ServiceId.Should().Be("ConsulDiscoveryServiceName");
    }

    [Fact]
    public void FromOptions_ReturnsDefaultInstanceId()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "serviceName"
        };

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.InstanceId.Should().StartWith("serviceName-");
    }

    [Fact]
    public void FromOptions_ReturnsConfiguredInstanceId()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:instanceId"] = "instanceId",
            ["spring:application:name"] = "foobar"
        };

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.InstanceId.Should().Be("instanceId");

        appSettings.Remove("consul:discovery:instanceId");
        instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.InstanceId.Should().StartWith("foobar-");
    }

    [Fact]
    public void FromOptions_ReturnsExpectedSecureInstance()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar",
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1100",
            ["consul:discovery:scheme"] = "https"
        };

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(appSettings);

        instance.InstanceId.Should().StartWith("foobar-");
        instance.IsSecure.Should().BeTrue();
        instance.ServiceId.Should().Be("foobar");
        instance.Host.Should().Be("some-host");
        instance.Port.Should().Be(1100);
        instance.Uri.Should().Be(new Uri("https://some-host:1100"));
        instance.SecureUri.Should().Be(instance.Uri);
        instance.NonSecureUri.Should().BeNull();
    }
}
