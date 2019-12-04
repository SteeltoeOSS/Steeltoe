// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
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
