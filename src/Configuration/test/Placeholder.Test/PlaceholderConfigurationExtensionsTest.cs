// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderConfigurationExtensionsTest
{
    [Fact]
    public void AddPlaceholderResolver_AddsPlaceholderResolverSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddPlaceholderResolver();

        PlaceholderResolverSource? placeholderSource = configurationBuilder.Sources.OfType<PlaceholderResolverSource>().SingleOrDefault();
        Assert.NotNull(placeholderSource);
    }

    [Fact]
    public void AddPlaceholderResolver_NoDuplicates()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddPlaceholderResolver();
        configurationBuilder.AddPlaceholderResolver();
        configurationBuilder.AddPlaceholderResolver();

        PlaceholderResolverSource? source = configurationBuilder.Sources.OfType<PlaceholderResolverSource>().SingleOrDefault();
        Assert.NotNull(source);
        Assert.NotNull(source.Sources);
        Assert.Empty(source.Sources);
    }

    [Fact]
    public void AddPlaceholderResolver_CreatesProvider()
    {
        var configurationBuilder = new ConfigurationBuilder();
        var loggerFactory = new LoggerFactory();

        configurationBuilder.AddPlaceholderResolver(loggerFactory);
        IConfigurationRoot configuration = configurationBuilder.Build();

        PlaceholderResolverProvider? provider = configuration.Providers.OfType<PlaceholderResolverProvider>().SingleOrDefault();

        Assert.NotNull(provider);
    }

    [Fact]
    public void AddPlaceholderResolver_JsonAppSettingsResolvesPlaceholders()
    {
        const string appsettings = @"
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, false);

        configurationBuilder.AddPlaceholderResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_XmlAppSettingsResolvesPlaceholders()
    {
        const string appsettings = @"
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddXmlFile(fileName, false, false);

        configurationBuilder.AddPlaceholderResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_IniAppSettingsResolvesPlaceholders()
    {
        const string appsettings = @"
[spring:bar]
    name=myName
[spring:cloud:config]
    name=${spring:bar:name?noName}
";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddIniFile(fileName, false, false);

        configurationBuilder.AddPlaceholderResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_CommandLineAppSettingsResolvesPlaceholders()
    {
        string[] appsettings =
        [
            "spring:bar:name=myName",
            "--spring:cloud:config:name=${spring:bar:name?noName}"
        ];

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCommandLine(appsettings);

        configurationBuilder.AddPlaceholderResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_HandlesRecursivePlaceHolders()
    {
        const string appsettingsJson = @"
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

        const string appsettingsXml = @"
<settings>
    <spring>
        <xml>
            <name>${spring:ini:name?noName}</name>
        </xml>
    </spring>
</settings>";

        const string appsettingsIni = @"
[spring:ini]
    name=${spring:line:name?noName}
";

        string[] appsettingsLine = ["--spring:line:name=${spring:json:name?noName}"];

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string iniPath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
        string iniFileName = Path.GetFileName(iniPath);

        string directory = Path.GetDirectoryName(jsonPath)!;
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(jsonFileName, false, false);
        configurationBuilder.AddXmlFile(xmlFileName, false, false);
        configurationBuilder.AddIniFile(iniFileName, false, false);
        configurationBuilder.AddCommandLine(appsettingsLine);

        configurationBuilder.AddPlaceholderResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddPlaceholderResolver_ClearsSources()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        builder.AddPlaceholderResolver();

        Assert.Single(builder.Sources);
        IConfigurationRoot configurationRoot = builder.Build();

        Assert.Single(configurationRoot.Providers);
        IConfigurationProvider provider = configurationRoot.Providers.ToList()[0];
        Assert.IsType<PlaceholderResolverProvider>(provider);
    }

    [Fact]
    public void AddPlaceholderResolver_WithConfiguration_ReturnsNewConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IConfiguration config2 = config1.AddPlaceholderResolver();
        Assert.NotSame(config1, config2);

        var root2 = (IConfigurationRoot)config2;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<PlaceholderResolverProvider>(provider);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("value1", config2["key2"]);
        Assert.Equal("notfound", config2["key3"]);
        Assert.Equal("${nokey}", config2["key4"]);
    }

    [Fact]
    public void AddPlaceholderResolver_WithConfiguration_NoDuplicates()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        IConfiguration newConfiguration = configurationRoot.AddPlaceholderResolver().AddPlaceholderResolver().AddPlaceholderResolver();

        ConfigurationRoot newConfigurationRoot = newConfiguration.Should().BeOfType<ConfigurationRoot>().Which;
        newConfigurationRoot.Providers.Should().HaveCount(1);

        PlaceholderResolverProvider? provider = newConfigurationRoot.Providers.Single().Should().BeOfType<PlaceholderResolverProvider>().Which;
        provider.Providers.Should().BeEmpty();
    }
}
