// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Xunit;

namespace Steeltoe.Discovery.Consul.Client.Test;

public class ConsulClientFactoryTest
{
    [Fact]
    public void CreateClient_ThrowsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => ConsulClientFactory.CreateClient(null));
    }

    [Fact]
    public void CreateClient_Succeeds()
    {
        var opts = new ConsulOptions
        {
            Host = "foobar",
            Datacenter = "datacenter",
            Token = "token",
            Username = "username",
            Password = "password",
            Port = 5555,
            Scheme = "https",
            WaitTime = "5s"
        };

        var client = ConsulClientFactory.CreateClient(opts) as ConsulClient;
        Assert.NotNull(client);
        Assert.NotNull(client.Config);
        Assert.Equal(opts.Datacenter, client.Config.Datacenter);
        Assert.Equal(opts.Token, client.Config.Token);
        Assert.Equal(opts.Host, client.Config.Address.Host);
        Assert.Equal(opts.Port, client.Config.Address.Port);
        Assert.Equal(opts.Scheme, client.Config.Address.Scheme);
        Assert.Equal(new TimeSpan(0, 0, 5), client.Config.WaitTime);
    }
}
