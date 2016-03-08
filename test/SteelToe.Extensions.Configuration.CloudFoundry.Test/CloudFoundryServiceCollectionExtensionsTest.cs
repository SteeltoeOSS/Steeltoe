using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using System;

using Xunit;

namespace SteelToe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddCloudFoundry_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundry(services, config));
            Assert.Contains(nameof(services), ex.Message);

        }
        [Fact]
        public void AddCloudFoundry_ThrowsIfConfigurtionNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundry(services, config));
            Assert.Contains(nameof(config), ex.Message);

        }
        [Fact]
        public void AddCloudFoundry_ConfiguresCloudFoundryOptions()
        { 
            // Arrange
            var services = new ServiceCollection();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();
            CloudFoundryServiceCollectionExtensions.AddCloudFoundry(services, config);

            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(app);
            var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);


        }
        [Fact]
        public void AddCloudFoundry_AddsConfigurationAsService()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();
            CloudFoundryServiceCollectionExtensions.AddCloudFoundry(services, config);

            var service = services.BuildServiceProvider().GetService<IConfigurationRoot>();
            Assert.NotNull(service);

        }
    }
}
