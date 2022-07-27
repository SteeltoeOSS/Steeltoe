// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Consul.Client.Test;

public class ConsulOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var opts = new ConsulOptions();
        Assert.Null(opts.Datacenter);
        Assert.Null(opts.Password);
        Assert.Null(opts.Username);
        Assert.Null(opts.WaitTime);
        Assert.Null(opts.Token);
        Assert.Equal("localhost", opts.Host);
        Assert.Equal("http", opts.Scheme);
        Assert.Equal(8500, opts.Port);
    }
}