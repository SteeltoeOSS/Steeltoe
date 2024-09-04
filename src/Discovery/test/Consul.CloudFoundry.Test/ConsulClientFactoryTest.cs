// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.CloudFoundry.Test;

public sealed class ConsulClientFactoryTest
{
    [Fact]
    public void CreateClient_Succeeds()
    {
        var options = new ConsulOptions
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

        var client = ConsulClientFactory.CreateClient(options) as ConsulClient;

        Assert.NotNull(client);
        Assert.NotNull(client.Config);
        Assert.Equal(options.Datacenter, client.Config.Datacenter);
        Assert.Equal(options.Token, client.Config.Token);
        Assert.Equal(options.Host, client.Config.Address.Host);
        Assert.Equal(options.Port, client.Config.Address.Port);
        Assert.Equal(options.Scheme, client.Config.Address.Scheme);
        Assert.Equal(new TimeSpan(0, 0, 5), client.Config.WaitTime);
    }
}
