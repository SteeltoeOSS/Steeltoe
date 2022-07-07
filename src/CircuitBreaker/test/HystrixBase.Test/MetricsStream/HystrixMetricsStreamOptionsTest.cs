// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test;

public class HystrixMetricsStreamOptionsTest : HystrixTestBase
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var opts = new HystrixMetricsStreamOptions();
        Assert.True(opts.Validate_Certificates);
        Assert.Equal(500, opts.SendRate);
        Assert.Equal(500, opts.GatherRate);
    }
}
