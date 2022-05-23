// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Utils.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test
{
    public class PlaceholderResolverConfigurationExtensionsTest
    {
        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigBuilderNull()
        {
            IConfigurationBuilder configurationBuilder = null;

            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddPlaceholderResolver());
        }

        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigNull()
        {
            IConfiguration configuration = null;

            var ex = Assert.Throws<ArgumentNullException>(() => configuration.AddPlaceholderResolver());
        }

        [Fact]
        public void AddPlaceholderResolver_AddsPlaceholderResolverSourceToList()
        {
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddPlaceholderResolver();

            var placeholderSource =
                configurationBuilder.Sources.OfType<PlaceholderResolverSource>().SingleOrDefault();
            Assert.NotNull(placeholderSource);
        }

        [Fact]
        public void AddPlaceholderResolver_WithLoggerFactorySucceeds()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();

            configurationBuilder.AddPlaceholderResolver(loggerFactory);
            var configuration = configurationBuilder.Build();

            var provider =
                configuration.Providers.OfType<PlaceholderResolverProvider>().SingleOrDefault();

            Assert.NotNull(provider);
            Assert.NotNull(provider._logger);
        }

        // Mac issue https://github.com/dotnet/runtime/issues/30056
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void AddPlaceholderResolver_JsonAppSettingsResolvesPlaceholders()
        {
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

            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName, false, false);

            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        // Mac issue https://github.com/dotnet/runtime/issues/30056
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void AddPlaceholderResolver_XmlAppSettingsResolvesPlaceholders()
        {
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
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddXmlFile(fileName, false, false);

            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        // Mac issue https://github.com/dotnet/runtime/issues/30056
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void AddPlaceholderResolver_IniAppSettingsResolvesPlaceholders()
        {
            var appsettings = @"
[spring:bar]
    name=myName
[spring:cloud:config]
    name=${spring:bar:name?noName}
";
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddIniFile(fileName, false, false);

            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_CommandLineAppSettingsResolvesPlaceholders()
        {
            var appsettings = new[]
                {
                            "spring:bar:name=myName",
                            "--spring:cloud:config:name=${spring:bar:name?noName}"
                };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(appsettings);

            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        // Mac issue https://github.com/dotnet/runtime/issues/30056
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
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
            var appsettingsLine = new[]
    {
                            "--spring:line:name=${spring:json:name?noName}"
    };
            using var sandbox = new Sandbox();
            var jsonpath = sandbox.CreateFile("appsettings.json", appsettingsJson);
            var jsonfileName = Path.GetFileName(jsonpath);
            var xmlpath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
            var xmlfileName = Path.GetFileName(xmlpath);
            var inipath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
            var inifileName = Path.GetFileName(inipath);

            var directory = Path.GetDirectoryName(jsonpath);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(jsonfileName, false, false);
            configurationBuilder.AddXmlFile(xmlfileName, false, false);
            configurationBuilder.AddIniFile(inifileName, false, false);
            configurationBuilder.AddCommandLine(appsettingsLine);

            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_ClearsSources()
        {
            var settings = new Dictionary<string, string>
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
            var settings = new Dictionary<string, string>
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
