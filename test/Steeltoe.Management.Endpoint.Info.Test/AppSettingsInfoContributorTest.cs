using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class AppSettingsInfoContributorTest : BaseTest
    {
        [Fact]
        public void ConstributeWithConfigNull()
        {
            var contributor = new AppSettingsInfoContributor(null);
            InfoBuilder builder = new InfoBuilder();
            contributor.Contribute(builder);
            var result = builder.Build();
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

        }

        [Fact]
        public void ContributeWithNullBUilderThrows()
        {
            var appsettings = @"
{
    'info': {
        'application': {
            'name': 'foobar',
            'version': '1.0.0',
            'date': '5/1/2008',
            'time' : '8:30:52 AM'
        },
        'NET' : {
            'type': 'Core',
            'version': '1.1.0'
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
            var settings = new AppSettingsInfoContributor(config);

            Assert.Throws<ArgumentNullException>(() => settings.Contribute(null));
        }
        [Fact]
        public void ContributeAddsToBuilder()
        {
            var appsettings = @"
{
    'info': {
        'application': {
            'name': 'foobar',
            'version': '1.0.0',
            'date': '5/1/2008',
            'time' : '8:30:52 AM'
        },
        'NET': {
            'type': 'Core',
            'version': '1.1.0',
            'ASPNET' : {
                'type': 'Core',
                'version': '1.1.0'
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
            var settings = new AppSettingsInfoContributor(config);

            InfoBuilder builder = new InfoBuilder();
            settings.Contribute(builder);

            Dictionary<string,object> info = builder.Build();
            Assert.NotNull(info);
            Assert.Equal(2, info.Count);
            Assert.True(info.ContainsKey("application"));
            Assert.True(info.ContainsKey("NET"));

            var appNode = info["application"] as Dictionary<string, object>;
            Assert.NotNull(appNode);
            Assert.Equal("foobar", appNode["name"]);

            var netNode = info["NET"] as Dictionary<string, object>;
            Assert.NotNull(netNode);
            Assert.Equal("Core", netNode["type"]);

            Assert.NotNull(netNode["ASPNET"]);
        }

    }
}
