// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class IAppBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudFoundryOpenIdConnect_ThrowsIfBuilderNull()
        {
            IAppBuilder app = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<ArgumentNullException>(() => app.UseCloudFoundryOpenIdConnect(config));
            Assert.Equal("appBuilder", exception.ParamName);
        }

        [Fact]
        public void UseCloudFoundryOpenIdConnect_ThrowsIfConfigurationNull()
        {
            IAppBuilder app = new AppBuilder();
            IConfiguration config = null;

            var exception = Assert.Throws<ArgumentNullException>(() => app.UseCloudFoundryOpenIdConnect(config));
            Assert.Equal("configuration", exception.ParamName);
        }

        [Fact]
        public void UseCloudFoundryJwtBearerAuthentication_ThrowsIfBuilderNull()
        {
            IAppBuilder app = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var exception = Assert.Throws<ArgumentNullException>(() => app.UseCloudFoundryJwtBearerAuthentication(config));
            Assert.Equal("appBuilder", exception.ParamName);
        }

        [Fact]
        public void UseCloudFoundryJwtBearerAuthentication_ThrowsIfConfigurationNull()
        {
            IAppBuilder app = new AppBuilder();
            IConfiguration config = null;

            var exception = Assert.Throws<ArgumentNullException>(() => app.UseCloudFoundryJwtBearerAuthentication(config));
            Assert.Equal("configuration", exception.ParamName);
        }
    }
}
