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
