// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test
{
    public class PlaceholderResolverExtensionsTest
    {
        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PlaceholderResolverExtensions.AddPlaceholderResolver(configurationBuilder));
        }

        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration configuration = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PlaceholderResolverExtensions.AddPlaceholderResolver(configuration));
        }

        [Fact]
        public void AddPlaceholderResolver_AddsPlaceholderResolverSourceToList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();

            var placeholderSource =
                configurationBuilder.Sources.OfType<PlaceholderResolverSource>().SingleOrDefault();
            Assert.NotNull(placeholderSource);
        }

        [Fact]
        public void AddPlaceholderResolver_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver(loggerFactory);
            var configuration = configurationBuilder.Build();

            var provider =
                configuration.Providers.OfType<PlaceholderResolverProvider>().SingleOrDefault();

            Assert.NotNull(provider);
            Assert.NotNull(provider._logger);
        }

        [Fact]
        public void AddPlaceholderResolver_JsonAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name?noname}"",
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_XmlAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
<settings>
    <spring>
        <bar>
            <name>myName</name>
        </bar>
      <cloud>
        <config>
            <name>${spring:bar:name?noName}</name>
        </config>
      </cloud>
    </spring>
</settings>";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddXmlFile(fileName);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_IniAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
[spring:bar]
    name=myName
[spring:cloud:config]
    name=${spring:bar:name?noName}
";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddIniFile(fileName);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_CommandLineAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = new string[]
                {
                            "spring:bar:name=myName",
                            "--spring:cloud:config:name=${spring:bar:name?noName}"
                };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(appsettings);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_HandlesRecursivePlaceHolders()
        {
            var appsettingsJson = @"
                {
                    ""spring"": {
                        ""json"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:xml:name?noname}"",
                        }
                      }
                    }
                }";

            var appsettingsXml = @"
<settings>
    <spring>
        <xml>
            <name>${spring:ini:name?noName}</name>
        </xml>
    </spring>
</settings>";

            var appsettingsIni = @"
[spring:ini]
    name=${spring:line:name?noName}
";
            var appsettingsLine = new string[]
    {
                            "--spring:line:name=${spring:json:name?noName}"
    };
            var jsonpath = TestHelpers.CreateTempFile(appsettingsJson);
            var jsonfileName = Path.GetFileName(jsonpath);
            var xmlpath = TestHelpers.CreateTempFile(appsettingsXml);
            var xmlfileName = Path.GetFileName(xmlpath);
            var inipath = TestHelpers.CreateTempFile(appsettingsIni);
            var inifileName = Path.GetFileName(inipath);

            var directory = Path.GetDirectoryName(jsonpath);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(jsonfileName);
            configurationBuilder.AddXmlFile(xmlfileName);
            configurationBuilder.AddIniFile(inifileName);
            configurationBuilder.AddCommandLine(appsettingsLine);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_ClearsSources()
        {
            var settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            builder.AddPlaceholderResolver();

            Assert.Single(builder.Sources);
            var config = builder.Build();

            Assert.Single(config.Providers);
            var provider = config.Providers.ToList()[0];
            Assert.IsType<PlaceholderResolverProvider>(provider);
        }

        [Fact]
        public void AddPlaceholderResolver_WithConfiguration_ReturnsNewConfiguration()
        {
            var settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var config1 = builder.Build();

            var config2 = config1.AddPlaceholderResolver();
            Assert.NotSame(config1, config2);

            var root2 = config2 as IConfigurationRoot;
            Assert.Single(root2.Providers);
            var provider = root2.Providers.ToList()[0];
            Assert.IsType<PlaceholderResolverProvider>(provider);

            Assert.Null(config2["nokey"]);
            Assert.Equal("value1", config2["key1"]);
            Assert.Equal("value1", config2["key2"]);
            Assert.Equal("notfound", config2["key3"]);
            Assert.Equal("${nokey}", config2["key4"]);
        }
    }
}
