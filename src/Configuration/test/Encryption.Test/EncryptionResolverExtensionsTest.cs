// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test;

public sealed class EncryptionResolverExtensionsTest
{
    private readonly Mock<ITextDecryptor> _decryptorMock;

    public EncryptionResolverExtensionsTest()
    {
        _decryptorMock = new Mock<ITextDecryptor>();
    }
    [Fact]
    public void ConfigureEncryptionResolver_WithServiceCollection_ThrowsIfNulls()
    {
        const IServiceCollection nullServiceCollection = null;
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullServiceCollection.ConfigureEncryptionResolver(configuration, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(null, loggerFactory,_decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(configuration, null,_decryptorMock.Object));
    }

    [Fact]
    public void AddEncryptionResolver_WithWebHostBuilder_ThrowsIfNulls()
    {
        const IWebHostBuilder nullWebHostBuilder = null;
        var webHostBuilder = new WebHostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebHostBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => webHostBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WithHostBuilder_ThrowsIfNulls()
    {
        const IHostBuilder nullHostBuilder = null;
        var hostBuilder = new HostBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullHostBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => hostBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void AddEncryptionResolver_WebApplicationBuilder_ThrowsIfNulls()
    {
        const WebApplicationBuilder nullWebApplicationBuilder = null;
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => nullWebApplicationBuilder.AddEncryptionResolver(loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => webApplicationBuilder.AddEncryptionResolver(null));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ThrowsIfNulls()
    {
        const IServiceCollection nullServices = null;
        var serviceCollection = new ServiceCollection();
        var loggerFactory = NullLoggerFactory.Instance;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => nullServices.ConfigureEncryptionResolver(configuration, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(null, loggerFactory, _decryptorMock.Object));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.ConfigureEncryptionResolver(configuration, null, _decryptorMock.Object));
    }

    [Fact]
    public void ConfigureEncryptionResolver_ConfiguresIConfiguration_ReplacesExisting()
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

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForConfigureEncryptionResolver>().UseConfiguration(config1);

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForConfigureEncryptionResolver.ServiceProvider;
        IConfiguration config2 = services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.NotSame(config1, config2);

        Assert.Null(config2["nokey"]);
        Assert.Equal("value1", config2["key1"]);
        Assert.Equal("value1", config2["key2"]);
        Assert.Equal("notfound", config2["key3"]);
        Assert.Equal("${nokey}", config2["key4"]);
    }

    [Fact]
    public void AddEncryptionResolver_WebHostBuilder_WrapsApplicationsConfiguration()
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

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<StartupForAddEncryptionResolver>().ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(jsonFileName);
            configurationBuilder.AddXmlFile(xmlFileName);
            configurationBuilder.AddIniFile(iniFileName);
            configurationBuilder.AddCommandLine(appsettingsLine);
        }).AddEncryptionResolver(_decryptorMock.Object);

        using var server = new TestServer(hostBuilder);
        IServiceProvider services = StartupForAddEncryptionResolver.ServiceProvider;
        IConfiguration configuration = services.GetServices<IConfiguration>().SingleOrDefault();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }
    
    [Fact]
    public void AddEncryptionResolverViaWebApplicationBuilderWorks()
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
                        <name>${spring:json:name?noName}</name>
                    </xml>
                </spring>
            </settings>";

        using var sandbox = new Sandbox();
        string jsonPath = sandbox.CreateFile("appsettings.json", appsettingsJson);
        string jsonFileName = Path.GetFileName(jsonPath);
        string xmlPath = sandbox.CreateFile("appsettings.xml", appsettingsXml);
        string xmlFileName = Path.GetFileName(xmlPath);
        string directory = Path.GetDirectoryName(jsonPath);

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Configuration.SetBasePath(directory);
        hostBuilder.Configuration.AddJsonFile(jsonFileName);
        hostBuilder.Configuration.AddXmlFile(xmlFileName);
        hostBuilder.AddEncryptionResolver(_decryptorMock.Object);

        using WebApplication server = hostBuilder.Build();
        IConfiguration configuration = server.Services.GetServices<IConfiguration>().First();
        Assert.Equal("myName", configuration["spring:cloud:config:name"]);
    }
}
