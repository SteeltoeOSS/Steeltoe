// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using System;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixApplicationBuilderExtensionsTest : HystrixTestBase
    {
        [Fact]
        [Obsolete]
        public void UseHystrixMetricsStream_ThrowsIfBuilderNull()
        {
            IApplicationBuilder builder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => builder.UseHystrixMetricsStream());
            Assert.Contains(nameof(builder), ex.Message);
        }
    }
}
