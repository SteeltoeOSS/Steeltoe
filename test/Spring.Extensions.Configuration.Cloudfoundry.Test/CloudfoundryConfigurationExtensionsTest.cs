using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Spring.Extensions.Configuration.Cloudfoundry.Test
{
    public class CloudfoundryConfigurationExtensionsTest
    {
        [Fact]
        public void AddConfigService_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudfoundryConfigurationExtensions.AddCloudfoundry(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);

        }

        [Fact]
        public void AddConfigService_AddsConfigServerProviderToProvidersList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddCloudfoundry();

            CloudfoundryConfigurationProvider cloudProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                cloudProvider = provider as CloudfoundryConfigurationProvider;
                if (cloudProvider != null)
                    break;
            }
            Assert.NotNull(cloudProvider);

        }
    }
}
