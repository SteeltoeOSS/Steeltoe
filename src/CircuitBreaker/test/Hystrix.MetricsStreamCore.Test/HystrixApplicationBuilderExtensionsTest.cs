// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test;

public class HystrixApplicationBuilderExtensionsTest : HystrixTestBase
{
    [Fact]
    [Obsolete("To be removed in the next major version.")]
    public void UseHystrixMetricsStream_ThrowsIfBuilderNull()
    {
        const IApplicationBuilder builder = null;

        var ex = Assert.Throws<ArgumentNullException>(() => builder.UseHystrixMetricsStream());
        Assert.Contains(nameof(builder), ex.Message);
    }
}
