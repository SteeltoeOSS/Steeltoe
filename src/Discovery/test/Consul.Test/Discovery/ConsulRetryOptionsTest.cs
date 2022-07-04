// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class ConsulRetryOptionsTest
{
    [Fact]
    public void Constructor_InitsDefaults()
    {
        var opts = new ConsulRetryOptions();
        Assert.False(opts.Enabled);
        Assert.Equal(ConsulRetryOptions.DefaultMaxRetryAttempts, opts.MaxAttempts);
        Assert.Equal(ConsulRetryOptions.DefaultInitialRetryInterval, opts.InitialInterval);
        Assert.Equal(ConsulRetryOptions.DefaultRetryMultiplier, opts.Multiplier);
        Assert.Equal(ConsulRetryOptions.DefaultMaxRetryInterval, opts.MaxInterval);
    }
}
