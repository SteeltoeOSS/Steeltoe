// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.CloudFoundry.Test;

public sealed class EurekaServiceInstanceTest
{
    [Fact]
    public void InstanceWithBothPorts()
    {
        var instance = new InstanceInfo("id", "app", "host", "127.0.0.1", new DataCenterInfo())
        {
            NonSecurePort = 8888,
            IsNonSecurePortEnabled = true,
            SecurePort = 9999,
            IsSecurePortEnabled = true,
            Metadata = new Dictionary<string, string?>
            {
                ["foo"] = "bar"
            }
        };

        var serviceInstance = new EurekaServiceInstance(instance);

        serviceInstance.ServiceId.Should().Be(instance.AppName);
        serviceInstance.Host.Should().Be(instance.HostName);
        serviceInstance.Port.Should().Be(instance.SecurePort);
        serviceInstance.IsSecure.Should().BeTrue();
        serviceInstance.Metadata.Should().ContainSingle(pair => pair.Key == "foo" && pair.Value == "bar");
        serviceInstance.Uri.Should().Be("https://host:9999/");
    }

    [Fact]
    public void InstanceWithSecurePort()
    {
        var instance = new InstanceInfo("id", "app", "host", "127.0.0.1", new DataCenterInfo())
        {
            NonSecurePort = 8888,
            IsNonSecurePortEnabled = false,
            SecurePort = 9999,
            IsSecurePortEnabled = true
        };

        var serviceInstance = new EurekaServiceInstance(instance);

        serviceInstance.Port.Should().Be(instance.SecurePort);
        serviceInstance.IsSecure.Should().BeTrue();
        serviceInstance.Uri.Should().Be("https://host:9999/");
    }

    [Fact]
    public void InstanceWithNonSecurePort()
    {
        var instance = new InstanceInfo("id", "app", "host", "127.0.0.1", new DataCenterInfo())
        {
            NonSecurePort = 8888,
            IsNonSecurePortEnabled = true,
            SecurePort = 9999,
            IsSecurePortEnabled = false
        };

        var serviceInstance = new EurekaServiceInstance(instance);

        serviceInstance.Port.Should().Be(instance.NonSecurePort);
        serviceInstance.IsSecure.Should().BeFalse();
        serviceInstance.Uri.Should().Be("http://host:8888/");
    }

    [Fact]
    public void InstanceWithoutPort()
    {
        var instance = new InstanceInfo("id", "app", "host", "127.0.0.1", new DataCenterInfo())
        {
            NonSecurePort = 8888,
            IsNonSecurePortEnabled = false,
            SecurePort = 9999,
            IsSecurePortEnabled = false
        };

        var serviceInstance = new EurekaServiceInstance(instance);

        serviceInstance.Port.Should().Be(0);
        serviceInstance.IsSecure.Should().BeFalse();
        serviceInstance.Uri.Should().Be("http://host:0/");
    }
}
