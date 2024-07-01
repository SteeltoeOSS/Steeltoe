// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulRetryOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulRetryOptions();

        Assert.False(options.Enabled);
        Assert.Equal(6, options.MaxAttempts);
        Assert.Equal(1000, options.InitialInterval);
        Assert.Equal(1.1, options.Multiplier);
        Assert.Equal(2000, options.MaxInterval);
    }
}
