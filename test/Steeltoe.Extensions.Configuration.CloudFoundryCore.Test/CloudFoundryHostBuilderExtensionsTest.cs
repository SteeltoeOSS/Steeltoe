//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.AspNetCore.Hosting;
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {

        [Fact]
        public void UseCloudFoundryHosting_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryHostBuilderExtensions.UseCloudFoundryHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        public void UseCloudFoundryHosting_DoNotSetUrlsIfNull()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup>()
                                .UseKestrel();

            // Act and Assert
            hostBuilder.UseCloudFoundryHosting();
            using (hostBuilder.Build())
            {
                //No-Op
            }
        }

        [Fact]
        public void UseCloudFoundryHosting_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "42");
            var hostBuilder = new WebHostBuilder()
                                .UseStartup<TestServerStartup42>()
                                .UseKestrel();

            // Act and Assert
            hostBuilder.UseCloudFoundryHosting();
            using (hostBuilder.Build())
            {
                //No-Op
            }

        }
    }
}
