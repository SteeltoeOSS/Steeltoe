// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry.Test
{
    public class HypermediaEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseCloudFoundryActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseCloudFoundryActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseCloudFoundryActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseCloudFoundryActuator(null));
            Assert.Equal("config", exception.ParamName);
        }
    }
}
