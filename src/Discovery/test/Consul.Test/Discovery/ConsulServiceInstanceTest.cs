// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Xunit;

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
        Assert.Equal("foo.bar.com", serviceInstance.Host);
        Assert.Equal("ServiceId", serviceInstance.ServiceId);
        Assert.True(serviceInstance.IsSecure);
        Assert.Equal(1234, serviceInstance.Port);
        Assert.Equal(2, serviceInstance.Tags.Count);
        Assert.Contains("tag1", serviceInstance.Tags);
        Assert.Contains("tag2", serviceInstance.Tags);
        Assert.Equal(2, serviceInstance.Metadata.Count);
        Assert.Contains(serviceInstance.Metadata, x => x.Key == "foo");
        Assert.Equal("bar", serviceInstance.Metadata["foo"]);
        Assert.Contains(serviceInstance.Metadata, x => x.Key == "secure");
        Assert.Equal("true", serviceInstance.Metadata["secure"]);
        Assert.Equal(new Uri("https://foo.bar.com:1234"), serviceInstance.Uri);
    }
}
