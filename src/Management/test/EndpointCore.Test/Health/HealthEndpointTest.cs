// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOptionsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(new HealthEndpointOptions(), null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(new HealthEndpointOptions(), new HealthRegistrationsAggregator(), null, null, null));
            Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(new HealthEndpointOptions(), new HealthRegistrationsAggregator(), new List<IHealthContributor>(), null, null));
            var svcOptions = default(IOptionsMonitor<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>);
            Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(new HealthEndpointOptions(), new HealthRegistrationsAggregator(), new List<IHealthContributor>(), svcOptions, null));
        }
    }
}
