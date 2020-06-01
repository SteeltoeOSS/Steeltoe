// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Health.Test
{
    public class HealthEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseHealthActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseHealthActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseHealthActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseHealthActuator(config: null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void UseHealthActuator_ThrowsIfOptionsNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseHealthActuator(options: null));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void UseHealthActuator_ThrowsIfAggregatorNull()
        {
            var builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var options = new HealthEndpointOptions(config);
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseHealthActuator(options, aggregator: null));
            Assert.Equal("aggregator", exception.ParamName);
        }

        [Fact]
        public void UseHealthActuator_ThrowsIfContributorsNull()
        {
            var builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var options = new HealthEndpointOptions(config);
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseHealthActuator(options, new DefaultHealthAggregator(), contributors: null));
            Assert.Equal("contributors", exception.ParamName);
        }
    }
}
