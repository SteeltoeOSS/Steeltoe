// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulServiceInstanceTest
{
    [Fact]
    public void Constructor_Initializes()
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

        var serviceInstance = new ConsulServiceInstance(healthService);

        serviceInstance.Host.Should().Be("foo.bar.com");
        serviceInstance.ServiceId.Should().Be("ServiceId");
        serviceInstance.InstanceId.Should().Be("Instance1");
        serviceInstance.IsSecure.Should().BeTrue();
        serviceInstance.Port.Should().Be(1234);
        serviceInstance.Tags.Should().HaveCount(2);
        serviceInstance.Tags.Should().Contain("tag1");
        serviceInstance.Tags.Should().Contain("tag2");
        serviceInstance.Metadata.Should().HaveCount(2);
        serviceInstance.Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        serviceInstance.Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
        serviceInstance.Uri.Should().Be(new Uri("https://foo.bar.com:1234"));
        serviceInstance.NonSecureUri.Should().BeNull();
        serviceInstance.SecureUri.Should().Be(serviceInstance.Uri);
    }
}
