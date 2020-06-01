// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Info.Test
{
    public class InfoEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseInfoActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseInfoActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseInfoActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseInfoActuator(null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void UseInfoActuator_ThrowsIfContributorsNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseInfoActuator(config, contributors: null));
            Assert.Equal("contributors", exception.ParamName);
        }
    }
}
