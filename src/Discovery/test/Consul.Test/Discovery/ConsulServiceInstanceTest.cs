// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class ConsulServiceInstanceTest
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
                Tags = new[] { "foo=bar", "secure=true" }
            }
        };

        var si = new ConsulServiceInstance(healthService);
        Assert.Equal("foo.bar.com", si.Host);
        Assert.Equal("ServiceId", si.ServiceId);
        Assert.True(si.IsSecure);
        Assert.Equal(1234, si.Port);
        Assert.Equal(2, si.Metadata.Count);
        Assert.Contains("foo", si.Metadata.Keys);
        Assert.Contains("secure", si.Metadata.Keys);
        Assert.Contains("bar", si.Metadata.Values);
        Assert.Contains("true", si.Metadata.Values);
        Assert.Equal(new Uri("https://foo.bar.com:1234"), si.Uri);
    }
}
