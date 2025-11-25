// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ThisServiceInstanceTest
{
    [Fact]
    public void Constructor_Initializes()
    {
        var serviceRegistration = new AgentServiceRegistration
        {
            ID = "ID",
            Name = "foobar",
            Address = "test.foo.bar",
            Port = 1234,
            Meta = new Dictionary<string, string>
            {
                ["foo"] = "bar"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var consulRegistration = new ConsulRegistration(serviceRegistration, optionsMonitor);
        var instance = new ThisServiceInstance(consulRegistration);

        instance.Host.Should().Be("test.foo.bar");
        instance.ServiceId.Should().Be("foobar");
        instance.InstanceId.Should().Be("ID");
        instance.IsSecure.Should().BeFalse();
        instance.Port.Should().Be(1234);
        instance.Metadata.Should().ContainSingle();
        instance.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instance.Uri.Should().Be(new Uri("http://test.foo.bar:1234"));
        instance.NonSecureUri.Should().Be(instance.Uri);
        instance.SecureUri.Should().BeNull();
    }
}
