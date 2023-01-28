// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverConfigurationExtensionsTest
{
    [Fact]
    public void AddEncryptionResolver_WithConfigurationBuilder_ThrowsIfNulls()
    {
        const IConfigurationBuilder nullConfigurationBuilder = null;
        var configurationBuilder = new ConfigurationBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullConfigurationBuilder.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ThrowsIfNulls()
    {
        const IConfiguration nullConfiguration = null;
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullConfiguration.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => configuration.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WithConfigurationManager_ThrowsIfNulls()
    {
        const ConfigurationManager nullConfigurationManager = null;
        var configurationManager = new ConfigurationManager();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullConfigurationManager.AddEncryptionResolver(loggerFactory));
        Assert.Throws<ArgumentNullException>(() => configurationManager.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_AddsEncryptionResolverSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddEncryptionResolver();

        EncryptionResolverSource encryptionSource = configurationBuilder.Sources.OfType<EncryptionResolverSource>().SingleOrDefault();
        Assert.NotNull(encryptionSource);
    }

    [Fact]
    public void AddEncryptionResolver_WithLoggerFactorySucceeds()
    {
        var configurationBuilder = new ConfigurationBuilder();
        var loggerFactory = new LoggerFactory();

        configurationBuilder.AddEncryptionResolver(loggerFactory);
        IConfigurationRoot configuration = configurationBuilder.Build();

        EncryptionResolverProvider provider = configuration.Providers.OfType<EncryptionResolverProvider>().SingleOrDefault();

        Assert.NotNull(provider);
        Assert.NotNull(provider.Logger);
    }

    [Fact]
    public void AddEncryptionResolver_JsonAppSettingsResolvesEncryptions()
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
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, false);

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddEncryptionResolver_XmlAppSettingsResolvesEncryptions()
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
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddXmlFile(fileName, false, false);

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddEncryptionResolver_IniAppSettingsResolvesEncryptions()
    {
        const string appsettings = @"
[spring:bar]
    name=myName
[spring:cloud:config]
    name=${spring:bar:name?noName}
";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddIniFile(fileName, false, false);

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddEncryptionResolver_CommandLineAppSettingsResolvesEncryptions()
    {
        string[] appsettings =
        {
            "spring:bar:name=myName",
            "--spring:cloud:config:name=${spring:bar:name?noName}"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCommandLine(appsettings);

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddEncryptionResolver_HandlesRecursivePlaceHolders()
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

        string[] appsettingsLine =
        {
            "--spring:line:name=${spring:json:name?noName}"
        };

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string iniPath = sandbox.CreateFile("appsettings.ini", appsettingsIni);
        string iniFileName = Path.GetFileName(iniPath);

        string directory = Path.GetDirectoryName(jsonPath);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(jsonFileName, false, false);
        configurationBuilder.AddXmlFile(xmlFileName, false, false);
        configurationBuilder.AddIniFile(iniFileName, false, false);
        configurationBuilder.AddCommandLine(appsettingsLine);

        configurationBuilder.AddEncryptionResolver();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        Assert.Equal("myName", configurationRoot["spring:cloud:config:name"]);
    }

    [Fact]
    public void AddEncryptionResolver_ClearsSources()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        builder.AddEncryptionResolver();

        Assert.Single(builder.Sources);
        IConfigurationRoot configurationRoot = builder.Build();

        Assert.Single(configurationRoot.Providers);
        IConfigurationProvider provider = configurationRoot.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);
    }

    [Fact]
    public void AddEncryptionResolver_WithConfiguration_ReturnsNewConfiguration()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        IConfigurationRoot config1 = builder.Build();

        IConfiguration config2 = config1.AddEncryptionResolver();
        Assert.NotSame(config1, config2);

        var root2 = config2 as IConfigurationRoot;
        Assert.Single(root2.Providers);
        IConfigurationProvider provider = root2.Providers.ToList()[0];
        Assert.IsType<EncryptionResolverProvider>(provider);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("value1", config2["key2"]);
        Assert.Equal("notfound", config2["key3"]);
        Assert.Equal("${nokey}", config2["key4"]);
    }
}
