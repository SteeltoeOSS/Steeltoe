using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            TestOptions2 opts = new TestOptions2();
            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.NotNull(opts.Global);
            Assert.True(opts.Global.Enabled);
            Assert.False(opts.Global.Sensitive);
            Assert.Equal("/", opts.Global.Path);
        }

        [Fact]
        public void ThrowsIfSectionNameNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestOptions2(null, null));
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TestOptions2("foobar", config));
        }

        [Fact]
        public void CanSetEnableSensitive()
        {
            TestOptions2 opt2 = new TestOptions2()
            {
                Enabled = false,
                Sensitive = true
            };
            Assert.False(opt2.Enabled);
            Assert.True(opt2.Sensitive);
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': true,
            'path': '/management',
            'info' : {
                'enabled': true,
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


            TestOptions2 opts = new TestOptions2("management:endpoints:info", config);

            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.True(opts.Global.Sensitive);
            Assert.Equal("/management", opts.Global.Path);

            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);

        }

        [Fact]
        public void GlobalSettinsConfigureCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': true,
            'path': '/management',
            'info' : {
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


            TestOptions2 opts = new TestOptions2("management:endpoints:info", config);

            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.True(opts.Global.Sensitive);
            Assert.Equal("/management", opts.Global.Path);

            Assert.False(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);

        }
    }

    class TestOptions2 : AbstractOptions
    {
        public TestOptions2() : base()
        {

        }
        public TestOptions2(string section, IConfiguration config) :base(section, config)
        {

        }
    }
}
