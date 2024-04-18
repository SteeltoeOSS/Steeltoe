// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Configuration;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class ConsulOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulOptions();

        Assert.Null(options.Datacenter);
        Assert.Null(options.Password);
        Assert.Null(options.Username);
        Assert.Null(options.WaitTime);
        Assert.Null(options.Token);
        Assert.Equal("localhost", options.Host);
        Assert.Equal("http", options.Scheme);
        Assert.Equal(8500, options.Port);
    }
}
