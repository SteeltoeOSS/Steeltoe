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
                Tags = new[] { "tag1", "tag2" },
                Meta = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                    ["secure"] = "true"
                }
            }
        };

        var si = new ConsulServiceInstance(healthService);
        Assert.Equal("foo.bar.com", si.Host);
        Assert.Equal("ServiceId", si.ServiceId);
        Assert.True(si.IsSecure);
        Assert.Equal(1234, si.Port);
        Assert.Equal(2, si.Tags.Length);
        Assert.Contains("tag1", si.Tags);
        Assert.Contains("tag2", si.Tags);
        Assert.Equal(2, si.Metadata.Count);
        Assert.Contains(si.Metadata, x => x.Key == "foo");
        Assert.Equal("bar", si.Metadata["foo"]);
        Assert.Contains(si.Metadata, x => x.Key == "secure");
        Assert.Equal("true", si.Metadata["secure"]);
        Assert.Equal(new Uri("https://foo.bar.com:1234"), si.Uri);
    }
}
