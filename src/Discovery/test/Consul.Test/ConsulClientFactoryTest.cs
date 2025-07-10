// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test;

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

        client.Should().NotBeNull();
        client.Config.Should().NotBeNull();
        client.Config.Datacenter.Should().Be(options.Datacenter);
        client.Config.Token.Should().Be(options.Token);
        client.Config.Address.Host.Should().Be(options.Host);
        client.Config.Address.Port.Should().Be(options.Port);
        client.Config.Address.Scheme.Should().Be(options.Scheme);
        client.Config.WaitTime.Should().Be(new TimeSpan(0, 0, 5));
    }
}
