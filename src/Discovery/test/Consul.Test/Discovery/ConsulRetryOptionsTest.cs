// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulRetryOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulRetryOptions();

        Assert.False(options.Enabled);
        Assert.Equal(ConsulRetryOptions.DefaultMaxRetryAttempts, options.MaxAttempts);
        Assert.Equal(ConsulRetryOptions.DefaultInitialRetryInterval, options.InitialInterval);
        Assert.Equal(ConsulRetryOptions.DefaultRetryMultiplier, options.Multiplier);
        Assert.Equal(ConsulRetryOptions.DefaultMaxRetryInterval, options.MaxInterval);
    }
}
