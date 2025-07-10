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

        options.Enabled.Should().BeFalse();
        options.MaxAttempts.Should().Be(6);
        options.InitialInterval.Should().Be(1000);
        options.Multiplier.Should().Be(1.1);
        options.MaxInterval.Should().Be(2000);
    }
}
