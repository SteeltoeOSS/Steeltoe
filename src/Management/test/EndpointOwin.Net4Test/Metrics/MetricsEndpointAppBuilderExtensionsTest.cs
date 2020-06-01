// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Metrics.Test
{
    public class MetricsEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseMetricsActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseMetricsActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseMetricsActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseMetricsActuator(null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void UseMetricsActuator_ThrowsIfStatsNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseMetricsActuator(config, stats: null, tags: null));
            Assert.Equal("stats", exception.ParamName);
        }

        [Fact]
        public void UseMetricsActuator_ThrowsIfTagsNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseMetricsActuator(config, stats: OpenCensusStats.Instance, tags: null));
            Assert.Equal("tags", exception.ParamName);
        }
    }
}
