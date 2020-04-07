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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundryCore.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {
        [Fact]
        public void WebHostAddCloudFoundry_Adds()
        {
            // arrange
            var hostbuilder = new WebHostBuilder();
            hostbuilder.Configure(builder => { });

            // act
            hostbuilder.AddCloudFoundry();
            var host = hostbuilder.Build();

            // assert
            var instanceInfo = host.Services.GetApplicationInstanceInfo();
            Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
        }

        [Fact]
        public void HostAddCloudFoundry_Adds()
        {
            // arrange
            var hostbuilder = new HostBuilder();

            // act
            hostbuilder.AddCloudFoundry();
            var host = hostbuilder.Build();

            // assert
            var instanceInfo = host.Services.GetApplicationInstanceInfo();
            Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
            var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
            Assert.Contains(cfg.Providers, ctype => ctype is CloudFoundryConfigurationProvider);
        }
    }
}
