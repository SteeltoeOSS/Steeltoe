// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Registry;
using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class ThisServiceInstanceTest
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
            Tags = new[] { "foo=bar" }
        };
        var opts = new ConsulDiscoveryOptions();
        var registration = new ConsulRegistration(serviceRegistration, opts);
        var thiz = new ThisServiceInstance(registration);
        Assert.Equal("test.foo.bar", thiz.Host);
        Assert.Equal("foobar", thiz.ServiceId);
        Assert.False(thiz.IsSecure);
        Assert.Equal(1234, thiz.Port);
        Assert.Single(thiz.Metadata);
        Assert.Contains("foo", thiz.Metadata.Keys);
        Assert.Contains("bar", thiz.Metadata.Values);
        Assert.Equal(new Uri("http://test.foo.bar:1234"), thiz.Uri);
    }
}
