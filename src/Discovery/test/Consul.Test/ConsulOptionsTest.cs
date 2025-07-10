// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class ConsulOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulOptions();

        options.Datacenter.Should().BeNull();
        options.Password.Should().BeNull();
        options.Username.Should().BeNull();
        options.WaitTime.Should().BeNull();
        options.Token.Should().BeNull();
        options.Host.Should().Be("localhost");
        options.Scheme.Should().Be("http");
        options.Port.Should().Be(8500);
    }
}
