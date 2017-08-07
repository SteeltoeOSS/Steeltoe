
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementOptionsTest : BaseTest
    {

        [Fact]
        public void InitializedWithDefaults()
        {
            ManagementOptions opts = ManagementOptions.GetInstance();
            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("/", opts.Path);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => ManagementOptions.GetInstance(config));
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/management',
            'info' : {
                'enabled': true,
                'sensitive': true,
                'id': '/infomanagement'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            ManagementOptions opts = ManagementOptions.GetInstance(config);
            Assert.False(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("/management", opts.Path);
        }

    }
}
