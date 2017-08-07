using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            InfoOptions opts = new InfoOptions();
            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("info", opts.Id);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new InfoOptions(config));
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
                'enabled': false,
                'sensitive': false,
                'id': 'infomanagement'
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

            InfoOptions opts = new InfoOptions(config);
            Assert.False(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("infomanagement", opts.Id);


            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.False(opts.Global.Sensitive);
            Assert.Equal("/management", opts.Global.Path);
        }
    }
}
