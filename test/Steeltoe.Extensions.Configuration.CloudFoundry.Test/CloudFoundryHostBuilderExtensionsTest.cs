using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryHostBuilderExtensionsTest
    {

        [Fact]
        public void AddCloudFoundry_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryHostBuilderExtensions.UseCloudFoundryHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        public void AddCloudFoundry_DoNotSetUrlsIfNull()
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
        public void AddCloudFoundry_MakeSureThePortIsSet()
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
