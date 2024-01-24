// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Xunit;

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

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions());
        var consulRegistration = new ConsulRegistration(serviceRegistration, optionsMonitor);
        var instance = new ThisServiceInstance(consulRegistration);

        Assert.Equal("test.foo.bar", instance.Host);
        Assert.Equal("foobar", instance.ServiceId);
        Assert.False(instance.IsSecure);
        Assert.Equal(1234, instance.Port);
        Assert.Single(instance.Metadata);
        Assert.Contains("foo", instance.Metadata.Keys);
        Assert.Contains("bar", instance.Metadata.Values);
        Assert.Equal(new Uri("http://test.foo.bar:1234"), instance.Uri);
    }
}
