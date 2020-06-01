// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulRetryOptionsTest
    {
        [Fact]
        public void Constructor_InitsDefaults()
        {
            ConsulRetryOptions opts = new ConsulRetryOptions();
            Assert.False(opts.Enabled);
            Assert.Equal(ConsulRetryOptions.DEFAULT_MAX_RETRY_ATTEMPTS, opts.MaxAttempts);
            Assert.Equal(ConsulRetryOptions.DEFAULT_INITIAL_RETRY_INTERVAL, opts.InitialInterval);
            Assert.Equal(ConsulRetryOptions.DEFAULT_RETRY_MULTIPLIER, opts.Multiplier);
            Assert.Equal(ConsulRetryOptions.DEFAULT_MAX_RETRY_INTERVAL, opts.MaxInterval);
        }
    }
}
